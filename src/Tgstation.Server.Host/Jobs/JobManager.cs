using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Serilog.Context;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Host.Components;
using Tgstation.Server.Host.Database;
using Tgstation.Server.Host.Extensions;
using Tgstation.Server.Host.Models;

namespace Tgstation.Server.Host.Jobs
{
	/// <inheritdoc />
	sealed class JobManager : IJobManager, IDisposable
	{
		/// <summary>
		/// The <see cref="IServiceProvider"/> for the <see cref="JobManager"/>
		/// </summary>
		readonly IDatabaseContextFactory databaseContextFactory;

		/// <summary>
		/// The <see cref="ILogger"/> for the <see cref="JobManager"/>
		/// </summary>
		readonly ILogger<JobManager> logger;

		/// <summary>
		/// The <see cref="IInstanceCoreProvider"/> for the <see cref="JobManager"/>.
		/// </summary>
		readonly Lazy<IInstanceCoreProvider> instanceCoreProvider;

		/// <summary>
		/// <see cref="Dictionary{TKey, TValue}"/> of <see cref="Job"/> <see cref="Api.Models.EntityId.Id"/>s to running <see cref="JobHandler"/>s
		/// </summary>
		readonly Dictionary<long, JobHandler> jobs;

		/// <summary>
		/// <see cref="TaskCompletionSource{TResult}"/> to delay starting jobs until the server is ready.
		/// </summary>
		readonly TaskCompletionSource<object> activationTcs;

		/// <summary>
		/// <see langword="lock"/> <see cref="object"/> for various operations.
		/// </summary>
		readonly object synchronizationLock;

		/// <summary>
		/// Construct a <see cref="JobManager"/>
		/// </summary>
		/// <param name="databaseContextFactory">The value of <see cref="databaseContextFactory"/></param>
		/// <param name="instanceCoreProvider">The value of <see cref="instanceCoreProvider"/>.</param>
		/// <param name="logger">The value of <see cref="logger"/></param>
		public JobManager(IDatabaseContextFactory databaseContextFactory, Lazy<IInstanceCoreProvider> instanceCoreProvider, ILogger<JobManager> logger)
		{
			this.databaseContextFactory = databaseContextFactory ?? throw new ArgumentNullException(nameof(databaseContextFactory));
			this.instanceCoreProvider = instanceCoreProvider ?? throw new ArgumentNullException(nameof(instanceCoreProvider));
			this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
			jobs = new Dictionary<long, JobHandler>();
			activationTcs = new TaskCompletionSource<object>();
			synchronizationLock = new object();
		}

		/// <inheritdoc />
		public void Dispose()
		{
			foreach (var job in jobs)
				job.Value.Dispose();
		}

		/// <summary>
		/// Gets the <see cref="JobHandler"/> for a given <paramref name="job"/> if it exists
		/// </summary>
		/// <param name="job">The <see cref="Job"/> to get the <see cref="JobHandler"/> for</param>
		/// <returns>The <see cref="JobHandler"/></returns>
		JobHandler CheckGetJob(Job job)
		{
			lock (synchronizationLock)
			{
				if (!jobs.TryGetValue(job.Id, out JobHandler jobHandler))
					throw new InvalidOperationException("Job not running!");
				return jobHandler;
			}
		}

		/// <summary>
		/// Runner for <see cref="JobHandler"/>s
		/// </summary>
		/// <param name="job">The <see cref="Job"/> being run</param>
		/// <param name="operation">The <see cref="JobEntrypoint"/> for the <paramref name="job"/></param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		async Task RunJob(Job job, JobEntrypoint operation, CancellationToken cancellationToken)
		{
			using (LogContext.PushProperty("Job", job.Id))
				try
				{
					void LogException(Exception ex) => logger.LogDebug(ex, "Job {0} exited with error!", job.Id);
					try
					{
						var oldJob = job;
						job = new Job { Id = oldJob.Id };

						void UpdateProgress(int progress)
						{
							lock (synchronizationLock)
								if (jobs.TryGetValue(oldJob.Id, out var handler))
									handler.Progress = progress;
						}

						await activationTcs.Task.WithToken(cancellationToken).ConfigureAwait(false);

						logger.LogTrace("Starting job...");
						await operation(
							instanceCoreProvider.Value.GetInstance(oldJob.Instance),
							databaseContextFactory,
							job,
							UpdateProgress,
							cancellationToken)
							.ConfigureAwait(false);

						logger.LogDebug("Job {0} completed!", job.Id);
					}
					catch (OperationCanceledException ex)
					{
						logger.LogDebug(ex, "Job {0} cancelled!", job.Id);
						job.Cancelled = true;
					}
					catch (JobException e)
					{
						job.ErrorCode = e.ErrorCode;
						job.ExceptionDetails = String.IsNullOrWhiteSpace(e.Message) ? e.InnerException?.Message : e.Message;
						LogException(e);
					}
					catch (Exception e)
					{
						job.ExceptionDetails = e.ToString();
						LogException(e);
					}

					await databaseContextFactory.UseContext(async databaseContext =>
					{
						var attachedJob = new Job
						{
							Id = job.Id
						};

						databaseContext.Jobs.Attach(attachedJob);
						attachedJob.StoppedAt = DateTimeOffset.UtcNow;
						attachedJob.ExceptionDetails = job.ExceptionDetails;
						attachedJob.ErrorCode = job.ErrorCode;
						attachedJob.Cancelled = job.Cancelled;

						// DCT: Cancellation token is for job, operation should always run
						await databaseContext.Save(default).ConfigureAwait(false);
					}).ConfigureAwait(false);
				}
				finally
				{
					lock (synchronizationLock)
					{
						var handler = jobs[job.Id];
						jobs.Remove(job.Id);
						handler.Dispose();
					}
				}
		}

