using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using NCrontab;

using Serilog.Context;

using Tgstation.Server.Api.Rights;
using Tgstation.Server.Host.Components.Chat;
using Tgstation.Server.Host.Components.Deployment;
using Tgstation.Server.Host.Components.Deployment.Remote;
using Tgstation.Server.Host.Components.Engine;
using Tgstation.Server.Host.Components.Events;
using Tgstation.Server.Host.Components.Repository;
using Tgstation.Server.Host.Components.Watchdog;
using Tgstation.Server.Host.Database;
using Tgstation.Server.Host.Jobs;
using Tgstation.Server.Host.Models;
using Tgstation.Server.Host.Utils;

namespace Tgstation.Server.Host.Components
{
	/// <inheritdoc />
#pragma warning disable CA1506 // TODO: Decomplexify
	sealed class Instance : IInstance
	{
		/// <summary>
		/// Message for the <see cref="InvalidOperationException"/> if ever a job starts on a different <see cref="IInstanceCore"/> than the one that queued it.
		/// </summary>
		public const string DifferentCoreExceptionMessage = "Job started on different instance core!";

		/// <inheritdoc />
		public IRepositoryManager RepositoryManager { get; }

		/// <inheritdoc />
		public IEngineManager EngineManager { get; }

		/// <inheritdoc />
		public IWatchdog Watchdog { get; }

		/// <inheritdoc />
		public IChatManager Chat { get; }

		/// <inheritdoc />
		public StaticFiles.IConfiguration Configuration { get; }

		/// <inheritdoc />
		public IDreamMaker DreamMaker { get; }

		/// <summary>
		/// The <see cref="IDmbFactory"/> for the <see cref="Instance"/>.
		/// </summary>
		readonly IDmbFactory dmbFactory;

		/// <summary>
		/// The <see cref="IJobManager"/> for the <see cref="Instance"/>.
		/// </summary>
		readonly IJobManager jobManager;

		/// <summary>
		/// The <see cref="IEventConsumer"/> for the <see cref="Instance"/>.
		/// </summary>
		readonly IEventConsumer eventConsumer;

		/// <summary>
		/// The <see cref="IRemoteDeploymentManagerFactory"/> for the <see cref="Instance"/>.
		/// </summary>
		readonly IRemoteDeploymentManagerFactory remoteDeploymentManagerFactory;

		/// <summary>
		/// The <see cref="IAsyncDelayer"/> for the <see cref="Instance"/>.
		/// </summary>
		readonly IAsyncDelayer asyncDelayer;

		/// <summary>
		/// The <see cref="ILogger"/> for the <see cref="Instance"/>.
		/// </summary>
		readonly ILogger<Instance> logger;

		/// <summary>
		/// The <see cref="Api.Models.Instance"/> for the <see cref="Instance"/>.
		/// </summary>
		readonly Api.Models.Instance metadata;

		/// <summary>
		/// <see langword="lock"/> <see cref="object"/> for <see cref="timerCts"/> and <see cref="timerTask"/>.
		/// </summary>
		readonly object timerLock;

		/// <summary>
		/// The auto update <see cref="Task"/>.
		/// </summary>
		Task? timerTask;

		/// <summary>
		/// <see cref="CancellationTokenSource"/> for <see cref="timerTask"/>.
		/// </summary>
		CancellationTokenSource? timerCts;

