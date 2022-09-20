using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Tgstation.Server.Api.Models;
using Tgstation.Server.Api.Models.Response;
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

		public async Task<JobResponse> WaitForJob(JobResponse originalJob, int timeout, bool? expectFailure, ErrorCode? expectedCode, CancellationToken cancellationToken)
		{
			var job = originalJob;
			do
			{
				await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
				job = await JobsClient.GetId(job, cancellationToken);
				--timeout;
			}
			while (!job.StoppedAt.HasValue && timeout > 0);

			if (!job.StoppedAt.HasValue)
			{
				await JobsClient.Cancel(job, cancellationToken);
				Assert.Fail($"Job ID {job.Id} \"{job.Description}\" timed out!");
			}

			if(expectFailure.HasValue && (expectFailure.Value ^ job.ExceptionDetails != null))
				Assert.Fail(job.ExceptionDetails
					?? $"Expected job \"{job.Id}\" \"{job.Description}\" to fail {(expectedCode.HasValue ? $"with ErrorCode \"{expectedCode.Value}\" " : String.Empty)}but it didn't");

			if (expectedCode.HasValue)
				Assert.AreEqual(expectedCode.Value, job.ErrorCode, job.ExceptionDetails);

			return job;
		}

		protected async Task<JobResponse> WaitForJobProgressThenCancel(JobResponse originalJob, int timeout, CancellationToken cancellationToken)
		{
			var job = originalJob;
			do
			{
				await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
				job = await JobsClient.GetId(job, cancellationToken);
				--timeout;
			}
			while (!job.Progress.HasValue && timeout > 0);

			if (job.StoppedAt.HasValue)
			{
				await JobsClient.Cancel(job, cancellationToken);
				Assert.Fail($"Job ID {job.Id} \"{job.Description}\" completed when we wanted it to just progress!");
			}

			if (job.ExceptionDetails != null)
				Assert.Fail(job.ExceptionDetails);

			await JobsClient.Cancel(job, cancellationToken);
			return await WaitForJob(job, timeout, false, null, cancellationToken);
		}
	}
}
