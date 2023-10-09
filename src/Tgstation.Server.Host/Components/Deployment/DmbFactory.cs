using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using Tgstation.Server.Common.Extensions;
using Tgstation.Server.Host.Components.Deployment.Remote;
using Tgstation.Server.Host.Components.Events;
using Tgstation.Server.Host.Database;
using Tgstation.Server.Host.IO;
using Tgstation.Server.Host.Models;

namespace Tgstation.Server.Host.Components.Deployment
{
	/// <summary>
	/// Standard <see cref="IDmbFactory"/>.
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
		/// The <see cref="IDatabaseContextFactory"/> for the <see cref="DmbFactory"/>.
		/// </summary>
		readonly IDatabaseContextFactory databaseContextFactory;

		/// <summary>
		/// The <see cref="IIOManager"/> for the <see cref="DmbFactory"/>.
		/// </summary>
		readonly IIOManager ioManager;

		/// <summary>
		/// The <see cref="IRemoteDeploymentManagerFactory"/> for the <see cref="DmbFactory"/>.
		/// </summary>
		readonly IRemoteDeploymentManagerFactory remoteDeploymentManagerFactory;

		/// <summary>
		/// The <see cref="ILogger"/> for the <see cref="DmbFactory"/>.
		/// </summary>
		readonly ILogger<DmbFactory> logger;

		/// <summary>
		/// The <see cref="IEventConsumer"/> for <see cref="DmbFactory"/>.
		/// </summary>
		readonly IEventConsumer eventConsumer;

		/// <summary>
		/// The <see cref="Api.Models.Instance"/> for the <see cref="DmbFactory"/>.
		/// </summary>
		readonly Api.Models.Instance metadata;

		/// <summary>
		/// The <see cref="CancellationTokenSource"/> for <see cref="cleanupTask"/>.
		/// </summary>
		readonly CancellationTokenSource cleanupCts;

		/// <summary>
		/// Map of <see cref="CompileJob.JobId"/>s to locks on them.
		/// </summary>
		readonly IDictionary<long, int> jobLockCounts;

		/// <summary>
		/// <see cref="TaskCompletionSource"/> resulting in the latest <see cref="DmbProvider"/> yet to exist.
		/// </summary>
		volatile TaskCompletionSource newerDmbTcs;

		/// <summary>
		/// <see cref="Task"/> representing calls to <see cref="CleanRegisteredCompileJob(CompileJob)"/>.
		/// </summary>
		Task cleanupTask;

		/// <summary>
		/// The latest <see cref="DmbProvider"/>.
		/// </summary>
		IDmbProvider nextDmbProvider;

		/// <summary>
		/// If the <see cref="DmbFactory"/> is "started" via <see cref="IComponentService"/>.
		/// </summary>
		bool started;

		/// <summary>
		/// Initializes a new instance of the <see cref="DmbFactory"/> class.
		/// </summary>
		/// <param name="databaseContextFactory">The value of <see cref="databaseContextFactory"/>.</param>
		/// <param name="ioManager">The value of <see cref="ioManager"/>.</param>
		/// <param name="remoteDeploymentManagerFactory">The value of <see cref="remoteDeploymentManagerFactory"/>.</param>
		/// <param name="eventConsumer">The value of <see cref="eventConsumer"/>.</param>
		/// <param name="logger">The value of <see cref="logger"/>.</param>
		/// <param name="metadata">The value of <see cref="metadata"/>.</param>
		public DmbFactory(
			IDatabaseContextFactory databaseContextFactory,
			IIOManager ioManager,
			IRemoteDeploymentManagerFactory remoteDeploymentManagerFactory,
			IEventConsumer eventConsumer,
			ILogger<DmbFactory> logger,
			Api.Models.Instance metadata)
		{
			this.databaseContextFactory = databaseContextFactory ?? throw new ArgumentNullException(nameof(databaseContextFactory));
			this.ioManager = ioManager ?? throw new ArgumentNullException(nameof(ioManager));
			this.remoteDeploymentManagerFactory = remoteDeploymentManagerFactory ?? throw new ArgumentNullException(nameof(remoteDeploymentManagerFactory));
			this.eventConsumer = eventConsumer ?? throw new ArgumentNullException(nameof(eventConsumer));
			this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
			this.metadata = metadata ?? throw new ArgumentNullException(nameof(metadata));

			cleanupTask = Task.CompletedTask;
			newerDmbTcs = new TaskCompletionSource();
			cleanupCts = new CancellationTokenSource();
			jobLockCounts = new Dictionary<long, int>();
		}

