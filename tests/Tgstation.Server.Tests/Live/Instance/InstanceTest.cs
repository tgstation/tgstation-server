using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Moq;

using Tgstation.Server.Api.Models;
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
using Tgstation.Server.Host.Jobs;
using Tgstation.Server.Host.System;
using Tgstation.Server.Host.Utils;

namespace Tgstation.Server.Tests.Live.Instance
{
	sealed class InstanceTest(IInstanceManagerClient instanceManagerClient, IFileDownloader fileDownloader, InstanceManager instanceManager, ushort serverPort)
	{
		readonly IInstanceManagerClient instanceManagerClient = instanceManagerClient ?? throw new ArgumentNullException(nameof(instanceManagerClient));
		readonly IFileDownloader fileDownloader = fileDownloader ?? throw new ArgumentNullException(nameof(fileDownloader));
		readonly InstanceManager instanceManager = instanceManager ?? throw new ArgumentNullException(nameof(instanceManager));
		readonly ushort serverPort = serverPort;

		public async Task RunTests(
			ILogger logger,
			IInstanceClient instanceClient,
			ushort dmPort,
			ushort ddPort,
			bool highPrioDD,
			bool lowPrioDeployment,
			bool usingBasicWatchdog,
			CancellationToken cancellationToken)
		{
			var testVersion = await EngineTest.GetEdgeVersion(EngineType.Byond, logger, fileDownloader, cancellationToken);
			await using var engineTest = new EngineTest(instanceClient.Engine, instanceClient.Jobs, fileDownloader, instanceClient.Metadata, testVersion.Engine.Value);
			await using var chatTest = new ChatTest(instanceClient.ChatBots, instanceManagerClient, instanceClient.Jobs, instanceClient.Metadata);
			var configTest = new ConfigurationTest(instanceClient.Configuration, instanceClient.Metadata);
			await using var repoTest = new RepositoryTest(instanceClient, instanceClient.Repository, instanceClient.Jobs);
			await using var dmTest = new DeploymentTest(instanceClient, instanceClient.Jobs, dmPort, ddPort, lowPrioDeployment, testVersion);

			var byondTask = engineTest.Run(logger, cancellationToken, out var firstInstall);
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

			await using var wdt = new WatchdogTest(
				testVersion,
				instanceClient,
				instanceManager,
				serverPort,
				highPrioDD,
				ddPort,
				usingBasicWatchdog);
			await wdt.Run(cancellationToken);

			await wdt.ExpectGameDirectoryCount(
				usingBasicWatchdog || new PlatformIdentifier().IsWindows
					? 2 // old + new deployment
					: 3, // + new mirrored deployment waiting to take over Live
				cancellationToken);
		}

		public static async ValueTask<IEngineInstallationData> DownloadEngineVersion(
			EngineVersion compatVersion,
			IFileDownloader fileDownloader,
			Uri openDreamUrl,
			CancellationToken cancellationToken)
		{
			var ioManager = new DefaultIOManager(new FileSystem());
			var odRepoDir = ioManager.ConcatPath(
				Environment.GetFolderPath(
					Environment.SpecialFolder.LocalApplicationData,
					Environment.SpecialFolderOption.DoNotVerify),
				new AssemblyInformationProvider().VersionPrefix,
				"OpenDreamRepository");
			var odRepoIoManager = ioManager.CreateResolverForSubdirectory(odRepoDir);

			var mockOptionsMonitor = new Mock<IOptionsMonitor<GeneralConfiguration>>();
			var genConfig = new GeneralConfiguration
			{
				OpenDreamGitUrl = openDreamUrl,
				ByondZipDownloadTemplate = TestingUtils.ByondZipDownloadTemplate,
			};
			mockOptionsMonitor.SetupGet(x => x.CurrentValue).Returns(genConfig);
			IEngineInstaller byondInstaller =
				compatVersion.Engine == EngineType.OpenDream
				? new OpenDreamInstaller(
					ioManager,
					Mock.Of<ILogger<OpenDreamInstaller>>(),
					new PlatformIdentifier(),
					Mock.Of<IProcessExecutor>(),
					new RepositoryManager(
						new LibGit2RepositoryFactory(
							Mock.Of<ILogger<LibGit2RepositoryFactory>>()),
						new LibGit2Commands(),
						odRepoIoManager,
						new NoopEventConsumer(),
						Mock.Of<IPostWriteHandler>(),
						Mock.Of<IGitRemoteFeaturesFactory>(),
						mockOptionsMonitor.Object,
						Mock.Of<ILogger<Repository>>(),
						Mock.Of<ILogger<RepositoryManager>>()),
					Mock.Of<IAsyncDelayer>(),
					Mock.Of<IHttpClientFactory>(),
					mockOptionsMonitor.Object,
					Mock.Of<IOptionsMonitor<SessionConfiguration>>())
				: new PlatformIdentifier().IsWindows
					? new WindowsByondInstaller(
						Mock.Of<IProcessExecutor>(),
						Mock.Of<IIOManager>(),
						fileDownloader,
						mockOptionsMonitor.Object,
						Mock.Of<IOptionsMonitor<SessionConfiguration>>(),
						Mock.Of<ILogger<WindowsByondInstaller>>())
					: new PosixByondInstaller(
						Mock.Of<IPostWriteHandler>(),
						Mock.Of<IIOManager>(),
						fileDownloader,
						mockOptionsMonitor.Object,
						Mock.Of<ILogger<PosixByondInstaller>>());

			using var windowsByondInstaller = byondInstaller as WindowsByondInstaller;

			// get the bytes for stable
			return await byondInstaller.DownloadVersion(compatVersion, new JobProgressReporter(), cancellationToken);
		}