		/// <summary>
		/// Initializes a new instance of the <see cref="Instance"/> class.
		/// </summary>
		/// <param name="metadata">The value of <see cref="metadata"/>.</param>
		/// <param name="repositoryManager">The value of <see cref="RepositoryManager"/>.</param>
		/// <param name="engineManager">The value of <see cref="EngineManager"/>.</param>
		/// <param name="dreamMaker">The value of <see cref="DreamMaker"/>.</param>
		/// <param name="watchdog">The value of <see cref="Watchdog"/>.</param>
		/// <param name="chat">The value of <see cref="Chat"/>.</param>
		/// <param name="configuration">The value of <see cref="Configuration"/>.</param>
		/// <param name="dmbFactory">The value of <see cref="dmbFactory"/>.</param>
		/// <param name="jobManager">The value of <see cref="jobManager"/>.</param>
		/// <param name="eventConsumer">The value of <see cref="eventConsumer"/>.</param>
		/// <param name="remoteDeploymentManagerFactory">The value of <see cref="remoteDeploymentManagerFactory"/>.</param>
		/// <param name="asyncDelayer">The value of <see cref="asyncDelayer"/>.</param>
		/// <param name="logger">The value of <see cref="logger"/>.</param>
		public Instance(
			Api.Models.Instance metadata,
			IRepositoryManager repositoryManager,
			IEngineManager engineManager,
			IDreamMaker dreamMaker,
			IWatchdog watchdog,
			IChatManager chat,
			StaticFiles.IConfiguration
			configuration,
			IDmbFactory dmbFactory,
			IJobManager jobManager,
			IEventConsumer eventConsumer,
			IRemoteDeploymentManagerFactory remoteDeploymentManagerFactory,
			IAsyncDelayer asyncDelayer,
			ILogger<Instance> logger)
		{
			this.metadata = metadata ?? throw new ArgumentNullException(nameof(metadata));
			RepositoryManager = repositoryManager ?? throw new ArgumentNullException(nameof(repositoryManager));
			EngineManager = engineManager ?? throw new ArgumentNullException(nameof(engineManager));
			DreamMaker = dreamMaker ?? throw new ArgumentNullException(nameof(dreamMaker));
			Watchdog = watchdog ?? throw new ArgumentNullException(nameof(watchdog));
			Chat = chat ?? throw new ArgumentNullException(nameof(chat));
			Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
			this.dmbFactory = dmbFactory ?? throw new ArgumentNullException(nameof(dmbFactory));
			this.jobManager = jobManager ?? throw new ArgumentNullException(nameof(jobManager));
			this.eventConsumer = eventConsumer ?? throw new ArgumentNullException(nameof(eventConsumer));
			this.remoteDeploymentManagerFactory = remoteDeploymentManagerFactory ?? throw new ArgumentNullException(nameof(remoteDeploymentManagerFactory));
			this.asyncDelayer = asyncDelayer ?? throw new ArgumentNullException(nameof(asyncDelayer));
			this.logger = logger ?? throw new ArgumentNullException(nameof(logger));

			timerLock = new object();
		}

		/// <inheritdoc />
		public async ValueTask DisposeAsync()
		{
			using (LogContext.PushProperty(SerilogContextHelper.InstanceIdContextProperty, metadata.Id))
			{
				var chatDispose = Chat.DisposeAsync();
				var watchdogDispose = Watchdog.DisposeAsync();
				timerCts?.Dispose();
				Configuration.Dispose();
				dmbFactory.Dispose();
				RepositoryManager.Dispose();
				EngineManager.Dispose();
				await chatDispose;
				await watchdogDispose;
			}
		}

		/// <inheritdoc />
		public ValueTask InstanceRenamed(string newName, CancellationToken cancellationToken)
		{
			ArgumentNullException.ThrowIfNull(newName);
			if (String.IsNullOrWhiteSpace(newName))
				throw new ArgumentException("newName cannot be whitespace!", nameof(newName));

			metadata.Name = newName;
			return Watchdog.InstanceRenamed(newName, cancellationToken);
		}

		/// <inheritdoc />
		public async Task StartAsync(CancellationToken cancellationToken)
		{
			using (LogContext.PushProperty(SerilogContextHelper.InstanceIdContextProperty, metadata.Id))
			{
				await Task.WhenAll(
					ScheduleAutoUpdate(metadata.Require(x => x.AutoUpdateInterval), metadata.AutoUpdateCron).AsTask(),
					Configuration.StartAsync(cancellationToken),
					EngineManager.StartAsync(cancellationToken),
					Chat.StartAsync(cancellationToken),
					dmbFactory.StartAsync(cancellationToken));

				// dependent on so many things, its just safer this way
				await Watchdog.StartAsync(cancellationToken);

				await dmbFactory.CleanUnusedCompileJobs(cancellationToken);
			}
		}

		/// <inheritdoc />
		public async Task StopAsync(CancellationToken cancellationToken)
		{
			using (LogContext.PushProperty(SerilogContextHelper.InstanceIdContextProperty, metadata.Id))
			{
				logger.LogDebug("Stopping instance...");
				await ScheduleAutoUpdate(0, null);
				await Watchdog.StopAsync(cancellationToken);
				await Task.WhenAll(
					Configuration.StopAsync(cancellationToken),
					EngineManager.StopAsync(cancellationToken),
					Chat.StopAsync(cancellationToken),
					dmbFactory.StopAsync(cancellationToken));
			}
		}

