using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Octokit;
using System;
using System.Collections.Generic;
using System.Globalization;
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
	#pragma warning disable CA1506 // TODO: Decomplexify
	sealed class Instance : IInstance
	{
		/// <inheritdoc />
		public IRepositoryManager RepositoryManager { get; }

		/// <inheritdoc />
		public IByondManager ByondManager { get; }

		/// <inheritdoc />
		public IWatchdog Watchdog { get; }

		/// <inheritdoc />
		public IChat Chat { get; }

		/// <inheritdoc />
		public StaticFiles.IConfiguration Configuration { get; }

		/// <summary>
		/// The <see cref="IDreamMaker"/> for the <see cref="Instance"/>
		/// </summary>
		readonly IDreamMaker dreamMaker;

		/// <summary>
		/// The <see cref="ICompileJobConsumer"/> for the <see cref="Instance"/>
		/// </summary>
		readonly ICompileJobConsumer compileJobConsumer;

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
		/// The <see cref="IEventConsumer"/> for the <see cref="Instance"/>
		/// </summary>
		readonly IEventConsumer eventConsumer;

		/// <summary>
		/// The <see cref="IGitHubClientFactory"/> for the <see cref="Instance"/>
		/// </summary>
		readonly IGitHubClientFactory gitHubClientFactory;

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
		/// <param name="dreamMaker">The value of <see cref="dreamMaker"/></param>
		/// <param name="watchdog">The value of <see cref="Watchdog"/></param>
		/// <param name="chat">The value of <see cref="Chat"/></param>
		/// <param name="configuration">The value of <see cref="Configuration"/></param>
		/// <param name="compileJobConsumer">The value of <see cref="compileJobConsumer"/></param>
		/// <param name="databaseContextFactory">The value of <see cref="databaseContextFactory"/></param>
		/// <param name="dmbFactory">The value of <see cref="dmbFactory"/></param>
		/// <param name="jobManager">The value of <see cref="jobManager"/></param>
		/// <param name="eventConsumer">The value of <see cref="eventConsumer"/></param>
		/// <param name="gitHubClientFactory">The value of <see cref="gitHubClientFactory"/></param>
		/// <param name="logger">The value of <see cref="logger"/></param>
		public Instance(Api.Models.Instance metadata, IRepositoryManager repositoryManager, IByondManager byondManager, IDreamMaker dreamMaker, IWatchdog watchdog, IChat chat, StaticFiles.IConfiguration configuration, ICompileJobConsumer compileJobConsumer, IDatabaseContextFactory databaseContextFactory, IDmbFactory dmbFactory, IJobManager jobManager, IEventConsumer eventConsumer, IGitHubClientFactory gitHubClientFactory, ILogger<Instance> logger)
		{
			this.metadata = metadata ?? throw new ArgumentNullException(nameof(metadata));
			RepositoryManager = repositoryManager ?? throw new ArgumentNullException(nameof(repositoryManager));
			ByondManager = byondManager ?? throw new ArgumentNullException(nameof(byondManager));
			this.dreamMaker = dreamMaker ?? throw new ArgumentNullException(nameof(dreamMaker));
			Watchdog = watchdog ?? throw new ArgumentNullException(nameof(watchdog));
			Chat = chat ?? throw new ArgumentNullException(nameof(chat));
			Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
			this.compileJobConsumer = compileJobConsumer ?? throw new ArgumentNullException(nameof(compileJobConsumer));
			this.databaseContextFactory = databaseContextFactory ?? throw new ArgumentNullException(nameof(databaseContextFactory));
			this.dmbFactory = dmbFactory ?? throw new ArgumentNullException(nameof(dmbFactory));
			this.jobManager = jobManager ?? throw new ArgumentNullException(nameof(jobManager));
			this.eventConsumer = eventConsumer ?? throw new ArgumentNullException(nameof(eventConsumer));
			this.gitHubClientFactory = gitHubClientFactory ?? throw new ArgumentNullException(nameof(gitHubClientFactory));
			this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
		}

		/// <inheritdoc />
		public void Dispose()
		{
			timerCts?.Dispose();
			compileJobConsumer.Dispose();
			Configuration.Dispose();
			Chat.Dispose();
			Watchdog.Dispose();
			RepositoryManager.Dispose();
		}

		/// <inheritdoc />
		public async Task CompileProcess(Job job, IDatabaseContext databaseContext, Action<int> progressReporter, CancellationToken cancellationToken)
		{
#pragma warning disable IDE0016 // Use 'throw' expression
			if (job == null)
				throw new ArgumentNullException(nameof(job));
#pragma warning restore IDE0016 // Use 'throw' expression
			if (databaseContext == null)
				throw new ArgumentNullException(nameof(databaseContext));
			if (progressReporter == null)
				throw new ArgumentNullException(nameof(progressReporter));

			var ddSettingsTask = databaseContext.DreamDaemonSettings.Where(x => x.InstanceId == metadata.Id).Select(x => new DreamDaemonSettings
			{
				StartupTimeout = x.StartupTimeout,
			}).FirstOrDefaultAsync(cancellationToken);

			var compileJobsTask = databaseContext.CompileJobs
				.Where(x => x.Job.Instance.Id == metadata.Id)
				.OrderByDescending(x => x.Job.StoppedAt)
				.Select(x => new Job
				{
					StoppedAt = x.Job.StoppedAt,
					StartedAt = x.Job.StartedAt
				})
				.Take(10)
				.ToListAsync(cancellationToken);

			var dreamMakerSettings = await databaseContext.DreamMakerSettings.Where(x => x.InstanceId == metadata.Id).FirstAsync(cancellationToken).ConfigureAwait(false);
			if (dreamMakerSettings == default)
				throw new JobException("Missing DreamMakerSettings in DB!");
			var ddSettings = await ddSettingsTask.ConfigureAwait(false);
			if (ddSettings == default)
				throw new JobException("Missing DreamDaemonSettings in DB!");

			Task<RepositorySettings> repositorySettingsTask = null;
			string repoOwner = null;
			string repoName = null;
			CompileJob compileJob;
			RevisionInformation revInfo;
			using (var repo = await RepositoryManager.LoadRepository(cancellationToken).ConfigureAwait(false))
			{
				if (repo == null)
					throw new JobException("Missing Repository!");

				if (repo.IsGitHubRepository)
				{
					repoOwner = repo.GitHubOwner;
					repoName = repo.GitHubRepoName;
					repositorySettingsTask = databaseContext.RepositorySettings.Where(x => x.InstanceId == metadata.Id).Select(x => new RepositorySettings
					{
						AccessToken = x.AccessToken,
						ShowTestMergeCommitters = x.ShowTestMergeCommitters
					}).FirstOrDefaultAsync(cancellationToken);
				}

				var repoSha = repo.Head;
				revInfo = await databaseContext.RevisionInformations.Where(x => x.CommitSha == repoSha && x.Instance.Id == metadata.Id).Include(x => x.ActiveTestMerges).ThenInclude(x => x.TestMerge).ThenInclude(x => x.MergedBy).FirstOrDefaultAsync().ConfigureAwait(false);

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

				TimeSpan? averageSpan = null;
				var previousCompileJobs = await compileJobsTask.ConfigureAwait(false);
				if(previousCompileJobs.Count != 0)
				{
					var totalSpan = TimeSpan.Zero;
					foreach (var I in previousCompileJobs)
						totalSpan += I.StoppedAt.Value - I.StartedAt.Value;
					averageSpan = totalSpan / previousCompileJobs.Count;
				}

				compileJob = await dreamMaker.Compile(revInfo, dreamMakerSettings, ddSettings.StartupTimeout.Value, repo, progressReporter, averageSpan, cancellationToken).ConfigureAwait(false);
			}

			compileJob.Job = job;

			databaseContext.CompileJobs.Add(compileJob); // will be saved by job context

			job.PostComplete = ct => compileJobConsumer.LoadCompileJob(compileJob, ct);

			if (repositorySettingsTask != null)
			{
				var repositorySettings = await repositorySettingsTask.ConfigureAwait(false);
				if (repositorySettings == default)
					throw new JobException("Missing repository settings!");

				if (repositorySettings.AccessToken != null)
				{
					// potential for commenting on a test merge change
					var outgoingCompileJob = LatestCompileJob();

					if(outgoingCompileJob != null && outgoingCompileJob.RevisionInformation.CommitSha != compileJob.RevisionInformation.CommitSha)
					{
						var gitHubClient = gitHubClientFactory.CreateClient(repositorySettings.AccessToken);

						async Task CommentOnPR(int prNumber, string comment)
						{
							try
							{
								await gitHubClient.Issue.Comment.Create(repoOwner, repoName, prNumber, comment).ConfigureAwait(false);
							}
							catch (ApiException e)
							{
								logger.LogWarning("Error posting GitHub comment! Exception: {0}", e);
							}
						}

						var tasks = new List<Task>();

						string FormatTestMerge(TestMerge testMerge, bool updated) => String.Format(CultureInfo.InvariantCulture, "#### Test Merge {4}{0}{0}##### Server Instance{0}{5}{1}{0}{0}##### Revision{0}Origin: {6}{0}Pull Request: {2}{0}Server: {7}{3}",
							Environment.NewLine,
							repositorySettings.ShowTestMergeCommitters.Value ? String.Format(CultureInfo.InvariantCulture, "{0}{0}##### Merged By{0}{1}", Environment.NewLine, testMerge.MergedBy.Name) : String.Empty,
							testMerge.PullRequestRevision,
							testMerge.Comment != null ? String.Format(CultureInfo.InvariantCulture, "{0}{0}##### Comment{0}{1}", Environment.NewLine, testMerge.Comment) : String.Empty,
							updated ? "Updated" : "Deployed",
							metadata.Name,
							compileJob.RevisionInformation.OriginCommitSha,
							compileJob.RevisionInformation.CommitSha);

						// added prs
						foreach (var I in compileJob
							.RevisionInformation
							.ActiveTestMerges
							.Select(x => x.TestMerge)
							.Where(x => !outgoingCompileJob
								.RevisionInformation
								.ActiveTestMerges
								.Any(y => y.TestMerge.Number == x.Number)))
							tasks.Add(CommentOnPR(I.Number.Value, FormatTestMerge(I, false)));

						// removed prs
						foreach (var I in outgoingCompileJob
							.RevisionInformation
							.ActiveTestMerges
							.Select(x => x.TestMerge)
								.Where(x => !compileJob
								.RevisionInformation
								.ActiveTestMerges
								.Any(y => y.TestMerge.Number == x.Number)))
							tasks.Add(CommentOnPR(I.Number.Value, "#### Test Merge Removed"));

						// updated prs
						foreach(var I in compileJob
							.RevisionInformation
							.ActiveTestMerges
							.Select(x => x.TestMerge)
							.Where(x => outgoingCompileJob
								.RevisionInformation
								.ActiveTestMerges
								.Any(y => y.TestMerge.Number == x.Number)))
							tasks.Add(CommentOnPR(I.Number.Value, FormatTestMerge(I, true)));

						if (tasks.Any())
							await Task.WhenAll(tasks).ConfigureAwait(false);
					}
				}
			}
		}

		/// <summary>
		/// Pull the repository and compile for every set of given <paramref name="minutes"/>
		/// </summary>
		/// <param name="minutes">How many minutes the operation should repeat. Does not include running time</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		#pragma warning disable CA1502 // TODO: Decomplexify
		async Task TimerLoop(uint minutes, CancellationToken cancellationToken)
		{
			while (true)
				try
				{
					await Task.Delay(TimeSpan.FromMinutes(minutes > Int32.MaxValue ? Int32.MaxValue : (int)minutes), cancellationToken).ConfigureAwait(false);
					logger.LogDebug("Beginning auto update...");
					await eventConsumer.HandleEvent(EventType.InstanceAutoUpdateStart, new List<string>(), cancellationToken).ConfigureAwait(false);
					try
					{
						Models.User user = null;
						await databaseContextFactory.UseContext(async (db) => user = await db.Users.Where(x => x.CanonicalName == Api.Models.User.AdminName.ToUpperInvariant()).FirstAsync(cancellationToken).ConfigureAwait(false)).ConfigureAwait(false);
						var repositoryUpdateJob = new Job
						{
							Instance = new Models.Instance
							{
								Id = metadata.Id
							},
							Description = "Scheduled repository update",
							CancelRightsType = RightsType.Repository,
							CancelRight = (ulong)RepositoryRights.CancelPendingChanges,
							StartedBy = user
						};

						string deploySha = null;
						await jobManager.RegisterOperation(repositoryUpdateJob, async (paramJob, databaseContext, progressReporter, jobCancellationToken) =>
						{
							var repositorySettingsTask = databaseContext.RepositorySettings.Where(x => x.InstanceId == metadata.Id).FirstAsync(jobCancellationToken);

							// assume 5 steps with synchronize
							const int ProgressSections = 7;
							const int ProgressStep = 100 / ProgressSections;

							const int NumSteps = 3;
							var doneSteps = 0;

							Action<int> NextProgressReporter()
							{
								var tmpDoneSteps = doneSteps;
								++doneSteps;
								return progress => progressReporter((progress + (100 * tmpDoneSteps)) / NumSteps);
							}

							using (var repo = await RepositoryManager.LoadRepository(jobCancellationToken).ConfigureAwait(false))
							{
								if (repo == null)
								{
									logger.LogTrace("Aborting repo update, no repository!");
									return;
								}

								var startSha = repo.Head;
								if (!repo.Tracking)
								{
									logger.LogTrace("Aborting repo update, not tracking origin!");
									deploySha = startSha;
									return;
								}

								var repositorySettings = await repositorySettingsTask.ConfigureAwait(false);

								// the main point of auto update is to pull the remote
								await repo.FetchOrigin(repositorySettings.AccessUser, repositorySettings.AccessToken, NextProgressReporter(), jobCancellationToken).ConfigureAwait(false);

								RevisionInformation currentRevInfo = null;
								bool hasDbChanges = false;

								Task<RevisionInformation> LoadRevInfo() => databaseContext.RevisionInformations
										.Where(x => x.CommitSha == startSha && x.Instance.Id == metadata.Id)
										.Include(x => x.ActiveTestMerges).ThenInclude(x => x.TestMerge)
										.FirstOrDefaultAsync(cancellationToken);

								async Task UpdateRevInfo(string currentHead, bool onOrigin)
								{
									if(currentRevInfo == null)
										currentRevInfo = await LoadRevInfo().ConfigureAwait(false);

									if (currentRevInfo == default)
									{
										logger.LogWarning(Repository.Repository.OriginTrackingErrorTemplate, currentHead);
										onOrigin = true;
									}

									var attachedInstance = new Models.Instance
									{
										Id = metadata.Id
									};
									var oldRevInfo = currentRevInfo;
									currentRevInfo = new RevisionInformation
									{
										CommitSha = currentHead,
										OriginCommitSha = onOrigin ? currentHead : oldRevInfo.OriginCommitSha,
										Instance = attachedInstance
									};
									if (!onOrigin)
										currentRevInfo.ActiveTestMerges = new List<RevInfoTestMerge>(oldRevInfo.ActiveTestMerges);

									databaseContext.Instances.Attach(attachedInstance);
									databaseContext.RevisionInformations.Add(currentRevInfo);
									hasDbChanges = true;
								}

								// take appropriate auto update actions
								bool shouldSyncTracked;
								if (repositorySettings.AutoUpdatesKeepTestMerges.Value)
								{
									logger.LogTrace("Preserving test merges...");

									var currentRevInfoTask = LoadRevInfo();

									var result = await repo.MergeOrigin(repositorySettings.CommitterName, repositorySettings.CommitterEmail, NextProgressReporter(), jobCancellationToken).ConfigureAwait(false);

									if (!result.HasValue)
										throw new JobException("Merge conflict while preserving test merges!");

									currentRevInfo = await currentRevInfoTask.ConfigureAwait(false);

									var lastRevInfoWasOriginCommit = currentRevInfo == default || currentRevInfo.CommitSha == currentRevInfo.OriginCommitSha;
									var stillOnOrigin = result.Value && lastRevInfoWasOriginCommit;

									var currentHead = repo.Head;
									if (currentHead != startSha)
									{
										await UpdateRevInfo(currentHead, stillOnOrigin).ConfigureAwait(false);
										shouldSyncTracked = stillOnOrigin;
									}
									else
										shouldSyncTracked = false;
								}
								else
								{
									logger.LogTrace("Not preserving test merges...");
									await repo.ResetToOrigin(NextProgressReporter(), jobCancellationToken).ConfigureAwait(false);

									var currentHead = repo.Head;

									currentRevInfo = await databaseContext.RevisionInformations
									.Where(x => x.CommitSha == currentHead && x.Instance.Id == metadata.Id)
									.FirstOrDefaultAsync(jobCancellationToken).ConfigureAwait(false);

									if (currentHead != startSha && currentRevInfo != default)
										await UpdateRevInfo(currentHead, true).ConfigureAwait(false);

									shouldSyncTracked = true;
								}

								// synch if necessary
								if (repositorySettings.AutoUpdatesSynchronize.Value && startSha != repo.Head)
								{
									var pushedOrigin = await repo.Sychronize(repositorySettings.AccessUser, repositorySettings.AccessToken, repositorySettings.CommitterName, repositorySettings.CommitterEmail, NextProgressReporter(), shouldSyncTracked, jobCancellationToken).ConfigureAwait(false);
									var currentHead = repo.Head;
									if (currentHead != currentRevInfo.CommitSha)
										await UpdateRevInfo(currentHead, pushedOrigin).ConfigureAwait(false);
								}

								if(hasDbChanges)
									try
									{
										await databaseContext.Save(cancellationToken).ConfigureAwait(false);
									}
									catch
									{
										await repo.ResetToSha(startSha, progressReporter, default).ConfigureAwait(false);
										throw;
									}

								progressReporter(5 * ProgressStep);
								deploySha = repo.Head;
							}
						}, cancellationToken).ConfigureAwait(false);

						await jobManager.WaitForJobCompletion(repositoryUpdateJob, user, cancellationToken, default).ConfigureAwait(false);

						if (deploySha == null)
						{
							logger.LogTrace("Aborting auto update, repository error!");
							continue;
						}

						if(deploySha == LatestCompileJob()?.RevisionInformation.CommitSha)
						{
							logger.LogTrace("Aborting auto update, same revision as latest CompileJob");
							continue;
						}

						// finally set up the job
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
		#pragma warning restore CA1502

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
			await Task.WhenAll(SetAutoUpdateInterval(metadata.AutoUpdateInterval.Value), Configuration.StartAsync(cancellationToken), ByondManager.StartAsync(cancellationToken), Chat.StartAsync(cancellationToken), compileJobConsumer.StartAsync(cancellationToken)).ConfigureAwait(false);

			// dependent on so many things, its just safer this way
			await Watchdog.StartAsync(cancellationToken).ConfigureAwait(false);

			CompileJob latestCompileJob = null;
			await databaseContextFactory.UseContext(async db =>
			{
				latestCompileJob = await db.CompileJobs.Where(x => x.Job.Instance.Id == metadata.Id).OrderByDescending(x => x.Job.StoppedAt).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
			}).ConfigureAwait(false);
			await dmbFactory.CleanUnusedCompileJobs(latestCompileJob, cancellationToken).ConfigureAwait(false);
		}

		/// <inheritdoc />
		public Task StopAsync(CancellationToken cancellationToken) => Task.WhenAll(SetAutoUpdateInterval(0), Configuration.StopAsync(cancellationToken), ByondManager.StopAsync(cancellationToken), Watchdog.StopAsync(cancellationToken), Chat.StopAsync(cancellationToken), compileJobConsumer.StopAsync(cancellationToken));

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
				// race condition, just quit
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
