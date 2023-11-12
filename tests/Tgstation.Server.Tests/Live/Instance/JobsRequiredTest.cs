using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Tgstation.Server.Api.Hubs;
using Tgstation.Server.Api.Models;
using Tgstation.Server.Api.Models.Response;
using Tgstation.Server.Client;
using Tgstation.Server.Client.Components;

namespace Tgstation.Server.Tests.Live.Instance
{
	class JobsRequiredTest
	{
		protected IJobsClient JobsClient { get; }

		readonly IApiClient apiClient;

		public JobsRequiredTest(IJobsClient jobsClient)
		{
			JobsClient = jobsClient ?? throw new ArgumentNullException(nameof(jobsClient));
			apiClient = (IApiClient)jobsClient.GetType().GetProperty("ApiClient", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(jobsClient);
		}

		class JobReceiver : IJobsHub
		{
			public Action<JobResponse> Callback { get; set; }

			public Task ReceiveJobUpdate(JobResponse job, CancellationToken cancellationToken)
			{
				Callback(job);
				return Task.CompletedTask;
			}
		}

		public async Task<JobResponse> WaitForJob(JobResponse originalJob, int timeout, bool? expectFailure, ErrorCode? expectedCode, CancellationToken cancellationToken)
		{
			Assert.IsNotNull(originalJob.Id);
			Assert.IsNotNull(originalJob.JobCode);

			var job = originalJob;
			if (!job.StoppedAt.HasValue)
			{
				var tcs = new TaskCompletionSource();
				var receiver = new JobReceiver
				{
					Callback = updatedJob =>
					{
						if (updatedJob.Id != job.Id)
							return;

						job = updatedJob;
						if (updatedJob.StoppedAt.HasValue)
							tcs.TrySetResult();
					},
				};

				JobResponse firstCheck;
				await using (var hubConnection = await apiClient.CreateHubConnection<IJobsHub>(receiver, null, null, cancellationToken))
				{
					// initial GET after connecting
					firstCheck = await JobsClient.GetId(job, cancellationToken);
					if (!firstCheck.StoppedAt.HasValue)
					{
						firstCheck = null;
						await Task.WhenAny(
							tcs.Task,
							Task.Delay(TimeSpan.FromSeconds(timeout), cancellationToken));
					}
				}

				if (firstCheck != null)
					job = firstCheck;
				else if (!job.StoppedAt.HasValue)
					// one last get in case SignalR dropped the ball
					job = await JobsClient.GetId(job, cancellationToken);

				if (!job.StoppedAt.HasValue)
				{
					await JobsClient.Cancel(job, cancellationToken);
					Assert.Fail($"Job ID {job.Id} \"{job.Description}\" timed out!");
				}
			}

			if (expectFailure.HasValue && expectFailure.Value ^ job.ExceptionDetails != null)
				Assert.Fail(job.ExceptionDetails
					?? $"Expected job \"{job.Id}\" \"{job.Description}\" to fail {(expectedCode.HasValue ? $"with ErrorCode \"{expectedCode.Value}\" " : string.Empty)}but it didn't");

			if (expectedCode.HasValue)
				Assert.AreEqual(expectedCode.Value, job.ErrorCode, job.ExceptionDetails);

			return job;
		}

		protected async Task<JobResponse> WaitForJobProgress(JobResponse originalJob, int timeout, CancellationToken cancellationToken)
		{
			Assert.IsNotNull(originalJob.Id);
			Assert.IsNotNull(originalJob.JobCode);
			var job = originalJob;
			do
			{
				await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
				job = await JobsClient.GetId(job, cancellationToken);
				Assert.IsNotNull(job.Id);
				Assert.IsNotNull(job.JobCode);
				--timeout;
			}
			while (!job.Progress.HasValue && job.Stage == null && timeout > 0);

			if (job.ExceptionDetails != null)
				Assert.Fail(job.ExceptionDetails);

			return job;
		}

		protected async Task<JobResponse> WaitForJobProgressThenCancel(JobResponse originalJob, int timeout, CancellationToken cancellationToken)
		{
			Assert.IsNotNull(originalJob.Id);
			Assert.IsNotNull(originalJob.JobCode);
			var start = DateTimeOffset.UtcNow;
			var job = await WaitForJobProgress(originalJob, timeout, cancellationToken);

			if (job.StoppedAt.HasValue)
				Assert.Fail($"Job ID {job.Id} \"{job.Description}\" completed when we wanted it to just progress!");

			if (job.ExceptionDetails != null)
				Assert.Fail(job.ExceptionDetails);

			await JobsClient.Cancel(job, cancellationToken);

			timeout -= (int)Math.Ceiling((DateTimeOffset.UtcNow - start).TotalSeconds);
			return await WaitForJob(job, timeout, false, null, cancellationToken);
		}
	}
}
