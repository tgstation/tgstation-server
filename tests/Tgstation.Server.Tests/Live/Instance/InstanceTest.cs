using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Moq;

using Tgstation.Server.Api.Models;
using Tgstation.Server.Api.Models.Internal;
using Tgstation.Server.Api.Models.Request;
using Tgstation.Server.Api.Models.Response;
using Tgstation.Server.Client;
using Tgstation.Server.Client.Components;
using Tgstation.Server.Host.Components;
using Tgstation.Server.Host.Components.Engine;
using Tgstation.Server.Host.Components.Events;
using Tgstation.Server.Host.Components.Repository;
using Tgstation.Server.Host.Configuration;
using Tgstation.Server.Host.IO;
using Tgstation.Server.Host.System;

namespace Tgstation.Server.Tests.Live.Instance
{
	sealed class InstanceTest(IInstanceManagerClient instanceManagerClient, IFileDownloader fileDownloader, InstanceManager instanceManager, ushort serverPort)
	{
		readonly IInstanceManagerClient instanceManagerClient = instanceManagerClient ?? throw new ArgumentNullException(nameof(instanceManagerClient));
		readonly IFileDownloader fileDownloader = fileDownloader ?? throw new ArgumentNullException(nameof(fileDownloader));
		readonly InstanceManager instanceManager = instanceManager ?? throw new ArgumentNullException(nameof(instanceManager));
		readonly ushort serverPort = serverPort;

