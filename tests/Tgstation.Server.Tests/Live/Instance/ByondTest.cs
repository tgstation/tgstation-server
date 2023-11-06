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
using Tgstation.Server.Host.Components.Byond;
using Tgstation.Server.Host.Configuration;
using Tgstation.Server.Host.IO;
using Tgstation.Server.Host.System;

namespace Tgstation.Server.Tests.Live.Instance
{
	sealed class ByondTest : JobsRequiredTest
	{
		readonly IByondClient byondClient;
		readonly IFileDownloader fileDownloader;

		readonly Api.Models.Instance metadata;

		static Version edgeVersion;

		Version testVersion;

		public ByondTest(IByondClient byondClient, IJobsClient jobsClient, IFileDownloader fileDownloader, Api.Models.Instance metadata)
			: base(jobsClient)
		{
			this.byondClient = byondClient ?? throw new ArgumentNullException(nameof(byondClient));
			this.fileDownloader = fileDownloader ?? throw new ArgumentNullException(nameof(fileDownloader));
			this.metadata = metadata ?? throw new ArgumentNullException(nameof(metadata));
		}

		public Task Run(CancellationToken cancellationToken, out Task firstInstall)
		{
			firstInstall = RunPartOne(cancellationToken);
			return RunContinued(firstInstall, cancellationToken);
		}

		public static async Task<Version> GetEdgeVersion(IFileDownloader fileDownloader, CancellationToken cancellationToken)
		{
			if (edgeVersion != null)
				return edgeVersion;

			await using var provider = fileDownloader.DownloadFile(new Uri("https://www.byond.com/download/version.txt"), null);
			var stream = await provider.GetResult(cancellationToken);
			using var reader = new StreamReader(stream, Encoding.UTF8, false, -1, true);
			var text = await reader.ReadToEndAsync();
			var splits = text.Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

			var targetVersion = splits.Last();

			var badVersionMap = new PlatformIdentifier().IsWindows
				? new Dictionary<string, string>()
				{
				}
				// linux map also needs updating in CI
				: new Dictionary<string, string>()
				{
					{ "515.1612", "515.1611" }
				};

			badVersionMap.Add("515.1617", "515.1616");

			if (badVersionMap.TryGetValue(targetVersion, out var remappedVersion))
				targetVersion = remappedVersion;

			return edgeVersion = Version.Parse(targetVersion);
		}

		async Task RunPartOne(CancellationToken cancellationToken)
		{
			testVersion = await GetEdgeVersion(fileDownloader, cancellationToken);
			await TestNoVersion(cancellationToken);
			await TestInstallStable(cancellationToken);
		}

		async Task RunContinued(Task firstInstall, CancellationToken cancellationToken)
		{
			await firstInstall;
			await TestInstallFakeVersion(cancellationToken);
			await TestCustomInstalls(cancellationToken);
			await TestDeletes(cancellationToken);
		}

		async Task TestDeletes(CancellationToken cancellationToken)
		{
			var deleteThisOneBecauseItWasntPartOfTheOriginalTest = await byondClient.DeleteVersion(new ByondVersionDeleteRequest
			{
				Version = new(testVersion.Major, testVersion.Minor, 2)
			}, cancellationToken);
			await WaitForJob(deleteThisOneBecauseItWasntPartOfTheOriginalTest, 30, false, null, cancellationToken);

			var nonExistentUninstallResponseTask = ApiAssert.ThrowsException<ConflictException, JobResponse>(() => byondClient.DeleteVersion(
				new ByondVersionDeleteRequest
				{
					Version = new(509, 1000)
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
					Version = new(testVersion.Major, testVersion.Minor, 1)
				},
				cancellationToken), ErrorCode.ByondCannotDeleteActiveVersion);

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
				Version = new Version(5011, 1385)
			};
			var test = await byondClient.SetActiveVersion(newModel, null, cancellationToken);
			Assert.IsNotNull(test.InstallJob);
			await WaitForJob(test.InstallJob, 60, true, ErrorCode.ByondDownloadFail, cancellationToken);
		}

		async Task TestInstallStable(CancellationToken cancellationToken)
		{
			var newModel = new ByondVersionRequest
			{
				Version = testVersion
			};
			var test = await byondClient.SetActiveVersion(newModel, null, cancellationToken);
			Assert.IsNotNull(test.InstallJob);
			await WaitForJob(test.InstallJob, 180, false, null, cancellationToken);
			var currentShit = await byondClient.ActiveVersion(cancellationToken);
			Assert.AreEqual(newModel.Version.Semver(), currentShit.Version);

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

			IByondInstaller byondInstaller = new PlatformIdentifier().IsWindows
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
			using var stableBytesMs = await byondInstaller.DownloadVersion(testVersion, cancellationToken);

			var test = await byondClient.SetActiveVersion(
				new ByondVersionRequest
				{
					Version = testVersion,
					UploadCustomZip = true
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
					UploadCustomZip = true
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
				Version = testVersion
			}, null, cancellationToken);
			Assert.IsNull(installResponse.InstallJob);
			await ApiAssert.ThrowsException<ApiConflictException, ByondInstallResponse>(() => byondClient.SetActiveVersion(new ByondVersionRequest
			{
				Version = new Version(testVersion.Major, testVersion.Minor, 3)
			}, null, cancellationToken), ErrorCode.ByondNonExistentCustomVersion);

			installResponse = await byondClient.SetActiveVersion(new ByondVersionRequest
			{
				Version = new Version(testVersion.Major, testVersion.Minor, 1)
			}, null, cancellationToken);
			Assert.IsNull(installResponse.InstallJob);
		}
	}
}