		public async Task RunCompatTests(
			EngineVersion compatVersion,
			Uri openDreamUrl,
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
			}, cancellationToken).AsTask();

			async Task UpdateDMSettings()
			{
				const int Limit = 10;
				for (var i = 0; i < Limit; ++i)
					try
					{
						if (i != 0)
						{
							global::System.Console.WriteLine($"PORT REUSE BUG 6: Setting I-{instanceClient.Metadata.Id} DM to {dmPort}");
						}
						await instanceClient.DreamMaker.Update(new DreamMakerRequest
						{
							ApiValidationPort = dmPort,
						}, cancellationToken);
					}
					catch (ConflictException ex) when (ex.ErrorCode == ErrorCode.PortNotAvailable && i < (Limit - 1))
					{
						// I have no idea why this happens sometimes
						await Task.Delay(TimeSpan.FromSeconds(3), cancellationToken);
					}
			}

			var dmUpdateRequest = UpdateDMSettings();

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

			await using var jrt = new JobsRequiredTest(instanceClient.Jobs);

			await jrt.HubConnectionTask;

			EngineInstallResponse installJob2;
			await using (var stableBytesMs = await TestingUtils.ExtractMemoryStreamFromInstallationData(
				await DownloadEngineVersion(compatVersion, fileDownloader, openDreamUrl, cancellationToken),
				cancellationToken))
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

			await Task.WhenAll(
				jrt.WaitForJob(installJob2.InstallJob, EngineTest.EngineInstallationTimeout(compatVersion) + 30, false, null, cancellationToken),
				jrt.WaitForJob(cloneRequest.Result.ActiveJob, 60, false, null, cancellationToken),
				dmUpdateRequest,
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

			var jobs = await instanceClient.Jobs.List(null, cancellationToken);
			var theJobWeWant = jobs
				.OrderByDescending(x => x.StartedAt)
				.First(x => x.Description.Contains("Reconnect chat bot"));
			await jrt.WaitForJob(theJobWeWant, 30, false, null, cancellationToken);

			var configSetupTask = new ConfigurationTest(instanceClient.Configuration, instanceClient.Metadata).SetupDMApiTests(true, cancellationToken);

			if (!String.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("TGS_TEST_GITHUB_TOKEN"))
				&& !(Boolean.TryParse(Environment.GetEnvironmentVariable("TGS_TEST_OD_EXCLUSIVE"), out var odExclusive) && odExclusive))
				await instanceClient.Repository.Update(new RepositoryUpdateRequest
				{
					CreateGitHubDeployments = true,
					PostTestMergeComment = true,
					PushTestMergeCommits = true,
					AccessUser = "Cyberboss",
					AccessToken = Environment.GetEnvironmentVariable("TGS_TEST_GITHUB_TOKEN"),
				}, cancellationToken);

			await configSetupTask;

			await using var wdt = new WatchdogTest(compatVersion, instanceClient, instanceManager, serverPort, highPrioDD, ddPort, usingBasicWatchdog);
			await wdt.Run(cancellationToken);

			await instanceClient.DreamDaemon.Shutdown(cancellationToken);

			await wdt.ExpectGameDirectoryCount(
				1, // current deployment
				cancellationToken);

			await instanceManagerClient.Update(new InstanceUpdateRequest
			{
				Id = instanceClient.Metadata.Id,
				Online = false,
			}, cancellationToken);
			System.Console.WriteLine($"COMPAT TEST END: {compatVersion}");
		}
	}
}
