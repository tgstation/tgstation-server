using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Api.Rights;
using Tgstation.Server.Host.Components.Byond;
using Tgstation.Server.Host.Components.Chat;
using Tgstation.Server.Host.Components.Compiler;
using Tgstation.Server.Host.Components.Repository;
using Tgstation.Server.Host.Components.Watchdog;
using Tgstation.Server.Host.Core;
using Tgstation.Server.Host.Models;

namespace Tgstation.Server.Host.Components
{
	/// <inheritdoc />
	sealed class Instance : IInstance
	{
		/// <inheritdoc />
		public IRepositoryManager RepositoryManager { get; }

		/// <inheritdoc />
		public IByondManager ByondManager { get; }

		/// <inheritdoc />
		public IDreamMaker DreamMaker { get; }

		/// <inheritdoc />
		public IWatchdog Watchdog { get; }

		/// <inheritdoc />
		public IChat Chat { get; }

		/// <inheritdoc />
		public StaticFiles.IConfiguration Configuration { get; }

		/// <inheritdoc />
		public ICompileJobConsumer CompileJobConsumer { get; }

		/// <summary>
		/// The <see cref="IDatabaseContextFactory"/> for the <see cref="Instance"/>
		/// </summary>
		readonly IDatabaseContextFactory databaseContextFactory;

		/// <summary>
		/// The <see cref="IDmbFactory"/> for the <see cref="Instance"/>
		/// </summary>
		readonly IDmbFactory dmbFactory;

		/// <summary>
		/// The <see cref="IJobManager"/> for the <see cref="Instance"/>
		/// </summary>
		readonly IJobManager jobManager;

		/// <summary>
		/// The <see cref="ILogger"/> for the <see cref="Instance"/>
		/// </summary>
		readonly ILogger<Instance> logger;

		/// <summary>
		/// The <see cref="Api.Models.Instance"/> for the <see cref="Instance"/>
		/// </summary>
		readonly Api.Models.Instance metadata;

		/// <summary>
		/// The auto update <see cref="Task"/>
		/// </summary>
		Task timerTask;
		/// <summary>
		/// <see cref="CancellationTokenSource"/> for <see cref="timerTask"/>
		/// </summary>
		CancellationTokenSource timerCts;

		/// <summary>
		/// Construct an <see cref="Instance"/>
		/// </summary>
		/// <param name="metadata">The value of <see cref="metadata"/></param>
		/// <param name="repositoryManager">The value of <see cref="RepositoryManager"/></param>
		/// <param name="byondManager">The value of <see cref="ByondManager"/></param>
		/// <param name="dreamMaker">The value of <see cref="DreamMaker"/></param>
		/// <param name="watchdog">The value of <see cref="Watchdog"/></param>
		/// <param name="chat">The value of <see cref="Chat"/></param>
		/// <param name="configuration">The value of <see cref="Configuration"/></param>
		/// <param name="compileJobConsumer">The value of <see cref="CompileJobConsumer"/></param>
		/// <param name="databaseContextFactory">The value of <see cref="databaseContextFactory"/></param>
		/// <param name="dmbFactory">The value of <see cref="dmbFactory"/></param>
		/// <param name="jobManager">The value of <see cref="jobManager"/></param>
		/// <param name="logger">The value of <see cref="logger"/></param>
		public Instance(Api.Models.Instance metadata, IRepositoryManager repositoryManager, IByondManager byondManager, IDreamMaker dreamMaker, IWatchdog watchdog, IChat chat, StaticFiles.IConfiguration configuration, ICompileJobConsumer compileJobConsumer, IDatabaseContextFactory databaseContextFactory, IDmbFactory dmbFactory, IJobManager jobManager, ILogger<Instance> logger)
		{
			this.metadata = metadata ?? throw new ArgumentNullException(nameof(metadata));
			RepositoryManager = repositoryManager ?? throw new ArgumentNullException(nameof(repositoryManager));
			ByondManager = byondManager ?? throw new ArgumentNullException(nameof(byondManager));
			DreamMaker = dreamMaker ?? throw new ArgumentNullException(nameof(dreamMaker));
			Watchdog = watchdog ?? throw new ArgumentNullException(nameof(watchdog));
			Chat = chat ?? throw new ArgumentNullException(nameof(chat));
			Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
			CompileJobConsumer = compileJobConsumer ?? throw new ArgumentNullException(nameof(compileJobConsumer));
			this.databaseContextFactory = databaseContextFactory ?? throw new ArgumentNullException(nameof(databaseContextFactory));
			this.dmbFactory = dmbFactory ?? throw new ArgumentNullException(nameof(dmbFactory));
			this.jobManager = jobManager ?? throw new ArgumentNullException(nameof(jobManager));
			this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
		}

		/// <inheritdoc />
		public void Dispose()
		{
			timerCts?.Dispose();
			CompileJobConsumer.Dispose();
			Configuration.Dispose();
			Chat.Dispose();
			Watchdog.Dispose();
			RepositoryManager.Dispose();
		}

