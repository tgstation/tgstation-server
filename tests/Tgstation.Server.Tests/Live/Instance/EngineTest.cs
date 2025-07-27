using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
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
using Tgstation.Server.Common.Extensions;
using Tgstation.Server.Host.Components.Engine;
using Tgstation.Server.Host.Configuration;
using Tgstation.Server.Host.IO;
using Tgstation.Server.Host.System;

namespace Tgstation.Server.Tests.Live.Instance
{
	sealed class EngineTest(IEngineClient engineClient, IJobsClient jobsClient, IFileDownloader fileDownloader, Api.Models.Instance metadata, EngineType engineType) : JobsRequiredTest(jobsClient)
	{
		readonly IEngineClient engineClient = engineClient ?? throw new ArgumentNullException(nameof(engineClient));
		readonly IFileDownloader fileDownloader = fileDownloader ?? throw new ArgumentNullException(nameof(fileDownloader));

		readonly Api.Models.Instance metadata = metadata ?? throw new ArgumentNullException(nameof(metadata));

		static readonly Dictionary<EngineType, EngineVersion> edgeVersions = new ()
		{
			{ EngineType.Byond, null },
			{ EngineType.OpenDream, null }
		};

		EngineVersion testVersion;
		readonly EngineType testEngine = engineType;

		public Task Run(ILogger logger, CancellationToken cancellationToken, out Task firstInstall)
		{
			firstInstall = RunPartOne(logger, cancellationToken);
			return RunContinued(firstInstall, cancellationToken);
		}

		public static async ValueTask<EngineVersion> GetEdgeVersion(EngineType engineType, ILogger logger, IFileDownloader fileDownloader, CancellationToken cancellationToken)
		{
			var edgeVersion = edgeVersions[engineType];

			if (edgeVersion != null)
				return edgeVersion;

			EngineVersion engineVersion;
			if (engineType == EngineType.Byond)
			{
				var targetVersion = await TestingUtils.GetByondEdgeVersion(logger, fileDownloader, cancellationToken);

				Assert.IsTrue(EngineVersion.TryParse(targetVersion, out engineVersion), $"Bad version: {targetVersion}");
			}
			else if (engineType == EngineType.OpenDream)
			{
				var forcedVersion = Environment.GetEnvironmentVariable("TGS_TEST_OD_ENGINE_VERSION");
				if (!String.IsNullOrWhiteSpace(forcedVersion))
				{
					engineVersion = new EngineVersion
					{
						Engine = EngineType.OpenDream,
						SourceSHA = forcedVersion,
					};
				}
				else
				{
					var masterBranch = await TestingGitHubService.RealClient.Repository.Branch.Get("OpenDreamProject", "OpenDream", "master");

					engineVersion = new EngineVersion
					{
						Engine = EngineType.OpenDream,
						SourceSHA = masterBranch.Commit.Sha,
					};
				}
			}
			else
			{
				Assert.Fail($"Unimplemented edge retrieval for engine type: {engineType}");
				return null;
			}

			global::System.Console.WriteLine($"Edge {engineType} version evalutated to {engineVersion}");
			return edgeVersions[engineType] = engineVersion;
		}

		async Task RunPartOne(ILogger logger, CancellationToken cancellationToken)
		{
			testVersion = await GetEdgeVersion(testEngine, logger, fileDownloader, cancellationToken);
			await TestNoVersion(cancellationToken);
			await TestInstallNullVersion(cancellationToken);
			await TestInstallStable(cancellationToken);
		}

		ValueTask TestInstallNullVersion(CancellationToken cancellationToken)
			=> ApiAssert.ThrowsException<ApiConflictException, EngineInstallResponse>(
				() => engineClient.SetActiveVersion(
					new EngineVersionRequest
					{
						EngineVersion = new EngineVersion
						{
							Engine = testEngine,
						}
					},
					null,
					cancellationToken),
				ErrorCode.ModelValidationFailure);
		public static int EngineInstallationTimeout(EngineVersion testVersion)
			=> testVersion.Engine.Value switch
			{
				EngineType.Byond => 30,
				EngineType.OpenDream => 500,
				_ => throw new InvalidOperationException($"Unknown engine type: {testVersion.Engine.Value}"),
			};

		int EngineInstallationTimeout() => EngineInstallationTimeout(testVersion);

		async Task RunContinued(Task firstInstall, CancellationToken cancellationToken)
		{
			await firstInstall;
			await TestInstallFakeVersion(cancellationToken);
			await TestCustomInstalls(cancellationToken);
			await TestDeletes(cancellationToken);
		}

