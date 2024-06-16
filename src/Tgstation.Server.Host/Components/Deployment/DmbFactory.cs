using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using Tgstation.Server.Api.Models;
using Tgstation.Server.Common.Extensions;
using Tgstation.Server.Host.Components.Deployment.Remote;
using Tgstation.Server.Host.Components.Events;
using Tgstation.Server.Host.Database;
using Tgstation.Server.Host.IO;
using Tgstation.Server.Host.Models;
using Tgstation.Server.Host.Utils;

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
				lock (jobLockManagers)
					return newerDmbTcs.Task;
			}
		}

		/// <inheritdoc />
		[MemberNotNullWhen(true, nameof(nextLockManager))]
		public bool DmbAvailable => nextLockManager != null;

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
		readonly Dictionary<long, DeploymentLockManager> jobLockManagers;

		/// <summary>
		/// <see cref="TaskCompletionSource"/> resulting in the latest <see cref="DmbProvider"/> yet to exist.
		/// </summary>
		volatile TaskCompletionSource newerDmbTcs;

		/// <summary>
		/// <see cref="Task"/> representing calls to <see cref="CleanRegisteredCompileJob(CompileJob)"/>.
		/// </summary>
		Task cleanupTask;

		/// <summary>
		/// The <see cref="DeploymentLockManager"/> for the latest <see cref="DmbProvider"/>.
		/// </summary>
		DeploymentLockManager? nextLockManager;

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
			jobLockManagers = new Dictionary<long, DeploymentLockManager>();
		}

		/// <inheritdoc />
		public void Dispose() => cleanupCts.Dispose(); // we don't dispose nextDmbProvider here, since it might be the only thing we have

		/// <inheritdoc />
		public async ValueTask LoadCompileJob(CompileJob job, Action<bool>? activationAction, CancellationToken cancellationToken)
		{
			ArgumentNullException.ThrowIfNull(job);

			var (dmbProvider, lockManager) = await FromCompileJobInternal(job, "Compile job loading", cancellationToken);
			if (dmbProvider == null)
				return;

			if (lockManager == null)
				throw new InvalidOperationException($"We did not acquire the first lock for compile job {job.Id}!");

			// Do this first, because it's entirely possible when we set the tcs it will immediately need to be applied
			if (started)
			{
				var remoteDeploymentManager = remoteDeploymentManagerFactory.CreateRemoteDeploymentManager(
					metadata,
					job);
				await remoteDeploymentManager.StageDeployment(
					lockManager.CompileJob,
					activationAction,
					cancellationToken);
			}

			ValueTask dmbDisposeTask;
			lock (jobLockManagers)
			{
				dmbDisposeTask = nextLockManager?.DisposeAsync() ?? ValueTask.CompletedTask;
				nextLockManager = lockManager;

				// Oh god dammit
				var temp = Interlocked.Exchange(ref newerDmbTcs, new TaskCompletionSource());
				temp.SetResult();
			}

			await dmbDisposeTask;
		}

		/// <inheritdoc />
		public IDmbProvider LockNextDmb(string reason, [CallerFilePath] string? callerFile = null, [CallerLineNumber] int callerLine = default)
		{
			if (!DmbAvailable)
				throw new InvalidOperationException("No .dmb available!");

			return nextLockManager.AddLock(reason, callerFile, callerLine);
		}

		/// <inheritdoc />
		public async Task StartAsync(CancellationToken cancellationToken)
		{
			CompileJob? cj = null;
			await databaseContextFactory.UseContext(
				async (db) =>
					cj = await db
						.CompileJobs
						.AsQueryable()
						.Where(x => x.Job.Instance!.Id == metadata.Id)
						.OrderByDescending(x => x.Job.StoppedAt)
						.FirstOrDefaultAsync(cancellationToken));

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
				lock (jobLockManagers)
					remoteDeploymentManagerFactory.ForgetLocalStateForCompileJobs(jobLockManagers.Keys);

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
		public async ValueTask<IDmbProvider?> FromCompileJob(CompileJob compileJob, string reason, CancellationToken cancellationToken, [CallerFilePath] string? callerFile = null, [CallerLineNumber] int callerLine = default)
		{
			ArgumentNullException.ThrowIfNull(compileJob);
			ArgumentNullException.ThrowIfNull(reason);

			var (dmb, _) = await FromCompileJobInternal(compileJob, reason, cancellationToken, callerFile, callerLine);

			return dmb;
		}

		/// <inheritdoc />
#pragma warning disable CA1506 // TODO: Decomplexify
		public async ValueTask CleanUnusedCompileJobs(CancellationToken cancellationToken)
		{
			List<long> jobIdsToSkip;

			// don't clean locked directories
			lock (jobLockManagers)
				jobIdsToSkip = jobLockManagers.Keys.ToList();

			List<string>? jobUidsToNotErase = null;

			// find the uids of locked directories
			if (jobIdsToSkip.Count > 0)
			{
				await databaseContextFactory.UseContext(async db =>
				{
					jobUidsToNotErase = (await db
							.CompileJobs
							.AsQueryable()
							.Where(
								x => x.Job.Instance!.Id == metadata.Id
								&& jobIdsToSkip.Contains(x.Id!.Value))
							.Select(x => x.DirectoryName!.Value)
							.ToListAsync(cancellationToken))
						.Select(x => x.ToString())
						.ToList();
				});
			}
			else
				jobUidsToNotErase = new List<string>();

			jobUidsToNotErase!.Add(SwappableDmbProvider.LiveGameDirectory);

			logger.LogTrace("We will not clean the following directories: {directoriesToNotClean}", String.Join(", ", jobUidsToNotErase));

			// cleanup
			var gameDirectory = ioManager.ResolvePath();
			await ioManager.CreateDirectory(gameDirectory, cancellationToken);
			var directories = await ioManager.GetDirectories(gameDirectory, cancellationToken);
			int deleting = 0;
			var tasks = directories.Select<string, ValueTask>(async x =>
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
				catch (Exception e) when (e is not OperationCanceledException)
				{
					logger.LogWarning(e, "Error deleting directory {dirName}!", x);
				}
			}).ToList();
			if (deleting > 0)
				await ValueTaskExtensions.WhenAll(tasks);
		}
