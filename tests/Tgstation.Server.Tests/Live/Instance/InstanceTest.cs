using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Moq;

using Tgstation.Server.Api.Models;
using Tgstation.Server.Api.Models.Request;
using Tgstation.Server.Api.Models.Response;
using Tgstation.Server.Client;
using Tgstation.Server.Client.Components;
using Tgstation.Server.Host.Components;
using Tgstation.Server.Host.Components.Byond;
using Tgstation.Server.Host.Configuration;
using Tgstation.Server.Host.IO;
using Tgstation.Server.Host.System;

namespace Tgstation.Server.Tests.Live.Instance
{
	sealed class InstanceTest
	{
		readonly IInstanceManagerClient instanceManagerClient;
		readonly IFileDownloader fileDownloader;
		readonly InstanceManager instanceManager;
		readonly ushort serverPort;

		public InstanceTest(IInstanceManagerClient instanceManagerClient, IFileDownloader fileDownloader, InstanceManager instanceManager, ushort serverPort)
		{
			this.instanceManagerClient = instanceManagerClient ?? throw new ArgumentNullException(nameof(instanceManagerClient));
			this.fileDownloader = fileDownloader ?? throw new ArgumentNullException(nameof(fileDownloader));
			this.instanceManager = instanceManager ?? throw new ArgumentNullException(nameof(instanceManager));
			this.serverPort = serverPort;
		}

		public async Task RunTests(
			IInstanceClient instanceClient,
			ushort dmPort,
			ushort ddPort,
			bool highPrioDD,
			bool lowPrioDeployment,
			bool usingBasicWatchdog,
			CancellationToken cancellationToken)
		{
			var byondTest = new ByondTest(instanceClient.Byond, instanceClient.Jobs, fileDownloader, instanceClient.Metadata);
			var chatTest = new ChatTest(instanceClient.ChatBots, instanceManagerClient, instanceClient.Jobs, instanceClient.Metadata);
			var configTest = new ConfigurationTest(instanceClient.Configuration, instanceClient.Metadata);
			var repoTest = new RepositoryTest(instanceClient.Repository, instanceClient.Jobs);
			var dmTest = new DeploymentTest(instanceClient, instanceClient.Jobs, dmPort, ddPort, lowPrioDeployment);

			var byondTask = byondTest.Run(cancellationToken, out var firstInstall);
			var chatTask = chatTest.RunPreWatchdog(cancellationToken);

			var repoLongJob = await repoTest.RunLongClone(cancellationToken);

			await dmTest.RunPreRepoClone(cancellationToken);
			await repoTest.AbortLongCloneAndCloneSomethingQuick(repoLongJob, cancellationToken);
			await configTest.RunPreWatchdog(cancellationToken);
			var dmTask = dmTest.RunPostRepoClone(firstInstall, cancellationToken);

			await chatTask;
			await dmTask;
			await configTest.SetupDMApiTests(true, cancellationToken);
			await byondTask;

			await new WatchdogTest(
				await ByondTest.GetEdgeVersion(fileDownloader, cancellationToken), instanceClient, instanceManager, serverPort, highPrioDD, ddPort, usingBasicWatchdog).Run(cancellationToken);
		}