		/// <inheritdoc />
		public void Dispose() => cleanupCts.Dispose(); // we don't dispose nextDmbProvider here, since it might be the only thing we have

		/// <inheritdoc />
		public async ValueTask LoadCompileJob(CompileJob job, Action<bool> activationAction, CancellationToken cancellationToken)
		{
			ArgumentNullException.ThrowIfNull(job);

			var newProvider = await FromCompileJob(job, cancellationToken);
			if (newProvider == null)
				return;

			// Do this first, because it's entirely possible when we set the tcs it will immediately need to be applied
			if (started)
			{
				var remoteDeploymentManager = remoteDeploymentManagerFactory.CreateRemoteDeploymentManager(
					metadata,
					job);
				await remoteDeploymentManager.StageDeployment(
					newProvider.CompileJob,
					activationAction,
					cancellationToken);
			}

			lock (jobLockCounts)
			{
				nextDmbProvider?.Dispose();
				nextDmbProvider = newProvider;

				// Oh god dammit
				var temp = Interlocked.Exchange(ref newerDmbTcs, new TaskCompletionSource());
				temp.SetResult();
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
				var incremented = jobLockCounts[jobId.Value] += lockCount;
				logger.LogTrace("Compile job {jobId} lock count now: {lockCount}", jobId, incremented);
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
					.CompileJobs
					.AsQueryable()
					.Where(x => x.Job.Instance.Id == metadata.Id)
					.OrderByDescending(x => x.Job.StoppedAt)
					.FirstOrDefaultAsync(cancellationToken);
			});

			try
			{
				if (cj == default(CompileJob))
					return;
				await LoadCompileJob(cj, null, cancellationToken);
			}
			finally
			{
				started = true;
			}

			// we dont do CleanUnusedCompileJobs here because the watchdog may have plans for them yet
		}

		/// <inheritdoc />
		public async Task StopAsync(CancellationToken cancellationToken)
		{
			try
			{
				lock (jobLockCounts)
					remoteDeploymentManagerFactory.ForgetLocalStateForCompileJobs(jobLockCounts.Keys);

				using (cancellationToken.Register(() => cleanupCts.Cancel()))
					await cleanupTask;
			}
			finally
			{
				started = false;
			}
		}

		/// <inheritdoc />