#pragma warning restore CA1506

		/// <inheritdoc />
		public async ValueTask<CompileJob?> LatestCompileJob()
		{
			if (!DmbAvailable)
				return null;

			await using IDmbProvider provider = LockNextDmb("Checking latest CompileJob");

			return provider.CompileJob;
		}

		/// <summary>
		/// Gets a <see cref="IDmbProvider"/> and potentially the <see cref="DeploymentLockManager"/> for a given <see cref="CompileJob"/>.
		/// </summary>
		/// <param name="compileJob">The <see cref="CompileJob"/> to make the <see cref="IDmbProvider"/> for.</param>
		/// <param name="reason">The reason the compile job needed to be loaded.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <param name="callerFile">The file path of the calling function.</param>
		/// <param name="callerLine">The line number of the call invocation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in, on success, a tuple containing new <see cref="IDmbProvider"/> representing the <see cref="CompileJob"/>. If the first lock on <paramref name="compileJob"/> was acquired, the <see cref="DeploymentLockManager"/> will also be returned. On failure, <see langword="null"/> Will be returned.</returns>
		async ValueTask<(IDmbProvider? DmbProvider, DeploymentLockManager? LockManager)> FromCompileJobInternal(CompileJob compileJob, string reason, CancellationToken cancellationToken, [CallerFilePath] string? callerFile = null, [CallerLineNumber] int callerLine = default)
		{
			// ensure we have the entire metadata tree
			var compileJobId = compileJob.Require(x => x.Id);
			lock (jobLockManagers)
				if (jobLockManagers.TryGetValue(compileJobId, out var lockManager))
					return (DmbProvider: lockManager.AddLock(reason, callerFile, callerLine), LockManager: null); // fast path

			logger.LogTrace("Loading compile job {id}...", compileJobId);
			await databaseContextFactory.UseContext(
				async db => compileJob = await db
					.CompileJobs
					.AsQueryable()
					.Where(x => x!.Id == compileJobId)
					.Include(x => x.Job!)
						.ThenInclude(x => x.StartedBy)
					.Include(x => x.Job!)
						.ThenInclude(x => x.Instance)
					.Include(x => x.RevisionInformation!)
						.ThenInclude(x => x.PrimaryTestMerge!)
							.ThenInclude(x => x.MergedBy)
					.Include(x => x.RevisionInformation!)
						.ThenInclude(x => x.ActiveTestMerges!)
							.ThenInclude(x => x.TestMerge!)
								.ThenInclude(x => x.MergedBy)
					.FirstAsync(cancellationToken)); // can't wait to see that query

			EngineVersion engineVersion;
			if (!EngineVersion.TryParse(compileJob.EngineVersion, out var engineVersionNullable))
			{
				logger.LogWarning("Error loading compile job, bad engine version: {engineVersion}", compileJob.EngineVersion);
				return (null, null); // omae wa mou shinderu
			}
			else
				engineVersion = engineVersionNullable!;

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

			var newProvider = new DmbProvider(compileJob, engineVersion, ioManager, new DisposeInvoker(CleanupAction));
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
						return (null, null); // omae wa mou shinderu
					}

					// rebuild the provider because it's using the legacy style directories
					// Don't dispose it
					logger.LogDebug("Creating legacy two folder .dmb provider targeting {aDirName} directory...", LegacyADirectoryName);
