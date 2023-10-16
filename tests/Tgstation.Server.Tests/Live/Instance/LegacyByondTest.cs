using System;
using System.IO;
using System.Threading.Tasks;
using System.Threading;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Moq;

using Tgstation.Server.Api.Models.Internal;
using Tgstation.Server.Api.Models.Request;
using Tgstation.Server.Api.Models.Response;
using Tgstation.Server.Api.Models;
using Tgstation.Server.Client;
using Tgstation.Server.Common.Extensions;
using Tgstation.Server.Host.Components.Engine;
using Tgstation.Server.Host.Configuration;

using Tgstation.Server.Host.IO;

using Tgstation.Server.Host.System;
using Tgstation.Server.Host.Controllers.Legacy.Models;
using Tgstation.Server.Client.Components;

namespace Tgstation.Server.Tests.Live.Instance
{
	internal class LegacyByondTest : JobsRequiredTest
	{
		readonly LegacyByondClient byondClient;
		readonly Version testVersion;
		readonly Api.Models.Instance metadata;

		readonly IFileDownloader fileDownloader;

		public LegacyByondTest(IJobsClient jobsClient, IFileDownloader fileDownloader, LegacyByondClient byondClient, Version testVersion, Api.Models.Instance metadata)
			: base(jobsClient)
		{
			this.fileDownloader = fileDownloader ?? throw new ArgumentNullException(nameof(fileDownloader));
			this.byondClient = byondClient ?? throw new ArgumentNullException(nameof(byondClient));
			this.testVersion = testVersion ?? throw new ArgumentNullException(nameof(testVersion));
			this.metadata = metadata ?? throw new ArgumentNullException(nameof(metadata));
		}

		public async Task Run(CancellationToken cancellationToken)
		{
			await TestNoVersion(cancellationToken);
			await TestInstallNullVersion(cancellationToken);
			await TestInstallStable(cancellationToken);
			await TestInstallFakeVersion(cancellationToken);
			await TestCustomInstalls(cancellationToken);
			await TestDeletes(cancellationToken);
		}

		ValueTask TestInstallNullVersion(CancellationToken cancellationToken)
			=> ApiAssert.ThrowsException<ApiConflictException, ByondInstallResponse>(
				() => byondClient.SetActiveVersion(
					new ByondVersionRequest(),
					null,
					cancellationToken),
				ErrorCode.ModelValidationFailure);

		async Task TestDeletes(CancellationToken cancellationToken)
		{
			var deleteThisOneBecauseItWasntPartOfTheOriginalTest = await byondClient.DeleteVersion(
				new ByondVersionDeleteRequest
				{
					Version = new Version(testVersion.Major, testVersion.Minor, 2),
				}, cancellationToken);
			await WaitForJob(deleteThisOneBecauseItWasntPartOfTheOriginalTest, 30, false, null, cancellationToken);

			var nonExistentUninstallResponseTask = ApiAssert.ThrowsException<ConflictException, JobResponse>(() => byondClient.DeleteVersion(
				new ByondVersionDeleteRequest
				{
					Version = new(509, 1000),
				},
				cancellationToken), ErrorCode.ResourceNotPresent);

			var uninstallResponseTask = byondClient.DeleteVersion(
				new ByondVersionDeleteRequest
				{
					Version = testVersion
				},
				cancellationToken);

			var badBecauseActiveResponseTask = ApiAssert.ThrowsException<ConflictException, JobResponse>(() => byondClient.DeleteVersion(
				new ByondVersionDeleteRequest
				{
					Version = new Version(testVersion.Major, testVersion.Minor, 1),
				},
				cancellationToken), ErrorCode.EngineCannotDeleteActiveVersion);

			await badBecauseActiveResponseTask;

			var uninstallJob = await uninstallResponseTask;
			Assert.IsNotNull(uninstallJob);

			// Has to wait on deployment test possibly
			var uninstallTask = WaitForJob(uninstallJob, 120, false, null, cancellationToken);

			await nonExistentUninstallResponseTask;

			await uninstallTask;
			var byondDir = Path.Combine(metadata.Path, "Byond", testVersion.ToString());
			Assert.IsFalse(Directory.Exists(byondDir));

			var newVersions = await byondClient.InstalledVersions(null, cancellationToken);
			Assert.IsNotNull(newVersions);
			Assert.AreEqual(1, newVersions.Count);
			Assert.AreEqual(new Version(testVersion.Major, testVersion.Minor, 1), newVersions[0].Version);
		}

		async Task TestInstallFakeVersion(CancellationToken cancellationToken)
		{
			var newModel = new ByondVersionRequest
			{
				Version = new Version(5011, 1385),
			};

			var test = await byondClient.SetActiveVersion(newModel, null, cancellationToken);
			Assert.IsNotNull(test.InstallJob);
			await WaitForJob(test.InstallJob, 60, true, ErrorCode.EngineDownloadFail, cancellationToken);
		}

