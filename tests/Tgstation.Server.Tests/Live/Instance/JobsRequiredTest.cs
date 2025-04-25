using System;
using System.Collections.Concurrent;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Tgstation.Server.Api.Hubs;
using Tgstation.Server.Api.Models;
using Tgstation.Server.Api.Models.Response;
using Tgstation.Server.Client;
using Tgstation.Server.Client.Components;

namespace Tgstation.Server.Tests.Live.Instance
{
	class JobsRequiredTest : IAsyncDisposable
	{
		public Task HubConnectionTask => hubConnection;

		protected IJobsClient JobsClient { get; }

		readonly IApiClient apiClient;

		IAsyncDisposable hubConnection;
		readonly Task hubConnectionTask;
		readonly CancellationTokenSource cancellationTokenSource;

		readonly ConcurrentDictionary<long, TaskCompletionSource<JobResponse>> registry;

		public JobsRequiredTest(IJobsClient jobsClient)
		{
			JobsClient = jobsClient ?? throw new ArgumentNullException(nameof(jobsClient));
			apiClient = (IApiClient)jobsClient.GetType().GetProperty("ApiClient", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(jobsClient);
			registry = new ConcurrentDictionary<long, TaskCompletionSource<JobResponse>>();

			cancellationTokenSource = new CancellationTokenSource();
			hubConnectionTask = CreateHubConnection();
		}

		async Task CreateHubConnection()
		{
			var receiver = new JobReceiver
			{
				Callback = job => Register(job),
			};

			hubConnection = await apiClient.CreateHubConnection<IJobsHub>(receiver, null, null, cancellationTokenSource.Token);
		}

		public async ValueTask DisposeAsync()
		{
			cancellationTokenSource.Cancel();
			cancellationTokenSource.Dispose();
			try
			{
				await hubConnectionTask;
			}
			catch (OperationCanceledException)
			{
			}

			if (hubConnection != null)
				await hubConnection.DisposeAsync();
		}

		Task<JobResponse> Register(JobResponse updatedJob)
		{
			var tcs = registry.AddOrUpdate(updatedJob.Id.Value,
				_ =>
				{
					var tcs = new TaskCompletionSource<JobResponse>();
					if (updatedJob.StoppedAt.HasValue)
						tcs.SetResult(updatedJob);
					return tcs;
				},
				(_, oldTcs) =>
				{
					if (updatedJob.StoppedAt.HasValue)
						oldTcs.TrySetResult(updatedJob);
					return oldTcs;
				});

			return tcs.Task;
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
			var registryTask = Register(job);
			await Task.WhenAny(
				registryTask,
				Task.Delay(TimeSpan.FromSeconds(timeout), cancellationToken));

			if (!registryTask.IsCompleted)
				// one last get in case SignalR dropped the ball
				job = await JobsClient.GetId(job, cancellationToken);
			else
				job = await registryTask;

			if (!job.StoppedAt.HasValue)
			{
				await JobsClient.Cancel(job, cancellationToken);
				Assert.Fail($"Job ID {job.Id} \"{job.Description}\" timed out!");
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

			timeout -= Math.Max(0, (int)Math.Ceiling((DateTimeOffset.UtcNow - start).TotalSeconds));
			timeout = Math.Max(0, timeout);
			return await WaitForJob(job, timeout, false, null, cancellationToken);
		}
	}
}
