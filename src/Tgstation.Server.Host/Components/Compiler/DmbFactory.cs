using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Host.Core;
using Tgstation.Server.Host.IO;
using Tgstation.Server.Host.Models;

namespace Tgstation.Server.Host.Components.Compiler
{
	/// <summary>
	/// Standard <see cref="IDmbFactory"/>
	/// </summary>
	sealed class DmbFactory : IDmbFactory, ICompileJobConsumer
	{
		/// <inheritdoc />
		public Task OnNewerDmb
		{
			get
			{
				lock (this)
					return newerDmbTcs.Task;
			}
		}

		/// <summary>
		/// The <see cref="IDatabaseContextFactory"/> for the <see cref="DmbFactory"/>
		/// </summary>
		readonly IDatabaseContextFactory databaseContextFactory;
		/// <summary>
		/// The <see cref="IIOManager"/> for the <see cref="DmbFactory"/>
		/// </summary>
		readonly IIOManager ioManager;
		/// <summary>
		/// The <see cref="ILogger"/> for the <see cref="DmbFactory"/>
		/// </summary>
		readonly ILogger<DmbFactory> logger;

		/// <summary>
		/// The <see cref="Api.Models.Instance"/> for the <see cref="DmbFactory"/>
		/// </summary>
		readonly Api.Models.Instance instance;
		/// <summary>
		/// The <see cref="CancellationTokenSource"/> for <see cref="cleanupTask"/>
		/// </summary>
		readonly CancellationTokenSource cleanupCts;

		/// <summary>
		/// <see cref="Task"/> representing calls to <see cref="CleanJob(CompileJob)"/>
		/// </summary>
		Task cleanupTask;
		/// <summary>
		/// <see cref="TaskCompletionSource{TResult}"/> resulting in the latest <see cref="DmbProvider"/> yet to exist
		/// </summary>
		TaskCompletionSource<IDmbProvider> newerDmbTcs;
		/// <summary>
		/// The latest <see cref="DmbProvider"/>
		/// </summary>
		IDmbProvider nextDmbProvider;

		Dictionary<long, int> jobLockCounts;

		/// <summary>
		/// Construct a <see cref="DmbFactory"/>
		/// </summary>
		/// <param name="databaseContextFactory">The value of <see cref="databaseContextFactory"/></param>
		/// <param name="ioManager">The value of <see cref="ioManager"/></param>
		/// <param name="logger">The value of <see cref="logger"/></param>
		/// <param name="instance">The value of <see cref="instance"/></param>
		public DmbFactory(IDatabaseContextFactory databaseContextFactory, IIOManager ioManager, ILogger<DmbFactory> logger, Api.Models.Instance instance)
		{
			this.databaseContextFactory = databaseContextFactory ?? throw new ArgumentNullException(nameof(databaseContextFactory));
			this.ioManager = ioManager ?? throw new ArgumentNullException(nameof(ioManager));
			this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
			this.instance = instance ?? throw new ArgumentNullException(nameof(instance));

			cleanupTask = Task.CompletedTask;
			cleanupCts = new CancellationTokenSource();
			jobLockCounts = new Dictionary<long, int>();
		}

		/// <inheritdoc />
		public void Dispose() => cleanupCts.Dispose();

		/// <summary>
		/// Delete the <see cref="Api.Models.Internal.CompileJob.DirectoryName"/> of <paramref name="job"/>
		/// </summary>
		/// <param name="job">The <see cref="CompileJob"/> to clean</param>
		void CleanJob(CompileJob job)
		{
			async Task HandleCleanup()
			{
				var deleteJob = ioManager.DeleteDirectory(job.DirectoryName.ToString(), cleanupCts.Token);
				Task otherTask;
				lock (this)
					otherTask = cleanupTask;
				await Task.WhenAll(otherTask, deleteJob).ConfigureAwait(false);
			}
			lock (this)
			{
				var currentVal = jobLockCounts[job.Id];
				if (--jobLockCounts[job.Id] == 0)
					cleanupTask = HandleCleanup();
			}
		}

		/// <inheritdoc />
		public Task LoadCompileJob(CompileJob job, CancellationToken cancellationToken) => LoadCompileJob(job, true, cancellationToken);

