using Castle.Core.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Tgstation.Server.Api;
using Tgstation.Server.Api.Models;
using Tgstation.Server.Api.Models.Request;
using Tgstation.Server.Client;
using Tgstation.Server.Client.Components;
using Tgstation.Server.Host.Components.Byond;
using Tgstation.Server.Host.IO;
using Tgstation.Server.Host.System;

namespace Tgstation.Server.Tests.Instance
{
	sealed class ByondTest : JobsRequiredTest
	{
		public static readonly Version TestVersion = new (513, 1536);

		readonly IByondClient byondClient;

		readonly Api.Models.Instance metadata;

		public ByondTest(IByondClient byondClient, IJobsClient jobsClient, Api.Models.Instance metadata)
			: base(jobsClient)
		{
			this.byondClient = byondClient ?? throw new ArgumentNullException(nameof(byondClient));
			this.metadata = metadata ?? throw new ArgumentNullException(nameof(metadata));
		}

		public async Task Run(CancellationToken cancellationToken)
		{
			await TestNoVersion(cancellationToken).ConfigureAwait(false);
			await TestInstallStable(cancellationToken).ConfigureAwait(false);
			await TestInstallFakeVersion(cancellationToken).ConfigureAwait(false);
			await TestCustomInstalls(cancellationToken);
		}

		async Task TestInstallFakeVersion(CancellationToken cancellationToken)
		{
			var newModel = new ByondVersionRequest
			{
				Version = new Version(5011, 1385)
			};
			var test = await byondClient.SetActiveVersion(newModel, null, cancellationToken).ConfigureAwait(false);
			Assert.IsNotNull(test.InstallJob);
			await WaitForJob(test.InstallJob, 60, true, ErrorCode.ByondDownloadFail, cancellationToken).ConfigureAwait(false);
		}

		async Task TestInstallStable(CancellationToken cancellationToken)
		{
			var newModel = new ByondVersionRequest
			{
				Version = TestVersion
			};
			var test = await byondClient.SetActiveVersion(newModel, null, cancellationToken).ConfigureAwait(false);
			Assert.IsNotNull(test.InstallJob);
			await WaitForJob(test.InstallJob, 180, false, null, cancellationToken).ConfigureAwait(false);
			var currentShit = await byondClient.ActiveVersion(cancellationToken).ConfigureAwait(false);
			Assert.AreEqual(newModel.Version.Semver(), currentShit.Version);

			var dreamMaker = "DreamMaker";
			if (new PlatformIdentifier().IsWindows)
				dreamMaker += ".exe";

			var dreamMakerDir = Path.Combine(metadata.Path, "Byond", newModel.Version.ToString(), "byond", "bin");

			Assert.IsTrue(Directory.Exists(dreamMakerDir), $"Directory {dreamMakerDir} does not exist!");
			Assert.IsTrue(File.Exists(Path.Combine(dreamMakerDir, dreamMaker)), $"Missing DreamMaker executable! Dir contents: {String.Join(", ", Directory.GetFileSystemEntries(dreamMakerDir))}");
		}

		async Task TestNoVersion(CancellationToken cancellationToken)
		{
			var allVersionsTask = byondClient.InstalledVersions(null, cancellationToken);
			var currentShit = await byondClient.ActiveVersion(cancellationToken).ConfigureAwait(false);
			Assert.IsNotNull(currentShit);
			Assert.IsNull(currentShit.Version);
			var otherShit = await allVersionsTask.ConfigureAwait(false);
			Assert.IsNotNull(otherShit);
			Assert.AreEqual(0, otherShit.Count);
		}

		async Task TestCustomInstalls(CancellationToken cancellationToken)
		{
			var byondInstaller = new PlatformIdentifier().IsWindows
				? (IByondInstaller)new WindowsByondInstaller(
					Mock.Of<IProcessExecutor>(),
					new DefaultIOManager(new AssemblyInformationProvider()),
					Mock.Of<ILogger<WindowsByondInstaller>>())
				: new PosixByondInstaller(
					Mock.Of<IPostWriteHandler>(),
					new DefaultIOManager(new AssemblyInformationProvider()),
					Mock.Of<ILogger<PosixByondInstaller>>());

			// get the bytes for stable
			using var stableBytesMs = await byondInstaller.DownloadVersion(TestVersion, cancellationToken);

			var test = await byondClient.SetActiveVersion(
				new ByondVersionRequest
				{
					Version = TestVersion,
					UploadCustomZip = true
				},
				stableBytesMs,
				cancellationToken)
				.ConfigureAwait(false);

			Assert.IsNotNull(test.InstallJob);
			await WaitForJob(test.InstallJob, 60, false, null, cancellationToken).ConfigureAwait(false);

			var newSettings = await byondClient.ActiveVersion(cancellationToken);
			Assert.AreEqual(new Version(TestVersion.Major, TestVersion.Minor, 1), newSettings.Version);

			// test a few switches
			var installResponse = await byondClient.SetActiveVersion(new ByondVersionRequest
			{
				Version = TestVersion
			}, null, cancellationToken);
			Assert.IsNull(installResponse.InstallJob);
			await ApiAssert.ThrowsException<ApiConflictException>(() => byondClient.SetActiveVersion(new ByondVersionRequest
			{
				Version = new Version(TestVersion.Major, TestVersion.Minor, 2)
			}, null, cancellationToken), ErrorCode.ByondNonExistentCustomVersion);

			installResponse = await byondClient.SetActiveVersion(new ByondVersionRequest
			{
				Version = new Version(TestVersion.Major, TestVersion.Minor, 1)
			}, null, cancellationToken);
			Assert.IsNull(installResponse.InstallJob);
		}
	}
}
