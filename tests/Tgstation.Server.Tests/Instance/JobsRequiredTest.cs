using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Api.Models;
using Tgstation.Server.Client.Components;

namespace Tgstation.Server.Tests.Instance
{
	class JobsRequiredTest
	{
		protected IJobsClient JobsClient { get; }

		public JobsRequiredTest(IJobsClient jobsClient)
		{
			this.JobsClient = jobsClient;
		}

		public async Task<Job> WaitForJob(Job originalJob, int timeout, bool expectFailure, ErrorCode? expectedCode, CancellationToken cancellationToken)
		{
			var job = originalJob;
			do
			{
				await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken).ConfigureAwait(false);
				job = await JobsClient.GetId(job, cancellationToken).ConfigureAwait(false);
				--timeout;
			}
			while (!job.StoppedAt.HasValue && timeout > 0);

			if (!job.StoppedAt.HasValue)
			{
				await JobsClient.Cancel(job, cancellationToken).ConfigureAwait(false);
				Assert.Fail($"Job ID {job.Id} \"{job.Description}\" timed out!");
			}

			if (expectFailure ^ job.ExceptionDetails != null)
				Assert.Fail(job.ExceptionDetails ?? $"Expected job \"{job.Id}\" \"{job.Description}\" to fail but it didn't");

			if (expectedCode.HasValue)
				Assert.AreEqual(expectedCode.Value, job.ErrorCode, job.ExceptionDetails);

			return job;
		}

		protected async Task<Job> WaitForJobProgressThenCancel(Job originalJob, int timeout, CancellationToken cancellationToken)
		{
			var job = originalJob;
			do
			{
				await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken).ConfigureAwait(false);
				job = await JobsClient.GetId(job, cancellationToken).ConfigureAwait(false);
				--timeout;
			}
			while (!job.Progress.HasValue && timeout > 0);

			if (job.StoppedAt.HasValue)
			{
				await JobsClient.Cancel(job, cancellationToken).ConfigureAwait(false);
				Assert.Fail($"Job ID {job.Id} \"{job.Description}\" completed when we wanted it to just progress!");
			}

			if (job.ExceptionDetails != null)
				Assert.Fail(job.ExceptionDetails);

			await JobsClient.Cancel(job, cancellationToken);
			return await WaitForJob(job, timeout, false, null, cancellationToken).ConfigureAwait(false);
		}
	}
}
