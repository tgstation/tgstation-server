using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Host.Database;
using Tgstation.Server.Host.Extensions;
using Tgstation.Server.Host.IO;
using Tgstation.Server.Host.Models;

namespace Tgstation.Server.Host.Components.Deployment
{
	/// <summary>
	/// Standard <see cref="IDmbFactory"/>
	/// </summary>
	sealed class DmbFactory : IDmbFactory, ICompileJobSink
	{
		/// <inheritdoc />
		public Task OnNewerDmb
		{
			get
			{
				lock (jobLockCounts)
					return newerDmbTcs.Task;
			}
		}

		/// <inheritdoc />
		public bool DmbAvailable => nextDmbProvider != null;

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
		/// Map of <see cref="CompileJob.JobId"/>s to locks on them.
		/// </summary>
		readonly IDictionary<long, int> jobLockCounts;

		/// <summary>
		/// <see cref="Task"/> representing calls to <see cref="CleanJob(CompileJob)"/>
		/// </summary>
		Task cleanupTask;

		/// <summary>
		/// <see cref="TaskCompletionSource{TResult}"/> resulting in the latest <see cref="DmbProvider"/> yet to exist
		/// </summary>
		TaskCompletionSource<object> newerDmbTcs;

		/// <summary>
		/// The latest <see cref="DmbProvider"/>
		/// </summary>
		IDmbProvider nextDmbProvider;

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
			newerDmbTcs = new TaskCompletionSource<object>();
			cleanupCts = new CancellationTokenSource();
			jobLockCounts = new Dictionary<long, int>();
		}

		/// <inheritdoc />
		public void Dispose() => cleanupCts.Dispose(); // we don't dispose nextDmbProvider here, since it might be the only thing we have

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