		/// <inheritdoc />
		public async Task CompileProcess(Job job, IDatabaseContext databaseContext, Action<int> progressReporter, CancellationToken cancellationToken)
		{
			//DO NOT FOLLOW THE SUGGESTION FOR A THROW EXPRESSION HERE
			if (job == null)
				throw new ArgumentNullException(nameof(job));
			if (databaseContext == null)
				throw new ArgumentNullException(nameof(databaseContext));
			if (progressReporter == null)
				throw new ArgumentNullException(nameof(progressReporter));

			var ddSettingsTask = databaseContext.DreamDaemonSettings.Where(x => x.InstanceId == metadata.Id).Select(x => new DreamDaemonSettings
			{
				StartupTimeout = x.StartupTimeout,
			}).FirstOrDefaultAsync(cancellationToken);

			var dreamMakerSettings = await databaseContext.DreamMakerSettings.Where(x => x.InstanceId == metadata.Id).FirstAsync(cancellationToken).ConfigureAwait(false);
			if (dreamMakerSettings == default)
				throw new JobException("Missing DreamMakerSettings in DB!");
			var ddSettings = await ddSettingsTask.ConfigureAwait(false);
			if (ddSettings == default)
				throw new JobException("Missing DreamDaemonSettings in DB!");

			CompileJob compileJob;
			RevisionInformation revInfo;
			using (var repo = await RepositoryManager.LoadRepository(cancellationToken).ConfigureAwait(false))
			{
				if (repo == null)
					throw new JobException("Missing Repository!");

				var repoSha = repo.Head;
				revInfo = await databaseContext.RevisionInformations.Where(x => x.CommitSha == repoSha).Include(x => x.ActiveTestMerges).ThenInclude(x => x.TestMerge).FirstOrDefaultAsync().ConfigureAwait(false);

				if (revInfo == default)
				{
					revInfo = new RevisionInformation
					{
						CommitSha = repoSha,
						OriginCommitSha = repoSha,
						Instance = new Models.Instance
						{
							Id = metadata.Id
						}
					};
					logger.LogWarning(Repository.Repository.OriginTrackingErrorTemplate, repoSha);
					databaseContext.Instances.Attach(revInfo.Instance);
				}

				compileJob = await DreamMaker.Compile(revInfo, dreamMakerSettings, ddSettings.StartupTimeout.Value, repo, cancellationToken).ConfigureAwait(false);
			}

			compileJob.Job = job;

			databaseContext.CompileJobs.Add(compileJob);    //will be saved by job context

			job.PostComplete = ct => CompileJobConsumer.LoadCompileJob(compileJob, ct);
		}

