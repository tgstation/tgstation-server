using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Api;
using Tgstation.Server.Client.Components;

namespace Tgstation.Server.Tests.Instance
{
	sealed class ByondTest
	{
		readonly IByondClient byondClient;
		readonly IJobsClient jobsClient;

		public ByondTest(IByondClient byondClient, IJobsClient jobsClient)
		{
			this.byondClient = byondClient ?? throw new ArgumentNullException(nameof(byondClient));
			this.jobsClient = jobsClient ?? throw new ArgumentNullException(nameof(jobsClient));
		}

		public async Task Run(CancellationToken cancellationToken)
		{
			await TestNoVersion(cancellationToken).ConfigureAwait(false);
			await TestInstall511(cancellationToken).ConfigureAwait(false);
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
			var job = test.InstallJob;
			var maxWait = 60;   //it's 10MB max give me a break
			do
			{
				await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken).ConfigureAwait(false);
				job = await jobsClient.GetId(job, cancellationToken).ConfigureAwait(false);
				--maxWait;
			}
			while (!job.StoppedAt.HasValue && maxWait > 0);
			if (!job.StoppedAt.HasValue)
			{
				await jobsClient.Cancel(job, cancellationToken).ConfigureAwait(false);
				Assert.Fail("Byond installation job timed out!");
			}

			if (job.ExceptionDetails != null)
				Assert.Fail(job.ExceptionDetails);

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