				// lock (this) //already locked below
				otherTask = cleanupTask;
				await Task.WhenAll(otherTask, deleteJob).ConfigureAwait(false);
			}

			lock (jobLockCounts)
				if (!jobLockCounts.TryGetValue(job.Id, out var currentVal) || currentVal == 1)
				{
					jobLockCounts.Remove(job.Id);
					logger.LogDebug("Cleaning lock-free compile job {0} => {1}", job.Id, job.DirectoryName);
					cleanupTask = HandleCleanup();
				}
				else
				{
					var decremented = --jobLockCounts[job.Id];
					logger.LogTrace("Compile job {0} lock count now: {1}", job.Id, decremented);
				}
		}

		/// <inheritdoc />
		public async Task LoadCompileJob(CompileJob job, CancellationToken cancellationToken)
		{
			if (job == null)
				throw new ArgumentNullException(nameof(job));

			var newProvider = await FromCompileJob(job, cancellationToken).ConfigureAwait(false);
			if (newProvider == null)
				return;
			lock (jobLockCounts)
			{
				nextDmbProvider?.Dispose();
				nextDmbProvider = newProvider;
				newerDmbTcs.SetResult(nextDmbProvider);
				newerDmbTcs = new TaskCompletionSource<object>();
			}
		}

		/// <inheritdoc />
		public IDmbProvider LockNextDmb(int lockCount)
		{
			if (!DmbAvailable)
				throw new InvalidOperationException("No .dmb available!");
			if (lockCount < 0)
				throw new ArgumentOutOfRangeException(nameof(lockCount), lockCount, "lockCount must be greater than or equal to 0!");
			lock (jobLockCounts)
			{
				var jobId = nextDmbProvider.CompileJob.Id;
				var incremented = jobLockCounts[jobId] += lockCount;
				logger.LogTrace("Compile job {0} lock count now: {1}", jobId, incremented);
				return nextDmbProvider;
			}
		}

		/// <inheritdoc />
		public async Task StartAsync(CancellationToken cancellationToken)
		{
			CompileJob cj = null;
			await databaseContextFactory.UseContext(async (db) =>
			{
				cj = await db
					.MostRecentCompletedCompileJobOrDefault(instance, cancellationToken)
					.ConfigureAwait(false);
			})
			.ConfigureAwait(false);

			if (cj == default(CompileJob))
				return;
			await LoadCompileJob(cj, cancellationToken).ConfigureAwait(false);

			// we dont do CleanUnusedCompileJobs here because the watchdog may have plans for them yet
		}

		/// <inheritdoc />
		public async Task StopAsync(CancellationToken cancellationToken)
		{
			using (cancellationToken.Register(() => cleanupCts.Cancel()))
				await cleanupTask.ConfigureAwait(false);
		}

		/// <inheritdoc />
		#pragma warning disable CA1506 // TODO: Decomplexify
		public async Task<IDmbProvider> FromCompileJob(CompileJob compileJob, CancellationToken cancellationToken)
		{
			if (compileJob == null)
				throw new ArgumentNullException(nameof(compileJob));

			// ensure we have the entire compile job tree
			logger.LogTrace("Loading compile job {0}...", compileJob.Id);
			await databaseContextFactory.UseContext(
				async db => compileJob = await db
					.CompileJobs
					.AsQueryable()
					.Where(x => x.Id == compileJob.Id)
					.Include(x => x.Job).ThenInclude(x => x.StartedBy)
					.Include(x => x.RevisionInformation).ThenInclude(x => x.PrimaryTestMerge).ThenInclude(x => x.MergedBy)
					.Include(x => x.RevisionInformation).ThenInclude(x => x.ActiveTestMerges).ThenInclude(x => x.TestMerge).ThenInclude(x => x.MergedBy)
					.FirstAsync(cancellationToken)
					.ConfigureAwait(false))
				.ConfigureAwait(false); // can't wait to see that query

			if (!compileJob.Job.StoppedAt.HasValue)
			{
				// This happens if we're told to load the compile job that is currently finished up
				// It can constitute an API violation if it's returned by the DreamDaemonController so just set it here
				// Bit of a hack, but it should work out to be the same value
				logger.LogTrace("Setting missing StoppedAt for CompileJob job...");
				compileJob.Job.StoppedAt = DateTimeOffset.Now;
			}

			var providerSubmitted = false;
			var newProvider = new DmbProvider(compileJob, ioManager, () =>
			{
				if (providerSubmitted)
					CleanJob(compileJob);
			});

			try
			{
				var primaryCheckTask = ioManager.FileExists(ioManager.ConcatPath(newProvider.PrimaryDirectory, newProvider.DmbName), cancellationToken);
				var secondaryCheckTask = ioManager.FileExists(ioManager.ConcatPath(newProvider.PrimaryDirectory, newProvider.DmbName), cancellationToken);

				if (!(await primaryCheckTask.ConfigureAwait(false) && await secondaryCheckTask.ConfigureAwait(false)))
				{
					logger.LogWarning("Error loading compile job, .dmb missing!");
					return null; // omae wa mou shinderu
				}

				lock (jobLockCounts)
				{
					if (!jobLockCounts.TryGetValue(compileJob.Id, out int value))
					{
						value = 1;
						jobLockCounts.Add(compileJob.Id, 1);
					}
					else
						jobLockCounts[compileJob.Id] = ++value;

					logger.LogTrace("Compile job {0} lock count now: {1}", compileJob.Id, value);

					providerSubmitted = true;
					return newProvider;
				}
			}
			finally
			{
				if (!providerSubmitted)
					newProvider.Dispose();
			}
		}
		#pragma warning restore CA1506

		/// <inheritdoc />
		#pragma warning disable CA1506 // TODO: Decomplexify
		public async Task CleanUnusedCompileJobs(CancellationToken cancellationToken)
		{
			List<long> jobIdsToSkip;

			// don't clean locked directories
			lock (jobLockCounts)
				jobIdsToSkip = jobLockCounts.Select(x => x.Key).ToList();

			List<string> jobUidsToNotErase = null;

			// find the uids of locked directories
			await databaseContextFactory.UseContext(async db =>
			{
				jobUidsToNotErase = (await db
					.CompileJobs
					.AsQueryable()
					.Where(
						x => x.Job.Instance.Id == instance.Id
						&& jobIdsToSkip.Contains(x.Id))
					.Select(x => x.DirectoryName.Value)
					.ToListAsync(cancellationToken)
					.ConfigureAwait(false))
					.Select(x => x.ToString())
					.ToList();
			}).ConfigureAwait(false);

			jobUidsToNotErase.Add(WindowsSwappableDmbProvider.LiveGameDirectory);

			logger.LogTrace("We will not clean the following directories: {0}", String.Join(", ", jobUidsToNotErase));

			// cleanup
			var gameDirectory = ioManager.ResolvePath();
			await ioManager.CreateDirectory(gameDirectory, cancellationToken).ConfigureAwait(false);
			var directories = await ioManager.GetDirectories(gameDirectory, cancellationToken).ConfigureAwait(false);
			int deleting = 0;
			var tasks = directories.Select(async x =>
			{
				var nameOnly = ioManager.GetFileName(x);
				if (jobUidsToNotErase.Contains(nameOnly))
					return;
				logger.LogDebug("Cleaning unused game folder: {0}...", nameOnly);
				try
				{
					++deleting;
					await ioManager.DeleteDirectory(x, cancellationToken).ConfigureAwait(false);
				}
				catch (OperationCanceledException)
				{
					throw;
				}
				catch (Exception e)
				{
					logger.LogWarning("Error deleting directory {0}! Exception: {1}", x, e);
				}
			}).ToList();
			if (deleting > 0)
				await Task.WhenAll(tasks).ConfigureAwait(false);
		}
		#pragma warning restore CA1506

		/// <inheritdoc />
		public CompileJob LatestCompileJob()
		{
			if (!DmbAvailable)
				return null;
			return LockNextDmb(0)?.CompileJob;
		}
	}
}
