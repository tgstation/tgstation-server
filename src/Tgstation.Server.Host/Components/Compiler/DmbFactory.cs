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
			newerDmbTcs = new TaskCompletionSource<object>();
			cleanupCts = new CancellationTokenSource();
			jobLockCounts = new Dictionary<long, int>();
		}

		/// <inheritdoc />
		public void Dispose() => cleanupCts.Dispose();  //we don't dispose nextDmbProvider here, since it might be the only thing we have

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
				//lock (this)	//already locked below
				otherTask = cleanupTask;
				await Task.WhenAll(otherTask, deleteJob).ConfigureAwait(false);
			}
			lock (this)
			{
				if (!jobLockCounts.TryGetValue(job.Id, out var currentVal) || currentVal == 1)
				{
					jobLockCounts.Remove(job.Id);
					logger.LogDebug("Cleaning compile job {0} => {1}", job.Id, job.DirectoryName);
					cleanupTask = HandleCleanup();
				}
				else
				{
					var decremented = --jobLockCounts[job.Id];
					logger.LogTrace("Compile job {0} lock count now: {1}", job.Id, decremented);
				}
			}
		}

		/// <inheritdoc />
		public async Task LoadCompileJob(CompileJob job, CancellationToken cancellationToken)
		{
			if (job == null)
				throw new ArgumentNullException(nameof(job));

			logger.LogTrace("Loading compile job {0}...", job.Id);

			CompileJob finalCompileJob = null;
			//now load the entire compile job tree
			await databaseContextFactory.UseContext(async db => finalCompileJob = await db.CompileJobs.Where(x => x.Id == job.Id)
				.Include(x => x.Job).ThenInclude(x => x.StartedBy)
				.Include(x => x.RevisionInformation).ThenInclude(x => x.PrimaryTestMerge).ThenInclude(x => x.MergedBy)
				.Include(x => x.RevisionInformation).ThenInclude(x => x.ActiveTestMerges).ThenInclude(x => x.TestMerge).ThenInclude(x => x.MergedBy)
				.FirstAsync(cancellationToken).ConfigureAwait(false)).ConfigureAwait(false);   //can't wait to see that query

			var newProvider = await FromCompileJob(finalCompileJob, cancellationToken).ConfigureAwait(false);
			if (newProvider == null)
				return;
			lock (this)
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
			lock (this)
			{
				var jobId = nextDmbProvider.CompileJob.Id;
				var incremented = jobLockCounts[jobId] += lockCount;
				logger.LogTrace("Compile job {0} lock count now: {1}", jobId, incremented);
				return nextDmbProvider;
			}
		}

		/// <inheritdoc />
		public Task StartAsync(CancellationToken cancellationToken) => databaseContextFactory.UseContext(async (db) =>
		{
			//where complete clause not necessary, only successful COMPILEjobs get in the db
			var cj = await db.CompileJobs.Where(x => x.Job.Instance.Id == instance.Id)
				.Include(x => x.Job).ThenInclude(x => x.StartedBy)
				.Include(x => x.RevisionInformation).ThenInclude(x => x.PrimaryTestMerge).ThenInclude(x => x.MergedBy)
				.Include(x => x.RevisionInformation).ThenInclude(x => x.ActiveTestMerges).ThenInclude(x => x.TestMerge).ThenInclude(x => x.MergedBy)
				.OrderByDescending(x => x.Job.StoppedAt).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
			if (cj == default(CompileJob))
				return;
			await LoadCompileJob(cj, cancellationToken).ConfigureAwait(false);
			//we dont do CleanUnusedCompileJobs here because the watchdog may have plans for them yet
		});

		/// <inheritdoc />
		public async Task StopAsync(CancellationToken cancellationToken)
		{
			using (cancellationToken.Register(() => cleanupCts.Cancel()))
				await cleanupTask.ConfigureAwait(false);
		}

		/// <inheritdoc />
		public async Task<IDmbProvider> FromCompileJob(CompileJob compileJob, CancellationToken cancellationToken)
		{
			logger.LogTrace("Loading compile job {0}...", compileJob.Id);
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
					return null;    //omae wa mou shinderu
				}

				lock (this)
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
				jobUidsToNotErase = await db.CompileJobs.Where(x => x.Job.Instance.Id == instance.Id && jobIdsToSkip.Contains(x.Id)).Select(x => x.DirectoryName.Value.ToString().ToUpperInvariant()).ToListAsync(cancellationToken).ConfigureAwait(false);
			}).ConfigureAwait(false);

			//add the other exemption
			if (exceptThisOne != null)
				jobUidsToNotErase.Add(exceptThisOne.DirectoryName.Value.ToString().ToUpperInvariant());

			//cleanup
			await ioManager.CreateDirectory(".", cancellationToken).ConfigureAwait(false);
			var directories = await ioManager.GetDirectories(".", cancellationToken).ConfigureAwait(false);
			int deleting = 0;
			var tasks = directories.Select(async x =>
			{
				var nameOnly = ioManager.GetFileName(x);
				if (jobUidsToNotErase.Contains(nameOnly.ToUpperInvariant()))
					return;
				try
				{
					++deleting;
					await ioManager.DeleteDirectory(x, cancellationToken).ConfigureAwait(false);
				}
				catch (Exception e)
				{
					logger.LogWarning("Error deleting directory {0}! Exception: {1}", x, e);
				}
			}).ToList();
			if (deleting > 0)
			{
				logger.LogDebug("Cleaning {0} unused game folders...", deleting);
				await Task.WhenAll().ConfigureAwait(false);
			}
		}
	}
}