		async Task LoadCompileJob(CompileJob job, bool setAsStagedInDb, CancellationToken cancellationToken)
		{
			if (job == null)
				throw new ArgumentNullException(nameof(job));
			if (job.DMApiValidated != true || job.Job.Cancelled.Value || job.Job.ExceptionDetails != null || job.Job.StoppedAt == null)
				throw new InvalidOperationException("Cannot load incomplete compile job!");
			if (setAsStagedInDb)
				await databaseContextFactory.UseContext(async db =>
				{
					var ddsettings = new DreamDaemonSettings
					{
						InstanceId = instance.Id
					};
					db.DreamDaemonSettings.Attach(ddsettings);
					ddsettings.StagedCompileJob = job;
					await db.Save(cancellationToken).ConfigureAwait(false);
				}).ConfigureAwait(false);
			lock (this)
			{
				var oldDmbProvider = nextDmbProvider;
				if (oldDmbProvider != null && oldDmbProvider.CompileJob.Job.StoppedAt < oldDmbProvider.CompileJob.Job.StoppedAt)
					throw new InvalidOperationException("Loaded compile job older than current job!");
				nextDmbProvider = FromCompileJob(job);
				newerDmbTcs.SetResult(nextDmbProvider);
				newerDmbTcs = new TaskCompletionSource<IDmbProvider>();
			}
		}

		/// <inheritdoc />
		public async Task<IDmbProvider> LockNextDmb(CancellationToken cancellationToken)
		{
			Task<IDmbProvider> task;
			lock (this)
				if (nextDmbProvider != null)
					return nextDmbProvider;
				else
					task = newerDmbTcs.Task;

			return await task.ConfigureAwait(false);
		}

		/// <inheritdoc />
		public Task StartAsync(CancellationToken cancellationToken) => databaseContextFactory.UseContext(async (db) =>
		{
			//where complete clause not necessary, only successful COMPILEjobs get in the db
			var cj = await db.CompileJobs.Where(x => x.Job.Instance.Id == instance.Id).OrderByDescending(x => x.Job.StoppedAt).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
			if (cj == default(CompileJob))
				return;
			await LoadCompileJob(cj, false, cancellationToken).ConfigureAwait(false);

			//we dont do CleanUnusedCompileJobs here because the watchdog may have plans for them yet
		});

		/// <inheritdoc />
		public async Task StopAsync(CancellationToken cancellationToken)
		{
			using (cancellationToken.Register(() => cleanupCts.Cancel()))
				await cleanupTask.ConfigureAwait(false);
		}

		/// <inheritdoc />
		public IDmbProvider FromCompileJob(CompileJob compileJob)
		{
			lock (this)
			{
				if (!jobLockCounts.TryGetValue(compileJob.Id, out int value))
					jobLockCounts.Add(compileJob.Id, 1);
				else
					jobLockCounts[compileJob.Id] = ++value;
				return new DmbProvider(compileJob, ioManager, () => CleanJob(compileJob));
			}
		}

		/// <inheritdoc />
		public async Task CleanUnusedCompileJobs(CompileJob exceptThisOne, CancellationToken cancellationToken)
		{
			List<long> jobIdsToSkip;
			//don't clean locked directories
			lock (this)
				jobIdsToSkip = jobLockCounts.Select(x => x.Key).ToList();

			List<string> jobUidsToNotErase = null;

			//find the uids of locked directories
			await databaseContextFactory.UseContext(async db =>
			{
				jobUidsToNotErase = await db.CompileJobs.Where(x => x.Job.Instance.Id == instance.Id && jobIdsToSkip.Contains(x.Id) && x.DirectoryName.HasValue).Select(x => x.DirectoryName.Value.ToString().ToUpperInvariant()).ToListAsync(cancellationToken).ConfigureAwait(false);
			}).ConfigureAwait(false);

			//add the other exemption
			if (exceptThisOne != null)
				jobUidsToNotErase.Add(exceptThisOne.DirectoryName.Value.ToString().ToUpperInvariant());

			//cleanup
			var directories = await ioManager.GetDirectories(".", cancellationToken).ConfigureAwait(false);
			if (directories.Count > 0)
			{
				logger.LogDebug("Cleaning {0} unused game folders...", directories.Count);
				await Task.WhenAll(directories.Select(async x =>
				{
					var nameOnly = ioManager.GetFileName(x);
					if (jobUidsToNotErase.Contains(nameOnly.ToUpperInvariant()))
						return;
					try
					{
						await ioManager.DeleteDirectory(x, cancellationToken).ConfigureAwait(false);
					}
					catch (Exception e)
					{
						logger.LogWarning("Error deleting directory {0}! Exception: {1}", x, e);
					}
				})).ConfigureAwait(false);
			}
		}
	}
}