#pragma warning disable CA1506 // TODO: Decomplexify
		public async ValueTask<IDmbProvider> FromCompileJob(CompileJob compileJob, CancellationToken cancellationToken)
		{
			ArgumentNullException.ThrowIfNull(compileJob);

			// ensure we have the entire metadata tree
			logger.LogTrace("Loading compile job {id}...", compileJob.Id);
			await databaseContextFactory.UseContext(
				async db => compileJob = await db
					.CompileJobs
					.AsQueryable()
					.Where(x => x.Id == compileJob.Id)
					.Include(x => x.Job)
						.ThenInclude(x => x.StartedBy)
					.Include(x => x.RevisionInformation)
						.ThenInclude(x => x.PrimaryTestMerge)
						.ThenInclude(x => x.MergedBy)
					.Include(x => x.RevisionInformation)
						.ThenInclude(x => x.ActiveTestMerges)
						.ThenInclude(x => x.TestMerge)
						.ThenInclude(x => x.MergedBy)
					.FirstAsync(cancellationToken)); // can't wait to see that query

			if (!Api.Models.Internal.ByondVersion.TryParse(compileJob.ByondVersion, out var byondVersion))
			{
				logger.LogWarning("Error loading compile job, bad BYOND version: {0}", compileJob.ByondVersion);
				return null; // omae wa mou shinderu
			}

			if (!compileJob.Job.StoppedAt.HasValue)
			{
				// This happens when we're told to load the compile job that is currently finished up
				// It constitutes an API violation if it's returned by the DreamDaemonController so just set it here
				// Bit of a hack, but it works out to be nearly if not the same value that's put in the DB
				logger.LogTrace("Setting missing StoppedAt for CompileJob.Job #{id}...", compileJob.Job.Id);
				compileJob.Job.StoppedAt = DateTimeOffset.UtcNow;
			}

			var providerSubmitted = false;

			void CleanupAction()
			{
				if (providerSubmitted)
					CleanRegisteredCompileJob(compileJob);
			}

			var newProvider = new DmbProvider(compileJob, byondVersion, ioManager, CleanupAction);
			try
			{
				const string LegacyADirectoryName = "A";
				const string LegacyBDirectoryName = "B";

				var dmbExistsAtRoot = await ioManager.FileExists(
					ioManager.ConcatPath(
						newProvider.Directory,
						newProvider.DmbName),
					cancellationToken);

				if (!dmbExistsAtRoot)
				{
					logger.LogTrace("Didn't find .dmb at game directory root, checking A/B dirs...");
					var primaryCheckTask = ioManager.FileExists(
						ioManager.ConcatPath(
							newProvider.Directory,
							LegacyADirectoryName,
							newProvider.DmbName),
						cancellationToken);
					var secondaryCheckTask = ioManager.FileExists(
						ioManager.ConcatPath(
							newProvider.Directory,
							LegacyBDirectoryName,
							newProvider.DmbName),
						cancellationToken);

					if (!(await primaryCheckTask && await secondaryCheckTask))
					{
						logger.LogWarning("Error loading compile job, .dmb missing!");
						return null; // omae wa mou shinderu
					}

					// rebuild the provider because it's using the legacy style directories
					// Don't dispose it
					logger.LogDebug("Creating legacy two folder .dmb provider targeting {aDirName} directory...", LegacyADirectoryName);
					newProvider = new DmbProvider(compileJob, byondVersion, ioManager, CleanupAction, Path.DirectorySeparatorChar + LegacyADirectoryName);
				}

				lock (jobLockCounts)
				{
					if (!jobLockCounts.TryGetValue(compileJob.Id.Value, out int value))
					{
						value = 1;
						jobLockCounts.Add(compileJob.Id.Value, 1);
					}
					else
						jobLockCounts[compileJob.Id.Value] = ++value;

					providerSubmitted = true;

					logger.LogTrace("Compile job {id} lock count now: {lockCount}", compileJob.Id, value);
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
		public async ValueTask CleanUnusedCompileJobs(CancellationToken cancellationToken)
		{
			List<long> jobIdsToSkip;

			// don't clean locked directories
			lock (jobLockCounts)
				jobIdsToSkip = jobLockCounts.Keys.ToList();

			List<string> jobUidsToNotErase = null;

			// find the uids of locked directories
			if (jobIdsToSkip.Any())
			{
				await databaseContextFactory.UseContext(async db =>
				{
					jobUidsToNotErase = (await db
							.CompileJobs
							.AsQueryable()
							.Where(
								x => x.Job.Instance.Id == metadata.Id
								&& jobIdsToSkip.Contains(x.Id.Value))
							.Select(x => x.DirectoryName.Value)
							.ToListAsync(cancellationToken))
						.Select(x => x.ToString())
						.ToList();
				});
			}
			else
				jobUidsToNotErase = new List<string>();

			jobUidsToNotErase.Add(SwappableDmbProvider.LiveGameDirectory);

			logger.LogTrace("We will not clean the following directories: {directoriesToNotClean}", String.Join(", ", jobUidsToNotErase));

			// cleanup
			var gameDirectory = ioManager.ResolvePath();
			await ioManager.CreateDirectory(gameDirectory, cancellationToken);
			var directories = await ioManager.GetDirectories(gameDirectory, cancellationToken);
			int deleting = 0;
			var tasks = directories.Select(async x =>
			{
				var nameOnly = ioManager.GetFileName(x);
				if (jobUidsToNotErase.Contains(nameOnly))
					return;
				logger.LogDebug("Cleaning unused game folder: {dirName}...", nameOnly);
				try
				{
					++deleting;
					await DeleteCompileJobContent(x, cancellationToken);
				}
				catch (OperationCanceledException)
				{
					throw;
				}
				catch (Exception e)
				{
					logger.LogWarning(e, "Error deleting directory {dirName}!", x);
				}
			}).ToList();
			if (deleting > 0)
				await Task.WhenAll(tasks);
		}
#pragma warning restore CA1506

		/// <inheritdoc />
		public CompileJob LatestCompileJob()
		{
			if (!DmbAvailable)
				return null;
			return LockNextDmb(0)?.CompileJob;
		}

		/// <summary>
		/// Delete the <see cref="Api.Models.Internal.CompileJob.DirectoryName"/> of <paramref name="job"/>.
		/// </summary>
		/// <param name="job">The <see cref="CompileJob"/> to clean.</param>
		void CleanRegisteredCompileJob(CompileJob job)
		{
			async Task HandleCleanup()
			{
				// First kill the GitHub deployment
				var remoteDeploymentManager = remoteDeploymentManagerFactory.CreateRemoteDeploymentManager(metadata, job);

				// DCT: None available
				var deploymentJob = remoteDeploymentManager.MarkInactive(job, CancellationToken.None);

				var deleteTask = DeleteCompileJobContent(job.DirectoryName.ToString(), cleanupCts.Token);
				var otherTask = cleanupTask;

				async Task WrapThrowableTasks()
				{
					try
					{
						await ValueTaskExtensions.WhenAll(deleteTask, deploymentJob);
					}
					catch (Exception ex)
					{
						logger.LogWarning(ex, "Error cleaning up compile job {jobGuid}!", job.DirectoryName);
					}
				}

				await Task.WhenAll(otherTask, WrapThrowableTasks());
			}

			lock (jobLockCounts)
				if (jobLockCounts.TryGetValue(job.Id.Value, out var currentVal))
					if (currentVal == 1)
					{
						jobLockCounts.Remove(job.Id.Value);
						logger.LogDebug("Cleaning lock-free compile job {id} => {dirName}", job.Id, job.DirectoryName);
						cleanupTask = HandleCleanup();
					}
					else
					{
						var decremented = --jobLockCounts[job.Id.Value];
						logger.LogTrace("Compile job {id} lock count now: {lockCount}", job.Id, decremented);
					}
				else
					logger.LogError("Extra Dispose of DmbProvider for CompileJob {compileJobId}!", job.Id);
		}

		/// <summary>
		/// Handles cleaning the resources of a <see cref="CompileJob"/>.
		/// </summary>
		/// <param name="directory">The directory to cleanup.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for this <see cref="Task"/>.</param>
		/// <returns>The deletion <see cref="ValueTask"/>.</returns>
		async ValueTask DeleteCompileJobContent(string directory, CancellationToken cancellationToken)
		{
			// Then call the cleanup event, waiting here first
			await eventConsumer.HandleEvent(EventType.DeploymentCleanup, new List<string> { ioManager.ResolvePath(directory) }, true, cancellationToken);
			await ioManager.DeleteDirectory(directory, cancellationToken);
		}
	}
}