		async Task TestDeletes(CancellationToken cancellationToken)
		{
			var deleteThisOneBecauseItWasntPartOfTheOriginalTest = await engineClient.DeleteVersion(new EngineVersionDeleteRequest
			{
				EngineVersion = new EngineVersion
				{
					Engine = testEngine,
					Version = testVersion.Version,
					CustomIteration = 2,
				}
			}, cancellationToken);
			await WaitForJob(deleteThisOneBecauseItWasntPartOfTheOriginalTest, EngineInstallationTimeout(), false, null, cancellationToken);

			var nonExistentUninstallResponseTask = ApiAssert.ThrowsException<ConflictException, JobResponse>(() => engineClient.DeleteVersion(
				new EngineVersionDeleteRequest
				{
					EngineVersion = new EngineVersion
					{
						Version = new(509, 1000),
						Engine = testEngine,
					}
				},
				cancellationToken), ErrorCode.ResourceNotPresent);

			var uninstallResponseTask = engineClient.DeleteVersion(
				new EngineVersionDeleteRequest
				{
					EngineVersion = new EngineVersion
					{
						Version = testVersion.Version,
						Engine = testVersion.Engine,
						SourceSHA = testVersion.SourceSHA,
					}
				},
				cancellationToken);

			var badBecauseActiveResponseTask = ApiAssert.ThrowsException<ConflictException, JobResponse>(() => engineClient.DeleteVersion(
				new EngineVersionDeleteRequest
				{
					EngineVersion = new EngineVersion
					{
						Version = testVersion.Version,
						Engine = testVersion.Engine,
						SourceSHA = testVersion.SourceSHA,
						CustomIteration = 1,
					}
				},
				cancellationToken), ErrorCode.EngineCannotDeleteActiveVersion);

			await badBecauseActiveResponseTask;

			var uninstallJob = await uninstallResponseTask;
			Assert.IsNotNull(uninstallJob);

			// Has to wait on deployment test possibly
			var uninstallTask = WaitForJob(uninstallJob, EngineInstallationTimeout() + 90, false, null, cancellationToken);

			await nonExistentUninstallResponseTask;

			await uninstallTask;
			var byondDir = Path.Combine(metadata.Path, "Byond", testVersion.ToString());
			Assert.IsFalse(Directory.Exists(byondDir));

			var newVersions = await engineClient.InstalledVersions(null, cancellationToken);
			Assert.IsNotNull(newVersions);
			Assert.AreEqual(1, newVersions.Count);
			Assert.AreEqual(testVersion.Version.Semver(), newVersions[0].EngineVersion.Version.Semver());
			Assert.AreEqual(1, newVersions[0].EngineVersion.CustomIteration);
		}

		async Task TestInstallFakeVersion(CancellationToken cancellationToken)
		{
			var newModel = new EngineVersionRequest
			{
				EngineVersion = new EngineVersion
				{
					Version = new Version(5011, 1385),
				}
			};

			await ApiAssert.ThrowsException<ApiConflictException, EngineInstallResponse>(() => engineClient.SetActiveVersion(newModel, null, cancellationToken), ErrorCode.ModelValidationFailure);

			newModel.EngineVersion.Engine = testEngine;

			var test = await engineClient.SetActiveVersion(newModel, null, cancellationToken);
			Assert.IsNotNull(test.InstallJob);
			await WaitForJob(test.InstallJob, EngineInstallationTimeout() + 30, true, ErrorCode.EngineDownloadFail, cancellationToken);
		}

		async Task TestInstallStable(CancellationToken cancellationToken)
		{
			var newModel = new EngineVersionRequest
			{
				EngineVersion = new EngineVersion
				{
					Version = testVersion.Version,
					Engine = testVersion.Engine,
					SourceSHA = testVersion.SourceSHA,
				}
			};
			var test = await engineClient.SetActiveVersion(newModel, null, cancellationToken);
			Assert.IsNotNull(test.InstallJob);
			await WaitForJob(test.InstallJob, EngineInstallationTimeout() + 150, false, null, cancellationToken);
			var currentShit = await engineClient.ActiveVersion(cancellationToken);
			Assert.AreEqual(newModel.EngineVersion, currentShit.EngineVersion);
			Assert.IsFalse(currentShit.EngineVersion.CustomIteration.HasValue);

			var dreamMaker = "DreamMaker";
			if (new PlatformIdentifier().IsWindows)
				dreamMaker += ".exe";

			var dreamMakerDir = Path.Combine(metadata.Path, "Byond", newModel.EngineVersion.Version.ToString(), "byond", "bin");

			Assert.IsTrue(Directory.Exists(dreamMakerDir), $"Directory {dreamMakerDir} does not exist!");
			Assert.IsTrue(
				File.Exists(
					Path.Combine(dreamMakerDir, dreamMaker)),
				$"Missing DreamMaker executable! Dir contents: {string.Join(", ", Directory.GetFileSystemEntries(dreamMakerDir))}");
		}

