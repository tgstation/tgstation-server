using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Host.Models;

namespace Tgstation.Server.Host.Core
{
	/// <inheritdoc />
	sealed class JobManager : IHostedService, IJobManager
	{
		/// <summary>
		/// The <see cref="IServiceProvider"/> for the <see cref="JobManager"/>
		/// </summary>
		readonly IServiceProvider serviceProvider;
		/// <summary>
		/// <see cref="Dictionary{TKey, TValue}"/> of <see cref="Api.Models.Internal.Job.Id"/> to running <see cref="JobHandler"/>s
		/// </summary>
		readonly Dictionary<long, JobHandler> jobs;

		/// <summary>
		/// Construct a <see cref="JobManager"/>
		/// </summary>
		/// <param name="serviceProvider">The value of <see cref="serviceProvider"/></param>
		public JobManager(IServiceProvider serviceProvider)
		{
			this.serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
			jobs = new Dictionary<long, JobHandler>();
		}

		/// <summary>
		/// Gets the <see cref="JobHandler"/> for a given <paramref name="job"/> if it exists
		/// </summary>
		/// <param name="job">The <see cref="Job"/> to get the <see cref="JobHandler"/> for</param>
		/// <returns>The <see cref="JobHandler"/></returns>
		JobHandler CheckGetJob(Job job)
		{
			lock (this)
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
		/// <param name="operation">The operation for the <paramref name="job"/></param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		async Task RunJob(Job job, Func<Job, IServiceProvider, CancellationToken, Task> operation, CancellationToken cancellationToken)
		{
			using (var scope = serviceProvider.CreateScope())
			{
				IDatabaseContext databaseContext = null;
				try
				{
					var oldJob = job;
					job = new Job { Id = oldJob.Id };
					try
					{
						await operation(job, scope.ServiceProvider, cancellationToken).ConfigureAwait(false);
					}
					finally
					{
						databaseContext = scope.ServiceProvider.GetRequiredService<IDatabaseContext>();
						databaseContext.Jobs.Attach(job);
					}
				}
				catch (OperationCanceledException)
				{
					job.Cancelled = true;
				}
				catch (Exception e)
				{
					job.ExceptionDetails = e.ToString();
				}
				job.StoppedAt = DateTimeOffset.Now;
				await databaseContext.Save(default).ConfigureAwait(false);
			}
		}

		/// <inheritdoc />
		public async Task RegisterOperation(Job job, Func<Job, IServiceProvider, CancellationToken, Task> operation, CancellationToken cancellationToken)
		{
			using (var scope = serviceProvider.CreateScope())
			{
				var databaseContext = scope.ServiceProvider.GetRequiredService<IDatabaseContext>();
				job.StartedAt = DateTimeOffset.Now;
				databaseContext.Jobs.Add(job);
				await databaseContext.Save(cancellationToken).ConfigureAwait(false);
				var jobHandler = JobHandler.Create(x => RunJob(job, operation, x));
				lock (this)
					jobs.Add(job.Id, jobHandler);
			}
		}

		/// <inheritdoc />
		public async Task StartAsync(CancellationToken cancellationToken)
		{
			using (var scope = serviceProvider.CreateScope())
			{
				var databaseContext = scope.ServiceProvider.GetRequiredService<IDatabaseContext>();
				await databaseContext.Initialize(cancellationToken).ConfigureAwait(false);

				//mark all jobs as cancelled
				var enumerator = await databaseContext.Jobs.Where(y => !y.Cancelled && y.StoppedAt == null).Select(y => y.Id).ToAsyncEnumerable().ToList(cancellationToken).ConfigureAwait(false);
				foreach(var I in enumerator)
				{
					var job = new Job { Id = I };
					databaseContext.Jobs.Attach(job);
					job.Cancelled = true;
				}
				await databaseContext.Save(cancellationToken).ConfigureAwait(false);
			}
		}

		/// <inheritdoc />
		public async Task StopAsync(CancellationToken cancellationToken)
		{
			var joinTasks = jobs.Select(x =>
			{
				x.Value.Cancel();
				return x.Value.Wait(cancellationToken);
			});
			await Task.WhenAll(joinTasks).ConfigureAwait(false);
			foreach (var job in jobs)
				job.Value.Dispose();
			jobs.Clear();
		}

		/// <inheritdoc />
		public async Task WaitForJob(Job job, CancellationToken cancellationToken)
		{
			var handler = CheckGetJob(job);
			await handler.Wait(cancellationToken).ConfigureAwait(false);
			lock (this)
				jobs.Remove(job.Id);
			handler.Dispose();
		}

		/// <inheritdoc />
		public void CancelJob(Job job) => CheckGetJob(job).Cancel();
	}
}