		/// <inheritdoc />
		public async ValueTask ScheduleAutoUpdate(uint newInterval, string? newCron)
		{
			if (newInterval > 0 && !String.IsNullOrWhiteSpace(newCron))
				throw new ArgumentException("Only one of newInterval and newCron may be set!");

			Task toWait;
			lock (timerLock)
				if (timerTask != null)
				{
					logger.LogTrace("Cancelling auto-update task");
					timerCts!.Cancel();
					timerCts.Dispose();
					toWait = timerTask;
					timerTask = null;
					timerCts = null;
				}
				else
					toWait = Task.CompletedTask;

			await toWait;
			if (newInterval == 0 && String.IsNullOrWhiteSpace(newCron))
			{
				logger.LogTrace("Auto-update disabled 0. Not starting task.");
				return;
			}

			lock (timerLock)
			{
				// race condition, just quit
				if (timerTask != null)
				{
					logger.LogWarning("Aborting auto-update scheduling change due to race condition!");
					return;
				}

				timerCts = new CancellationTokenSource();
				timerTask = TimerLoop(newInterval, newCron, timerCts.Token);
			}
		}

		/// <inheritdoc />
		public CompileJob? LatestCompileJob() => dmbFactory.LatestCompileJob();

		/// <summary>
		/// The <see cref="JobEntrypoint"/> for updating the repository.
		/// </summary>
		/// <param name="core">The <see cref="IInstanceCore"/> for the <paramref name="job"/>.</param>
		/// <param name="databaseContextFactory">The <see cref="IDatabaseContextFactory"/> for the <paramref name="job"/>.</param>
		/// <param name="job">The <see cref="Job"/> being run.</param>
		/// <param name="progressReporter">The progress reporter action for the <paramref name="job"/>.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask"/> representing the running operation.</returns>
#pragma warning disable CA1502 // Cyclomatic complexity
		ValueTask RepositoryAutoUpdateJob(
			IInstanceCore? core,
			IDatabaseContextFactory databaseContextFactory,
			Job job,
			JobProgressReporter progressReporter,
			CancellationToken cancellationToken)
			=> databaseContextFactory.UseContext(
				async databaseContext =>
				{
					if (core != this)
						throw new InvalidOperationException(DifferentCoreExceptionMessage);

					// assume 5 steps with synchronize
					var repositorySettingsTask = databaseContext
						.RepositorySettings
						.AsQueryable()
						.Where(x => x.InstanceId == metadata.Id)
						.FirstAsync(cancellationToken);

					const int ProgressSections = 7;
					JobProgressReporter NextProgressReporter(string stage)
					{
						return progressReporter.CreateSection(stage, 1.0 / ProgressSections);
					}

					using var repo = await RepositoryManager.LoadRepository(cancellationToken);
					if (repo == null)
					{
						logger.LogTrace("Aborting repo update, no repository!");
						return;
					}

					var startSha = repo.Head;
					if (!repo.Tracking)
					{
						logger.LogTrace("Aborting repo update, active ref not tracking any remote branch!");
						return;
					}

					var repositorySettings = await repositorySettingsTask;

					// the main point of auto update is to pull the remote
					await repo.FetchOrigin(
						NextProgressReporter("Fetch Origin"),
						repositorySettings.AccessUser,
						repositorySettings.AccessToken,
						true,
						cancellationToken);

					var hasDbChanges = false;
					RevisionInformation? currentRevInfo = null;
					Models.Instance? attachedInstance = null;
					async ValueTask UpdateRevInfo(string currentHead, bool onOrigin, IEnumerable<TestMerge>? updatedTestMerges)
					{
						if (currentRevInfo == null)
						{
							logger.LogTrace("Loading revision info for commit {sha}...", startSha[..7]);
							currentRevInfo = await databaseContext
								.RevisionInformations
								.AsQueryable()
								.Where(x => x.CommitSha == startSha && x.InstanceId == metadata.Id)
								.Include(x => x.ActiveTestMerges!)
									.ThenInclude(x => x.TestMerge)
								.FirstOrDefaultAsync(cancellationToken);
						}

						if (currentRevInfo == default)
						{
							logger.LogInformation(Repository.Repository.OriginTrackingErrorTemplate, currentHead);
							onOrigin = true;
						}
						else if (currentRevInfo.CommitSha == currentHead)
						{
							logger.LogTrace("Not updating rev-info, already in DB.");
							return;
						}

						if (attachedInstance == null)
						{
							attachedInstance = new Models.Instance
							{
								Id = metadata.Id,
							};
							databaseContext.Instances.Attach(attachedInstance);
						}

						var oldRevInfo = currentRevInfo;
						currentRevInfo = new RevisionInformation
						{
							CommitSha = currentHead,
							Timestamp = await repo.TimestampCommit(currentHead, cancellationToken),
							OriginCommitSha = onOrigin
								? currentHead
								: await repo.GetOriginSha(cancellationToken),
							Instance = attachedInstance,
						};

						if (!onOrigin)
						{
							var testMerges = updatedTestMerges ?? oldRevInfo!.ActiveTestMerges!.Select(x => x.TestMerge);
							var revInfoTestMerges = testMerges.Select(
								testMerge => new RevInfoTestMerge(testMerge, currentRevInfo))
								.ToList();

							currentRevInfo.ActiveTestMerges = revInfoTestMerges;
						}

						databaseContext.RevisionInformations.Add(currentRevInfo);
						hasDbChanges = true;
					}

					// build current commit data if it's missing
					await UpdateRevInfo(repo.Head, false, null);

					var preserveTestMerges = repositorySettings.AutoUpdatesKeepTestMerges!.Value;
					var remoteDeploymentManager = remoteDeploymentManagerFactory.CreateRemoteDeploymentManager(
						metadata,
						repo.RemoteGitProvider!.Value);

					var updatedTestMerges = await remoteDeploymentManager.RemoveMergedTestMerges(
						repo,
						repositorySettings,
						currentRevInfo!,
						cancellationToken);

					var result = await repo.MergeOrigin(
						NextProgressReporter("Merge Origin"),
						repositorySettings.CommitterName!,
						repositorySettings.CommitterEmail!,
						true,
						cancellationToken);

					// take appropriate auto update actions
					var shouldSyncTracked = false;
					if (result.HasValue)
					{
						if (updatedTestMerges.Count == 0)
						{
							logger.LogTrace("All test merges have been merged on remote");
							preserveTestMerges = false;
						}
						else
						{
							var lastRevInfoWasOriginCommit =
								currentRevInfo == default
								|| currentRevInfo.CommitSha == currentRevInfo.OriginCommitSha;
							var stillOnOrigin = result.Value && lastRevInfoWasOriginCommit;

							var currentHead = repo.Head;
							if (currentHead != startSha)
							{
								await UpdateRevInfo(currentHead, stillOnOrigin, updatedTestMerges);
								shouldSyncTracked = stillOnOrigin;
							}
						}
					}
					else if (preserveTestMerges)
						throw new JobException(Api.Models.ErrorCode.InstanceUpdateTestMergeConflict);

					if (!preserveTestMerges)
					{
						const string StageName = "Resetting to origin...";
						logger.LogTrace(StageName);
						await repo.ResetToOrigin(
							NextProgressReporter(StageName),
							repositorySettings.AccessUser,
							repositorySettings.AccessToken,
							repositorySettings.UpdateSubmodules!.Value,
							true,
							cancellationToken);

						var currentHead = repo.Head;

						currentRevInfo = await databaseContext.RevisionInformations
							.AsQueryable()
							.Where(x => x.CommitSha == currentHead && x.InstanceId == metadata.Id)
							.FirstOrDefaultAsync(cancellationToken);

						if (currentHead != startSha && currentRevInfo == default)
							await UpdateRevInfo(currentHead, true, null);

						shouldSyncTracked = true;
					}

					// synch if necessary
					if (repositorySettings.AutoUpdatesSynchronize!.Value && startSha != repo.Head && (shouldSyncTracked || repositorySettings.PushTestMergeCommits!.Value))
					{
						var pushedOrigin = await repo.Synchronize(
							NextProgressReporter("Synchronize"),
							repositorySettings.AccessUser,
							repositorySettings.AccessToken,
							repositorySettings.CommitterName!,
							repositorySettings.CommitterEmail!,
							shouldSyncTracked,
							true,
							cancellationToken);
						var currentHead = repo.Head;
						if (currentHead != currentRevInfo!.CommitSha)
							await UpdateRevInfo(currentHead, pushedOrigin, null);
					}

					if (hasDbChanges)
						try
						{
							await databaseContext.Save(cancellationToken);
						}
						catch
						{
							// DCT: Cancellation token is for job, operation must run regardless
							await repo.ResetToSha(startSha, progressReporter, CancellationToken.None);
							throw;
						}
				});
#pragma warning restore CA1502   // Cyclomatic complexity