		public async Task RunCompatTests(
			Version compatVersion,
			IInstanceClient instanceClient,
			ushort dmPort,
			ushort ddPort,
			bool highPrioDD,
			bool usingBasicWatchdog,
			CancellationToken cancellationToken)
		{
			System.Console.WriteLine($"COMPAT TEST START: {compatVersion}");
			const string Origin = "https://github.com/Cyberboss/common_core";
			var cloneRequest = instanceClient.Repository.Clone(new RepositoryCreateRequest
			{
				Origin = new Uri(Origin),
			}, cancellationToken);


			var dmUpdateRequest = instanceClient.DreamMaker.Update(new DreamMakerRequest
			{
				ApiValidationPort = dmPort,
			}, cancellationToken);

			// need at least one chat bot to satisfy DMAPI test,
			// use discord as it allows multi-botting on on token unlike IRC
			var connectionString = Environment.GetEnvironmentVariable("TGS_TEST_DISCORD_TOKEN");
			if (String.IsNullOrWhiteSpace(connectionString))
				// needs to just be valid
				connectionString = new DiscordConnectionStringBuilder
				{
					BotToken = "some_token",
					DeploymentBranding = true,
					DMOutputDisplay = DiscordDMOutputDisplayType.Always,
				}.ToString();
			else
				// standardize
				connectionString = new DiscordConnectionStringBuilder(connectionString).ToString();

			var channelIdStr = Environment.GetEnvironmentVariable("TGS_TEST_DISCORD_CHANNEL");
			if (String.IsNullOrWhiteSpace(channelIdStr))
				channelIdStr = "487268744419344384";

			var chatRequest = instanceClient.ChatBots.Create(new ChatBotCreateRequest
			{
				ChannelLimit = 10,
				Channels = new List<ChatChannel>
				{
					new ChatChannel
					{
						ChannelData = channelIdStr,
						Tag = "some_tag",
						IsAdminChannel = true,
						IsSystemChannel = true,
						IsUpdatesChannel = true,
						IsWatchdogChannel = true,
					},
				},
				ConnectionString = connectionString,
				Enabled = true,
				Name = "compat_test_bot",
				Provider = ChatProvider.Discord,
				ReconnectionInterval = 1,
			}, cancellationToken);

			var jrt = new JobsRequiredTest(instanceClient.Jobs);

			IByondInstaller byondInstaller = new PlatformIdentifier().IsWindows
				? new WindowsByondInstaller(
					Mock.Of<IProcessExecutor>(),
					Mock.Of<IIOManager>(),
					fileDownloader,
					Options.Create(new GeneralConfiguration()),
					Mock.Of<ILogger<WindowsByondInstaller>>())
				: new PosixByondInstaller(
					Mock.Of<IPostWriteHandler>(),
					Mock.Of<IIOManager>(),
					fileDownloader,
					Mock.Of<ILogger<PosixByondInstaller>>());

			using var windowsByondInstaller = byondInstaller as WindowsByondInstaller;

			// get the bytes for stable
			ByondInstallResponse installJob2;
			using (var stableBytesMs = await byondInstaller.DownloadVersion(compatVersion, cancellationToken))
			{
				installJob2 = await instanceClient.Byond.SetActiveVersion(new ByondVersionRequest
				{
					UploadCustomZip = true,
					Version = compatVersion,
				}, stableBytesMs, cancellationToken);
			}

			await chatRequest;
			await Task.Yield();


			await Task.WhenAll(
				jrt.WaitForJob(installJob2.InstallJob, 60, false, null, cancellationToken),
				jrt.WaitForJob(cloneRequest.Result.ActiveJob, 60, false, null, cancellationToken),
				dmUpdateRequest.AsTask(),
				cloneRequest.AsTask());

			var jobs = await instanceClient.Jobs.List(null, cancellationToken);
			var theJobWeWant = jobs
				.OrderByDescending(x => x.StartedAt)
				.First(x => x.Description.Contains("Reconnect chat bot"));
			await jrt.WaitForJob(theJobWeWant, 30, false, null, cancellationToken);

			var configSetupTask = new ConfigurationTest(instanceClient.Configuration, instanceClient.Metadata).SetupDMApiTests(true, cancellationToken);

			if (TestingUtils.RunningInGitHubActions
				|| String.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("TGS_TEST_GITHUB_TOKEN"))
				|| Environment.MachineName.Equals("CYBERSTATIONXVI", StringComparison.OrdinalIgnoreCase))
				await instanceClient.Repository.Update(new RepositoryUpdateRequest
				{
					CreateGitHubDeployments = true,
					PostTestMergeComment = true,
					PushTestMergeCommits = true,
					AccessUser = "Cyberboss",
					AccessToken = Environment.GetEnvironmentVariable("TGS_TEST_GITHUB_TOKEN"),
				}, cancellationToken);

			await configSetupTask;

			await new WatchdogTest(compatVersion, instanceClient, instanceManager, serverPort, highPrioDD, ddPort, usingBasicWatchdog).Run(cancellationToken);

			await instanceManagerClient.Update(new InstanceUpdateRequest
			{
				Id = instanceClient.Metadata.Id,
				Online = false,
			}, cancellationToken);
			System.Console.WriteLine($"COMPAT TEST END: {compatVersion}");
		}
	}
}
