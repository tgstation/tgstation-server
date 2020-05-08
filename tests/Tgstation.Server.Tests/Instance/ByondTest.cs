using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Api;
using Tgstation.Server.Client.Components;
using Tgstation.Server.Host.System;

namespace Tgstation.Server.Tests.Instance
{
	sealed class ByondTest : JobsRequiredTest
	{
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
			await TestInstall511(cancellationToken).ConfigureAwait(false);
			await TestInstallFakeVersion(cancellationToken).ConfigureAwait(false);
		}

		async Task TestInstallFakeVersion(CancellationToken cancellationToken)
		{
			var newModel = new Api.Models.Byond
			{
				Version = new Version(5011, 1385)
			};
			var test = await byondClient.SetActiveVersion(newModel, cancellationToken).ConfigureAwait(false);
			Assert.IsNotNull(test.InstallJob);
			await WaitForJob(test.InstallJob, 60, true, cancellationToken).ConfigureAwait(false);
		}

		async Task TestInstall511(CancellationToken cancellationToken)
		{
			var newModel = new Api.Models.Byond
			{
				Version = new Version(511, 1385)
			};
			var test = await byondClient.SetActiveVersion(newModel, cancellationToken).ConfigureAwait(false);
			Assert.IsNotNull(test.InstallJob);
			Assert.IsNull(test.Version);
			await WaitForJob(test.InstallJob, 60, false, cancellationToken).ConfigureAwait(false);
			var currentShit = await byondClient.ActiveVersion(cancellationToken).ConfigureAwait(false);
			Assert.AreEqual(newModel.Version.Semver(), currentShit.Version);

			var dreamMaker = "DreamMaker";
			if (new PlatformIdentifier().IsWindows)
				dreamMaker += ".exe";

			var dreamMakerDir = Path.Combine(metadata.Path, "Byond", "511.1385", "byond", "bin");

			Assert.IsTrue(Directory.Exists(dreamMakerDir), $"Directory {dreamMakerDir} does not exist!");
			Assert.IsTrue(File.Exists(Path.Combine(dreamMakerDir, dreamMaker)), $"Missing DreamMaker executable! Dir contents: {String.Join(", ", Directory.GetFileSystemEntries(dreamMakerDir))}");
		}

		async Task TestNoVersion(CancellationToken cancellationToken)
		{
			var allVersionsTask = byondClient.InstalledVersions(cancellationToken);
			var currentShit = await byondClient.ActiveVersion(cancellationToken).ConfigureAwait(false);
			Assert.IsNotNull(currentShit);
			Assert.IsNull(currentShit.InstallJob);
			Assert.IsNull(currentShit.Version);
			var otherShit = await allVersionsTask.ConfigureAwait(false);
			Assert.IsNotNull(otherShit);
			Assert.AreEqual(0, otherShit.Count);
		}
	}
}