		public async Task RunLegacyByondTest(
			IInstanceClient instanceClient,
			CancellationToken cancellationToken)
		{
			var testVersion = await EngineTest.GetEdgeVersion(EngineType.Byond, fileDownloader, cancellationToken);
			await new LegacyByondTest(
				instanceClient.Jobs,
				fileDownloader,
				new LegacyByondClient(
					(IApiClient)instanceClient.Engine.GetType().GetProperty("ApiClient", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(instanceClient.Engine),
					instanceClient.Metadata),
				testVersion.Version,
				instanceClient.Metadata)
				.Run(cancellationToken);
		}

		public async Task RunTests(
			IInstanceClient instanceClient,
			ushort dmPort,
			ushort ddPort,
			bool highPrioDD,
			bool lowPrioDeployment,
			CancellationToken cancellationToken)
		{
			var testVersion = await EngineTest.GetEdgeVersion(EngineType.Byond, fileDownloader, cancellationToken);
			var engineTest = new EngineTest(instanceClient.Engine, instanceClient.Jobs, fileDownloader, instanceClient.Metadata, testVersion.Engine.Value);
			var chatTest = new ChatTest(instanceClient.ChatBots, instanceManagerClient, instanceClient.Jobs, instanceClient.Metadata);
			var configTest = new ConfigurationTest(instanceClient.Configuration, instanceClient.Metadata);
			var repoTest = new RepositoryTest(instanceClient.Repository, instanceClient.Jobs);
			var dmTest = new DeploymentTest(instanceClient, instanceClient.Jobs, dmPort, ddPort, lowPrioDeployment, testVersion.Engine.Value);

			var byondTask = engineTest.Run(cancellationToken, out var firstInstall);
			var chatTask = chatTest.RunPreWatchdog(cancellationToken);

			var repoLongJob = await repoTest.RunLongClone(cancellationToken);

			await dmTest.RunPreRepoClone(cancellationToken);
			await repoTest.AbortLongCloneAndCloneSomethingQuick(repoLongJob, cancellationToken);
			await configTest.RunPreWatchdog(cancellationToken);
			var dmTask = dmTest.RunPostRepoClone(firstInstall, cancellationToken);

			await chatTask;
			await dmTask;
			await byondTask;

			await new WatchdogTest(testVersion, instanceClient, instanceManager, serverPort, highPrioDD, ddPort)
				.Run(cancellationToken);
		}

		public async Task RunCompatTests(
			EngineVersion compatVersion,
			IInstanceClient instanceClient,
			ushort dmPort,
			ushort ddPort,
			bool highPrioDD,
			CancellationToken cancellationToken)
		{
			System.Console.WriteLine($"COMPAT TEST START: {compatVersion}");
			const string Origin = "https://github.com/Cyberboss/common_core";
			var cloneRequest = instanceClient.Repository.Clone(new RepositoryCreateRequest
			{
				Origin = new Uri(Origin),
			}, cancellationToken).AsTask();

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
					new ()
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


			var odRepoDir = Path.GetFullPath(Path.Combine(instanceClient.Metadata.Path, "..", "OpenDreamRepo"));
			var tmpIOManager = new ResolvingIOManager(new DefaultIOManager(), odRepoDir);

			var mockOptions = new Mock<IOptions<GeneralConfiguration>>();
			mockOptions.SetupGet(x => x.Value).Returns(new GeneralConfiguration());
			IEngineInstaller byondInstaller =
				compatVersion.Engine == EngineType.OpenDream
				? new OpenDreamInstaller(
					new DefaultIOManager(),
					Mock.Of<ILogger<OpenDreamInstaller>>(),
					new PlatformIdentifier(),
					Mock.Of<IProcessExecutor>(),
					new RepositoryManager(
						new LibGit2RepositoryFactory(
							Mock.Of<ILogger<LibGit2RepositoryFactory>>()),
						new LibGit2Commands(),
						tmpIOManager,
						new NoopEventConsumer(),
						Mock.Of<IPostWriteHandler>(),
						Mock.Of<IGitRemoteFeaturesFactory>(),
						Mock.Of<ILogger<Repository>>(),
						Mock.Of<ILogger<RepositoryManager>>(),
						new GeneralConfiguration()),
					mockOptions.Object)
				: new PlatformIdentifier().IsWindows
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
			EngineInstallResponse installJob2;
			await using (var stableBytesMs = await TestingUtils.ExtractMemoryStreamFromInstallationData(await byondInstaller.DownloadVersion(compatVersion, null, cancellationToken), cancellationToken))
			{
				installJob2 = await instanceClient.Engine.SetActiveVersion(new EngineVersionRequest
				{
					UploadCustomZip = true,
					EngineVersion = new EngineVersion
					{
						Version = compatVersion.Version,
						Engine = compatVersion.Engine,
						SourceSHA = compatVersion.SourceSHA,
					},
				}, stableBytesMs, cancellationToken);
			}

			await chatRequest;
			await Task.Yield();

			var jobs = await instanceClient.Jobs.List(null, cancellationToken);
			var theJobWeWant = jobs.First(x => x.Description.Contains("Reconnect chat bot"));

			await Task.WhenAll(
				jrt.WaitForJob(installJob2.InstallJob, EngineTest.EngineInstallationTimeout(compatVersion) + 30, false, null, cancellationToken),
				jrt.WaitForJob(cloneRequest.Result.ActiveJob, 60, false, null, cancellationToken),
				jrt.WaitForJob(theJobWeWant, 30, false, null, cancellationToken),
				dmUpdateRequest.AsTask(),
				cloneRequest);

			if (compatVersion.Engine.Value == EngineType.OpenDream)
			{
				Assert.IsNotNull(compatVersion.SourceSHA);
				var activeVersion = await instanceClient.Engine.ActiveVersion(cancellationToken);
				Assert.AreEqual(Limits.MaximumCommitShaLength, activeVersion.EngineVersion.SourceSHA.Length);
				Assert.AreEqual(compatVersion.SourceSHA, activeVersion.EngineVersion.SourceSHA);
				Assert.AreEqual(compatVersion.Version, activeVersion.EngineVersion.Version);
				Assert.AreEqual(compatVersion.Engine, activeVersion.EngineVersion.Engine);
				Assert.AreEqual(1, activeVersion.EngineVersion.CustomIteration);
			}

			var configSetupTask = new ConfigurationTest(instanceClient.Configuration, instanceClient.Metadata).SetupDMApiTests(cancellationToken);

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

			await new WatchdogTest(compatVersion, instanceClient, instanceManager, serverPort, highPrioDD, ddPort).Run(cancellationToken);

			await instanceManagerClient.Update(new InstanceUpdateRequest
			{
				Id = instanceClient.Metadata.Id,
				Online = false,
			}, cancellationToken);
			System.Console.WriteLine($"COMPAT TEST END: {compatVersion}");
		}
	}
}