		async Task TestNoVersion(CancellationToken cancellationToken)
		{
			var allVersionsTask = engineClient.InstalledVersions(null, cancellationToken);
			var currentShit = await engineClient.ActiveVersion(cancellationToken);
			Assert.IsNotNull(currentShit);
			Assert.IsNull(currentShit.EngineVersion);
			var otherShit = await allVersionsTask;
			Assert.IsNotNull(otherShit);
			Assert.AreEqual(0, otherShit.Count);
		}

		async Task TestCustomInstalls(CancellationToken cancellationToken)
		{
			var generalConfigOptionsMock = new Mock<IOptionsMonitor<GeneralConfiguration>>();
			generalConfigOptionsMock.SetupGet(x => x.CurrentValue).Returns(new GeneralConfiguration
			{
				ByondZipDownloadTemplate = TestingUtils.ByondZipDownloadTemplate,
			});
			var sessionConfigOptionsMock = new Mock<IOptions<SessionConfiguration>>();
			sessionConfigOptionsMock.SetupGet(x => x.Value).Returns(new SessionConfiguration());

			var assemblyInformationProvider = new AssemblyInformationProvider();

			IEngineInstaller byondInstaller = new PlatformIdentifier().IsWindows
				? new WindowsByondInstaller(
					Mock.Of<IProcessExecutor>(),
					Mock.Of<IIOManager>(),
					fileDownloader,
					generalConfigOptionsMock.Object,
					sessionConfigOptionsMock.Object,
					Mock.Of<ILogger<WindowsByondInstaller>>())
				: new PosixByondInstaller(
					Mock.Of<IPostWriteHandler>(),
					Mock.Of<IIOManager>(),
					fileDownloader,
					generalConfigOptionsMock.Object,
					Mock.Of<ILogger<PosixByondInstaller>>());

			using var windowsByondInstaller = byondInstaller as WindowsByondInstaller;

			// get the bytes for stable
			await using var stableBytesMs = await TestingUtils.ExtractMemoryStreamFromInstallationData(await byondInstaller.DownloadVersion(testVersion, null, cancellationToken), cancellationToken);

			var test = await engineClient.SetActiveVersion(
				new EngineVersionRequest
				{
					EngineVersion = new EngineVersion
					{
						Engine = testVersion.Engine,
						Version = testVersion.Version,
						SourceSHA = testVersion.SourceSHA,
					},
					UploadCustomZip = true,
				},
				stableBytesMs,
				cancellationToken);

			Assert.IsNotNull(test.InstallJob);
			await WaitForJob(test.InstallJob, EngineInstallationTimeout(), false, null, cancellationToken);

			// do it again. #1501
			stableBytesMs.Seek(0, SeekOrigin.Begin);
			var test2 = await engineClient.SetActiveVersion(
				new EngineVersionRequest
				{
					EngineVersion = new EngineVersion
					{
						Version = testVersion.Version,
						SourceSHA = testVersion.SourceSHA,
						Engine = testVersion.Engine,
					},
					UploadCustomZip = true,

				},
				stableBytesMs,
				cancellationToken);

			Assert.IsNotNull(test2.InstallJob);
			await WaitForJob(test2.InstallJob, EngineInstallationTimeout(), false, null, cancellationToken);

			var newSettings = await engineClient.ActiveVersion(cancellationToken);
			Assert.AreEqual(new Version(testVersion.Version.Major, testVersion.Version.Minor, 0), newSettings.EngineVersion.Version);
			Assert.AreEqual(2, newSettings.EngineVersion.CustomIteration);

			// test a few switches
			var installResponse = await engineClient.SetActiveVersion(new EngineVersionRequest
			{
				EngineVersion = new EngineVersion
				{
					Version = testVersion.Version,
					SourceSHA = testVersion.SourceSHA,
					Engine = testVersion.Engine,
				}
			}, null, cancellationToken);
			Assert.IsNull(installResponse.InstallJob);
			await ApiAssert.ThrowsException<ApiConflictException, EngineInstallResponse>(() => engineClient.SetActiveVersion(new EngineVersionRequest
			{
				EngineVersion = new EngineVersion
				{
					Version = testVersion.Version,
					Engine = testEngine,
					CustomIteration = 3,
				}
			}, null, cancellationToken), ErrorCode.EngineNonExistentCustomVersion);

			installResponse = await engineClient.SetActiveVersion(new EngineVersionRequest
			{
				EngineVersion = new EngineVersion
				{
					Version = new Version(testVersion.Version.Major, testVersion.Version.Minor),
					Engine = testEngine,
					CustomIteration = 1,
				}
			}, null, cancellationToken);
			Assert.IsNull(installResponse.InstallJob);
		}
	}
}