		/// <summary>
		/// Pull the repository and compile for every set of given <paramref name="minutes"/>
		/// </summary>
		/// <param name="minutes">How many minutes the operation should repeat. Does not include running time</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		async Task TimerLoop(uint minutes, CancellationToken cancellationToken)
		{
			while (true)
				try
				{
					await Task.Delay(new TimeSpan(0, minutes > Int32.MaxValue ? Int32.MaxValue : (int)minutes, 0), cancellationToken).ConfigureAwait(false);

					try
					{
						Models.User user = null;
						await databaseContextFactory.UseContext(async (db) => user = await db.Users.FirstAsync(cancellationToken).ConfigureAwait(false)).ConfigureAwait(false);
						var repositoryUpdateJob = new Job
						{
							Instance = new Models.Instance
							{
								Id = metadata.Id
							},
							Description = "Scheduled repository update",
							CancelRightsType = RightsType.Repository,
							CancelRight = (ulong)RepositoryRights.CancelPendingChanges
						};

						var noRepo = false;
						await jobManager.RegisterOperation(repositoryUpdateJob, async (paramJob, databaseContext, progressReporter, jobCancellationToken) =>
						{
							var repositorySettingsTask = databaseContext.RepositorySettings.Where(x => x.InstanceId == metadata.Id).FirstAsync(jobCancellationToken);

							//assume 5 steps with synchronize
							const int ProgressSections = 7;
							const int ProgressStep = 100 / ProgressSections;


							const int NumSteps = 3;
							var doneSteps = 0;

							Action<int> NextProgressReporter()
							{
								var tmpDoneSteps = doneSteps;
								++doneSteps;
								return progress => progressReporter((progress + 100 * tmpDoneSteps) / NumSteps);
							};

							using (var repo = await RepositoryManager.LoadRepository(jobCancellationToken).ConfigureAwait(false))
							{
								if (repo == null)
								{
									//no repo, no auto updates
									noRepo = true;
									return;
								}

								var repositorySettings = await repositorySettingsTask.ConfigureAwait(false);
								
								//the main point of auto update is to pull the remote
								await repo.FetchOrigin(repositorySettings.AccessUser, repositorySettings.AccessToken, NextProgressReporter(), jobCancellationToken).ConfigureAwait(false);

								var startSha = repo.Head;

								//take appropriate auto update actions
								bool shouldSyncTracked;
								if (repositorySettings.AutoUpdatesKeepTestMerges.Value)
								{
									var result = await repo.MergeOrigin(repositorySettings.CommitterName, repositorySettings.CommitterEmail, NextProgressReporter(), jobCancellationToken).ConfigureAwait(false);
									if (!result.HasValue)
										return;
									shouldSyncTracked = result.Value;
								}
								else
								{
									await repo.ResetToOrigin(NextProgressReporter(), jobCancellationToken).ConfigureAwait(false);
									shouldSyncTracked = true;
								}

								//synch if necessary
								if (repositorySettings.AutoUpdatesSynchronize.Value && startSha != repo.Head)
									await repo.Sychronize(repositorySettings.AccessUser, repositorySettings.AccessToken, repositorySettings.CommitterName, repositorySettings.CommitterEmail, NextProgressReporter(), shouldSyncTracked, jobCancellationToken).ConfigureAwait(false);

								progressReporter(5 * ProgressStep);
							}
						}, cancellationToken).ConfigureAwait(false);

						await jobManager.WaitForJobCompletion(repositoryUpdateJob, user, cancellationToken, default).ConfigureAwait(false);

						if (noRepo)
							continue;

						//finally set up the job
						var compileProcessJob = new Job
						{
							StartedBy = user,
							Instance = repositoryUpdateJob.Instance,
							Description = "Scheduled code deployment",
							CancelRightsType = RightsType.DreamMaker,
							CancelRight = (ulong)DreamMakerRights.CancelCompile
						};

						await jobManager.RegisterOperation(compileProcessJob, CompileProcess, cancellationToken).ConfigureAwait(false);

						await jobManager.WaitForJobCompletion(compileProcessJob, user, cancellationToken, default).ConfigureAwait(false);
					}
					catch (OperationCanceledException)
					{
						logger.LogDebug("Cancelled auto update job!");
						throw;
					}
					catch (Exception e)
					{
						logger.LogWarning("Error in auto update loop! Exception: {0}", e);
						continue;
					}
				}
				catch (OperationCanceledException)
				{
					break;
				}
			logger.LogTrace("Leaving auto update loop...");
		}
		
		/// <inheritdoc />
		public void Rename(string newName)
		{
			if (String.IsNullOrWhiteSpace(newName))
				throw new ArgumentNullException(nameof(newName));
			metadata.Name = newName;
		}

		/// <inheritdoc />
		public async Task StartAsync(CancellationToken cancellationToken)
		{
			await Task.WhenAll(SetAutoUpdateInterval(metadata.AutoUpdateInterval.Value), Configuration.StartAsync(cancellationToken), ByondManager.StartAsync(cancellationToken), Chat.StartAsync(cancellationToken), CompileJobConsumer.StartAsync(cancellationToken)).ConfigureAwait(false);

			//dependent on so many things, its just safer this way
			await Watchdog.StartAsync(cancellationToken).ConfigureAwait(false);

			CompileJob latestCompileJob = null;
			await databaseContextFactory.UseContext(async db =>
			{
				latestCompileJob = await db.CompileJobs.Where(x => x.Job.Instance.Id == metadata.Id).OrderByDescending(x => x.Job.StoppedAt).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
			}).ConfigureAwait(false);
			await dmbFactory.CleanUnusedCompileJobs(latestCompileJob, cancellationToken).ConfigureAwait(false);
		}

		/// <inheritdoc />
		public Task StopAsync(CancellationToken cancellationToken) => Task.WhenAll(SetAutoUpdateInterval(0), Configuration.StopAsync(cancellationToken), ByondManager.StopAsync(cancellationToken), Watchdog.StopAsync(cancellationToken), Chat.StopAsync(cancellationToken), CompileJobConsumer.StopAsync(cancellationToken));

		/// <inheritdoc />
		public async Task SetAutoUpdateInterval(uint newInterval)
		{
			Task toWait;
			lock (this)
			{
				if (timerTask != null)
				{
					timerCts.Cancel();
					toWait = timerTask;
				}
				else
					toWait = Task.CompletedTask;
			}
			await toWait.ConfigureAwait(false);
			if (newInterval == 0)
				return;
			lock (this)
			{
				//race condition, just quit
				if (timerTask != null)
					return;
				timerCts?.Dispose();
				timerCts = new CancellationTokenSource();
				timerTask = TimerLoop(newInterval, timerCts.Token);
			}
		}

		/// <inheritdoc />
		public CompileJob LatestCompileJob()
		{
			if (!dmbFactory.DmbAvailable)
				return null;
			return dmbFactory.LockNextDmb(0)?.CompileJob;
		}
	}
}