		/// <inheritdoc />
		public Task RegisterOperation(Job job, JobEntrypoint operation, CancellationToken cancellationToken)
			=> databaseContextFactory.UseContext(
				async databaseContext =>
				{
					if (job == null)
						throw new ArgumentNullException(nameof(job));
					if (operation == null)
						throw new ArgumentNullException(nameof(operation));

					job.StartedAt = DateTimeOffset.UtcNow;
					job.Cancelled = false;

					job.Instance = new Models.Instance
					{
						Id = job.Instance.Id
					};
					databaseContext.Instances.Attach(job.Instance);

					if (job.StartedBy == null)
						job.StartedBy = await databaseContext
							.Users
							.GetTgsUser(cancellationToken)
							.ConfigureAwait(false);
					else
						job.StartedBy = new User
						{
							Id = job.StartedBy.Id
						};
					databaseContext.Users.Attach(job.StartedBy);

					databaseContext.Jobs.Add(job);

					await databaseContext.Save(cancellationToken).ConfigureAwait(false);

					logger.LogDebug("Registering job {0}: {1}...", job.Id, job.Description);
					var jobHandler = new JobHandler(jobCancellationToken => RunJob(job, operation, jobCancellationToken));
					try
					{
						lock (synchronizationLock)
							jobs.Add(job.Id, jobHandler);

						jobHandler.Start();
					}
					catch
					{
						jobHandler.Dispose();
						throw;
					}
				});

		/// <inheritdoc />
		public Task StartAsync(CancellationToken cancellationToken)
			=> databaseContextFactory.UseContext(async databaseContext =>
			{
				// mark all jobs as cancelled
				var badJobs = await databaseContext
					.Jobs
					.AsQueryable()
					.Where(y => !y.StoppedAt.HasValue)
					.Select(y => y.Id)
					.ToListAsync(cancellationToken)
					.ConfigureAwait(false);
				if (badJobs.Count > 0)
				{
					logger.LogTrace("Cleaning {0} unfinished jobs...", badJobs.Count);
					foreach (var I in badJobs)
					{
						var job = new Job { Id = I };
						databaseContext.Jobs.Attach(job);
						job.Cancelled = true;
						job.StoppedAt = DateTimeOffset.UtcNow;
					}

					await databaseContext.Save(cancellationToken).ConfigureAwait(false);
				}
			});

		/// <inheritdoc />
		public async Task StopAsync(CancellationToken cancellationToken)
		{
			var joinTasks = jobs.Select(x => CancelJob(
				new Job
				{
					Id = x.Key,
				},
				null,
				true,
				cancellationToken));
			await Task.WhenAll(joinTasks).ConfigureAwait(false);
		}

		/// <inheritdoc />
		public async Task<Job> CancelJob(Job job, User user, bool blocking, CancellationToken cancellationToken)
		{
			if (job == null)
				throw new ArgumentNullException(nameof(job));
			JobHandler handler;
			try
			{
				handler = CheckGetJob(job);
			}
			catch (InvalidOperationException)
			{
				// this is fine
				return null;
			}

			handler.Cancel(); // this will ensure the db update is only done once
			await databaseContextFactory.UseContext(async databaseContext =>
			{
				if (user == null)
					user = await databaseContext.Users.GetTgsUser(cancellationToken).ConfigureAwait(false);

				var updatedJob = new Job { Id = job.Id };
				databaseContext.Jobs.Attach(updatedJob);
				var attachedUser = new User { Id = user.Id };
				databaseContext.Users.Attach(attachedUser);
				updatedJob.CancelledBy = attachedUser;

				// let either startup or cancellation set job.cancelled
				await databaseContext.Save(cancellationToken).ConfigureAwait(false);
				job.CancelledBy = user;
			}).ConfigureAwait(false);

			if (blocking)
			{
				logger.LogTrace("Waiting on cancelled job #{0}...", job.Id);
				await handler.Wait(cancellationToken).ConfigureAwait(false);
				logger.LogTrace("Done waiting on job #{0}...", job.Id);
			}

			return job;
		}

		/// <inheritdoc />
		public int? JobProgress(Job job)
		{
			if (job == null)
				throw new ArgumentNullException(nameof(job));
			lock (synchronizationLock)
			{
				if (!jobs.TryGetValue(job.Id, out var handler))
					return null;
				return handler.Progress;
			}
		}

		/// <inheritdoc />
		public async Task WaitForJobCompletion(Job job, User canceller, CancellationToken jobCancellationToken, CancellationToken cancellationToken)
		{
			if (job == null)
				throw new ArgumentNullException(nameof(job));
			JobHandler handler;
			lock (synchronizationLock)
			{
				if (!jobs.TryGetValue(job.Id, out handler))
					return;
			}

			Task cancelTask = null;
			using (jobCancellationToken.Register(() => cancelTask = CancelJob(job, canceller, true, cancellationToken)))
				await handler.Wait(cancellationToken).ConfigureAwait(false);

			if (cancelTask != null)
				await cancelTask.ConfigureAwait(false);
		}

		/// <inheritdoc />
		public void Activate()
		{
			logger.LogTrace("Activating job manager...");
			activationTcs.SetResult(null);
		}
	}
}