#pragma warning disable CA2000 // Dispose objects before losing scope (false positive)
					newProvider = new DmbProvider(compileJob, engineVersion, ioManager, new DisposeInvoker(CleanupAction), Path.DirectorySeparatorChar + LegacyADirectoryName);
#pragma warning restore CA2000 // Dispose objects before losing scope
				}

				lock (jobLockManagers)
				{
					IDmbProvider lockedProvider;
					if (!jobLockManagers.TryGetValue(compileJobId, out var lockManager))
					{
						lockManager = DeploymentLockManager.Create(newProvider, logger, reason, out lockedProvider);
						jobLockManagers.Add(compileJobId, lockManager);

						providerSubmitted = true;
					}
					else
					{
						lockedProvider = lockManager.AddLock(reason, callerFile, callerLine); // race condition
						lockManager = null;
					}

					return (DmbProvider: lockedProvider, LockManager: lockManager);
				}
			}
			finally
			{
				if (!providerSubmitted)
					await newProvider.DisposeAsync();
			}
		}

		/// <summary>
		/// Delete the <see cref="Api.Models.Internal.CompileJob.DirectoryName"/> of <paramref name="job"/>.
		/// </summary>
		/// <param name="job">The <see cref="CompileJob"/> to clean.</param>
		void CleanRegisteredCompileJob(CompileJob job)
		{
			Task HandleCleanup()
			{
				lock (jobLockManagers)
					jobLockManagers.Remove(job.Require(x => x.Id));

				var otherTask = cleanupTask;

				async Task WrapThrowableTasks()
				{
					try
					{
						// First kill the GitHub deployment
						var remoteDeploymentManager = remoteDeploymentManagerFactory.CreateRemoteDeploymentManager(metadata, job);

						var cancellationToken = cleanupCts.Token;
						var deploymentJob = remoteDeploymentManager.MarkInactive(job, cancellationToken);

						var deleteTask = DeleteCompileJobContent(job.DirectoryName!.Value.ToString(), cancellationToken);

						await ValueTaskExtensions.WhenAll(deleteTask, deploymentJob);
					}
					catch (Exception ex) when (ex is not OperationCanceledException)
					{
						logger.LogWarning(ex, "Error cleaning up compile job {jobGuid}!", job.DirectoryName);
					}
				}

				return Task.WhenAll(otherTask, WrapThrowableTasks());
			}

			lock (cleanupCts)
				cleanupTask = HandleCleanup();
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