		/// <summary>
		/// Pull the repository and compile for every set of given <paramref name="minutes"/>.
		/// </summary>
		/// <param name="minutes">How many minutes the operation should repeat. Does not include running time.</param>
		/// <param name="cron">Alternative cron schedule.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task"/> representing the running operation.</returns>
#pragma warning disable CA1502 // TODO: Decomplexify
		async Task TimerLoop(uint minutes, string? cron, CancellationToken cancellationToken)
		{
			logger.LogDebug("Entering auto-update loop");
			while (true)
				try
				{
					TimeSpan delay;
					if (!String.IsNullOrWhiteSpace(cron))
					{
						logger.LogTrace("Using cron schedule: {cron}", cron);
						var schedule = CrontabSchedule.Parse(
							cron,
							new CrontabSchedule.ParseOptions
							{
								IncludingSeconds = true,
							});
						var now = DateTime.UtcNow;
						var nextOccurrence = schedule.GetNextOccurrence(now);
						delay = nextOccurrence - now;
					}
					else
					{
						logger.LogTrace("Using interval: {interval}m", minutes);

						delay = TimeSpan.FromMinutes(minutes);
					}

					logger.LogInformation("Next auto-update will occur at {time}", DateTimeOffset.UtcNow + delay);

					// https://learn.microsoft.com/en-us/dotnet/api/system.threading.tasks.task.delay?view=net-8.0#system-threading-tasks-task-delay(system-timespan)
					const uint DelayMinutesLimit = UInt32.MaxValue - 1;
					Debug.Assert(DelayMinutesLimit == 4294967294, "Delay limit assertion failure!");

					var maxDelayIterations = 0UL;
					if (delay.TotalMilliseconds >= UInt32.MaxValue)
					{
						maxDelayIterations = (ulong)Math.Floor(delay.TotalMilliseconds / DelayMinutesLimit);
						logger.LogDebug("Breaking interval into {iterationCount} iterations", maxDelayIterations + 1);
						delay = TimeSpan.FromMilliseconds(delay.TotalMilliseconds - (maxDelayIterations * DelayMinutesLimit));
					}

					if (maxDelayIterations > 0)
					{
						var longDelayTimeSpan = TimeSpan.FromMilliseconds(DelayMinutesLimit);
						for (var i = 0UL; i < maxDelayIterations; ++i)
						{
							logger.LogTrace("Long delay #{iteration}...", i + 1);
							await asyncDelayer.Delay(longDelayTimeSpan, cancellationToken);
						}

						logger.LogTrace("Final delay iteration #{iteration}...", maxDelayIterations + 1);
					}

					await asyncDelayer.Delay(delay, cancellationToken);
					logger.LogInformation("Beginning auto update...");
					await eventConsumer.HandleEvent(EventType.InstanceAutoUpdateStart, Enumerable.Empty<string>(), true, cancellationToken);

					var repositoryUpdateJob = Job.Create(Api.Models.JobCode.RepositoryAutoUpdate, null, metadata, RepositoryRights.CancelPendingChanges);
					await jobManager.RegisterOperation(
						repositoryUpdateJob,
						RepositoryAutoUpdateJob,
						cancellationToken);

					var repoUpdateJobResult = await jobManager.WaitForJobCompletion(repositoryUpdateJob, null, cancellationToken, cancellationToken);
					if (repoUpdateJobResult == false)
					{
						logger.LogWarning("Aborting auto-update due to repository update error!");
						continue;
					}

					Job compileProcessJob;
					using (var repo = await RepositoryManager.LoadRepository(cancellationToken))
					{
						if (repo == null)
							throw new JobException(Api.Models.ErrorCode.RepoMissing);

						var deploySha = repo.Head;
						if (deploySha == null)
						{
							logger.LogTrace("Aborting auto update, repository error!");
							continue;
						}

						if (deploySha == LatestCompileJob()?.RevisionInformation.CommitSha)
						{
							logger.LogTrace("Aborting auto update, same revision as latest CompileJob");
							continue;
						}

						// finally set up the job
						compileProcessJob = Job.Create(Api.Models.JobCode.AutomaticDeployment, null, metadata, DreamMakerRights.CancelCompile);
						await jobManager.RegisterOperation(
							compileProcessJob,
							(core, databaseContextFactory, job, progressReporter, jobCancellationToken) =>
							{
								if (core != this)
									throw new InvalidOperationException(DifferentCoreExceptionMessage);
								return DreamMaker.DeploymentProcess(
									job,
									databaseContextFactory,
									progressReporter,
									jobCancellationToken);
							},
							cancellationToken);
					}

					await jobManager.WaitForJobCompletion(compileProcessJob, null, default, cancellationToken);
				}
				catch (OperationCanceledException)
				{
					logger.LogDebug("Cancelled auto update loop!");
					break;
				}
				catch (Exception e)
				{
					logger.LogError(e, "Error in auto update loop!");
					continue;
				}

			logger.LogTrace("Leaving auto update loop...");
		}
#pragma warning restore CA1502
	}
}
