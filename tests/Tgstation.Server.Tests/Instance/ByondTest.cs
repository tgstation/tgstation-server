using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Api;
using Tgstation.Server.Client.Components;

namespace Tgstation.Server.Tests.Instance
{
	sealed class ByondTest : JobsRequiredTest
	{
		readonly IByondClient byondClient;

		public ByondTest(IByondClient byondClient, IJobsClient jobsClient)
			: base(jobsClient)
		{
			this.byondClient = byondClient ?? throw new ArgumentNullException(nameof(byondClient));
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
			var job = await WaitForJob(test.InstallJob, 60, false, cancellationToken).ConfigureAwait(false);
			var currentShit = await byondClient.ActiveVersion(cancellationToken).ConfigureAwait(false);
			Assert.AreEqual(newModel.Version.Semver(), currentShit.Version);
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