		async Task TestInstallStable(CancellationToken cancellationToken)
		{
			var newModel = new ByondVersionRequest
			{
				Version = testVersion,
			};
			var test = await byondClient.SetActiveVersion(newModel, null, cancellationToken);
			Assert.IsNotNull(test.InstallJob);
			await WaitForJob(test.InstallJob, 180, false, null, cancellationToken);
			var currentShit = await byondClient.ActiveVersion(cancellationToken);
			Assert.AreEqual(newModel.Version.Semver(), currentShit.Version.Semver());

			var dreamMaker = "DreamMaker";
			if (new PlatformIdentifier().IsWindows)
				dreamMaker += ".exe";

			var dreamMakerDir = Path.Combine(metadata.Path, "Byond", newModel.Version.ToString(), "byond", "bin");

			Assert.IsTrue(Directory.Exists(dreamMakerDir), $"Directory {dreamMakerDir} does not exist!");
			Assert.IsTrue(
				File.Exists(
					Path.Combine(dreamMakerDir, dreamMaker)),
				$"Missing DreamMaker executable! Dir contents: {string.Join(", ", Directory.GetFileSystemEntries(dreamMakerDir))}");
		}

		async Task TestNoVersion(CancellationToken cancellationToken)
		{
			var allVersionsTask = byondClient.InstalledVersions(null, cancellationToken);
			var currentShit = await byondClient.ActiveVersion(cancellationToken);
			Assert.IsNotNull(currentShit);
			Assert.IsNull(currentShit.Version);
			var otherShit = await allVersionsTask;
			Assert.IsNotNull(otherShit);
			Assert.AreEqual(0, otherShit.Count);
		}

		async Task TestCustomInstalls(CancellationToken cancellationToken)
		{
			var generalConfigOptionsMock = new Mock<IOptions<GeneralConfiguration>>();
			generalConfigOptionsMock.SetupGet(x => x.Value).Returns(new GeneralConfiguration());
			var sessionConfigOptionsMock = new Mock<IOptions<SessionConfiguration>>();
			sessionConfigOptionsMock.SetupGet(x => x.Value).Returns(new SessionConfiguration());

			var assemblyInformationProvider = new AssemblyInformationProvider();

			IEngineInstaller byondInstaller = new PlatformIdentifier().IsWindows
				? new WindowsByondInstaller(
					Mock.Of<IProcessExecutor>(),
					Mock.Of<IIOManager>(),
					fileDownloader,
					generalConfigOptionsMock.Object,
					Mock.Of<ILogger<WindowsByondInstaller>>())
				: new PosixByondInstaller(
					Mock.Of<IPostWriteHandler>(),
					Mock.Of<IIOManager>(),
					fileDownloader,
					Mock.Of<ILogger<PosixByondInstaller>>());

			using var windowsByondInstaller = byondInstaller as WindowsByondInstaller;

			// get the bytes for stable
			await using var stableBytesMs = await TestingUtils.ExtractMemoryStreamFromInstallationData(await byondInstaller.DownloadVersion(new EngineVersion
			{
				Version = testVersion,
				Engine = EngineType.Byond,
			}, null, cancellationToken), cancellationToken);

			var test = await byondClient.SetActiveVersion(
				new ByondVersionRequest
				{
					Version = testVersion,
					UploadCustomZip = true,
				},
				stableBytesMs,
				cancellationToken);

			Assert.IsNotNull(test.InstallJob);
			await WaitForJob(test.InstallJob, 30, false, null, cancellationToken);

			// do it again. #1501
			stableBytesMs.Seek(0, SeekOrigin.Begin);
			var test2 = await byondClient.SetActiveVersion(
				new ByondVersionRequest
				{
					Version = testVersion,
					UploadCustomZip = true,
				},
				stableBytesMs,
				cancellationToken);

			Assert.IsNotNull(test2.InstallJob);
			await WaitForJob(test2.InstallJob, 30, false, null, cancellationToken);

			var newSettings = await byondClient.ActiveVersion(cancellationToken);
			Assert.AreEqual(new Version(testVersion.Major, testVersion.Minor, 2), newSettings.Version);

			// test a few switches
			var installResponse = await byondClient.SetActiveVersion(new ByondVersionRequest
			{
				Version = testVersion,
			}, null, cancellationToken);
			Assert.IsNull(installResponse.InstallJob);
			await ApiAssert.ThrowsException<ApiConflictException, ByondInstallResponse>(() => byondClient.SetActiveVersion(new ByondVersionRequest
			{
				Version = new Version(testVersion.Major, testVersion.Minor, 3),
			}, null, cancellationToken), ErrorCode.EngineNonExistentCustomVersion);

			installResponse = await byondClient.SetActiveVersion(new ByondVersionRequest
			{
				Version = new Version(testVersion.Major, testVersion.Minor, 1),
			}, null, cancellationToken);
			Assert.IsNull(installResponse.InstallJob);
		}
	}
}
