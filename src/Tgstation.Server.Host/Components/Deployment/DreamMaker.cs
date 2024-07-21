using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using Tgstation.Server.Api.Models;
using Tgstation.Server.Api.Models.Internal;
using Tgstation.Server.Common.Extensions;
using Tgstation.Server.Host.Components.Chat;
using Tgstation.Server.Host.Components.Deployment.Remote;
using Tgstation.Server.Host.Components.Engine;
using Tgstation.Server.Host.Components.Events;
using Tgstation.Server.Host.Components.Repository;
using Tgstation.Server.Host.Components.Session;
using Tgstation.Server.Host.Configuration;
using Tgstation.Server.Host.Database;
using Tgstation.Server.Host.IO;
using Tgstation.Server.Host.Jobs;
using Tgstation.Server.Host.Models;
using Tgstation.Server.Host.System;
using Tgstation.Server.Host.Utils;

namespace Tgstation.Server.Host.Components.Deployment
{
	/// <inheritdoc />
	sealed class DreamMaker : IDreamMaker
	{
		/// <summary>
		/// Extension for .dmes.
		/// </summary>
		const string DmeExtension = "dme";

		/// <summary>
		/// The <see cref="IEngineManager"/> for <see cref="DreamMaker"/>.
		/// </summary>
		readonly IEngineManager engineManager;

		/// <summary>
		/// The <see cref="IIOManager"/> for <see cref="DreamMaker"/>.
		/// </summary>
		readonly IIOManager ioManager;

		/// <summary>
		/// The <see cref="StaticFiles.IConfiguration"/> for <see cref="DreamMaker"/>.
		/// </summary>
		readonly StaticFiles.IConfiguration configuration;

		/// <summary>
		/// The <see cref="ISessionControllerFactory"/> for <see cref="DreamMaker"/>.
		/// </summary>
		readonly ISessionControllerFactory sessionControllerFactory;

		/// <summary>
		/// The <see cref="IEventConsumer"/> for <see cref="DreamMaker"/>.
		/// </summary>
		readonly IEventConsumer eventConsumer;

		/// <summary>
		/// The <see cref="IChatManager"/> for <see cref="DreamMaker"/>.
		/// </summary>
		readonly IChatManager chatManager;

		/// <summary>
		/// The <see cref="IProcessExecutor"/> for <see cref="DreamMaker"/>.
		/// </summary>
		readonly IProcessExecutor processExecutor;

		/// <summary>
		/// The <see cref="IRepositoryManager"/> for <see cref="DreamMaker"/>.
		/// </summary>
		readonly IRepositoryManager repositoryManager;

		/// <summary>
		/// The <see cref="ICompileJobSink"/> for <see cref="DreamMaker"/>.
		/// </summary>
		readonly ICompileJobSink compileJobConsumer;

		/// <summary>
		/// The <see cref="IRemoteDeploymentManagerFactory"/> for <see cref="DreamMaker"/>.
		/// </summary>
		readonly IRemoteDeploymentManagerFactory remoteDeploymentManagerFactory;

		/// <summary>
		/// The <see cref="IAsyncDelayer"/> for <see cref="DreamMaker"/>.
		/// </summary>
		readonly IAsyncDelayer asyncDelayer;

		/// <summary>
		/// The <see cref="ILogger"/> for <see cref="DreamMaker"/>.
		/// </summary>
		readonly ILogger<DreamMaker> logger;

		/// <summary>
		/// The <see cref="SessionConfiguration"/> for <see cref="DreamMaker"/>.
		/// </summary>
		readonly SessionConfiguration sessionConfiguration;

		/// <summary>
		/// The <see cref="Instance"/> <see cref="DreamMaker"/> belongs to.
		/// </summary>
		readonly Api.Models.Instance metadata;

		/// <summary>
		/// <see langword="lock"/> <see cref="object"/> for <see cref="deploying"/>.
		/// </summary>
		readonly object deploymentLock;

		/// <summary>
		/// The active callback from <see cref="IChatManager.QueueDeploymentMessage"/>.
		/// </summary>
		Func<string?, string, Action<bool>>? currentChatCallback;

		/// <summary>
		/// Cached for <see cref="currentChatCallback"/>.
		/// </summary>
		string? currentDreamMakerOutput;

		/// <summary>
		/// If a compile job is running.
		/// </summary>
		bool deploying;

		/// <summary>
		/// Format a given <see cref="Exception"/> for display to users.
		/// </summary>
		/// <param name="exception">The <see cref="Exception"/> to format.</param>
		/// <returns>An error <see cref="string"/> for end users.</returns>
		static string FormatExceptionForUsers(Exception exception)
			=> exception is OperationCanceledException
				? "The job was cancelled!"
				: exception.Message;

		/// <summary>
		/// Initializes a new instance of the <see cref="DreamMaker"/> class.
		/// </summary>
		/// <param name="engineManager">The value of <see cref="engineManager"/>.</param>
		/// <param name="ioManager">The value of <see cref="ioManager"/>.</param>
		/// <param name="configuration">The value of <see cref="configuration"/>.</param>
		/// <param name="sessionControllerFactory">The value of <see cref="sessionControllerFactory"/>.</param>
		/// <param name="eventConsumer">The value of <see cref="eventConsumer"/>.</param>
		/// <param name="chatManager">The value of <see cref="chatManager"/>.</param>
		/// <param name="processExecutor">The value of <see cref="processExecutor"/>.</param>
		/// <param name="compileJobConsumer">The value of <see cref="compileJobConsumer"/>.</param>
		/// <param name="repositoryManager">The value of <see cref="repositoryManager"/>.</param>
		/// <param name="remoteDeploymentManagerFactory">The value of <see cref="remoteDeploymentManagerFactory"/>.</param>
		/// <param name="asyncDelayer">The value of <see cref="asyncDelayer"/>.</param>
		/// <param name="logger">The value of <see cref="logger"/>.</param>
		/// <param name="sessionConfiguration">The value of <see cref="sessionConfiguration"/>.</param>
		/// <param name="metadata">The value of <see cref="metadata"/>.</param>
		public DreamMaker(
			IEngineManager engineManager,
			IIOManager ioManager,
			StaticFiles.IConfiguration configuration,
			ISessionControllerFactory sessionControllerFactory,
			IEventConsumer eventConsumer,
			IChatManager chatManager,
			IProcessExecutor processExecutor,
			ICompileJobSink compileJobConsumer,
			IRepositoryManager repositoryManager,
			IRemoteDeploymentManagerFactory remoteDeploymentManagerFactory,
			IAsyncDelayer asyncDelayer,
			ILogger<DreamMaker> logger,
			SessionConfiguration sessionConfiguration,
			Api.Models.Instance metadata)
		{
			this.engineManager = engineManager ?? throw new ArgumentNullException(nameof(engineManager));
			this.ioManager = ioManager ?? throw new ArgumentNullException(nameof(ioManager));
			this.configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
			this.sessionControllerFactory = sessionControllerFactory ?? throw new ArgumentNullException(nameof(sessionControllerFactory));
			this.eventConsumer = eventConsumer ?? throw new ArgumentNullException(nameof(eventConsumer));
			this.chatManager = chatManager ?? throw new ArgumentNullException(nameof(chatManager));
			this.processExecutor = processExecutor ?? throw new ArgumentNullException(nameof(processExecutor));
			this.compileJobConsumer = compileJobConsumer ?? throw new ArgumentNullException(nameof(compileJobConsumer));
			this.repositoryManager = repositoryManager ?? throw new ArgumentNullException(nameof(repositoryManager));
			this.asyncDelayer = asyncDelayer ?? throw new ArgumentNullException(nameof(asyncDelayer));
			this.remoteDeploymentManagerFactory = remoteDeploymentManagerFactory ?? throw new ArgumentNullException(nameof(remoteDeploymentManagerFactory));
			this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
			this.sessionConfiguration = sessionConfiguration ?? throw new ArgumentNullException(nameof(sessionConfiguration));
			this.metadata = metadata ?? throw new ArgumentNullException(nameof(metadata));

			deploymentLock = new object();
		}

		/// <inheritdoc />
#pragma warning disable CA1506
		public async ValueTask DeploymentProcess(
			Models.Job job,
			IDatabaseContextFactory databaseContextFactory,
			JobProgressReporter progressReporter,
			CancellationToken cancellationToken)
		{
			ArgumentNullException.ThrowIfNull(job);
			ArgumentNullException.ThrowIfNull(databaseContextFactory);
			ArgumentNullException.ThrowIfNull(progressReporter);

			lock (deploymentLock)
			{
				if (deploying)
					throw new JobException(ErrorCode.DeploymentInProgress);
				deploying = true;
			}

			currentChatCallback = null;
			currentDreamMakerOutput = null;
			Models.CompileJob? compileJob = null;
			try
			{
				string? repoOwner = null;
				string? repoName = null;
				TimeSpan? averageSpan = null;
				Models.RepositorySettings? repositorySettings = null;
				Models.DreamDaemonSettings? ddSettings = null;
				Models.DreamMakerSettings? dreamMakerSettings = null;
				IRepository? repo = null;
				IRemoteDeploymentManager? remoteDeploymentManager = null;
				Models.RevisionInformation? revInfo = null;
				await databaseContextFactory.UseContext(
					async databaseContext =>
					{
						averageSpan = await CalculateExpectedDeploymentTime(databaseContext, cancellationToken);

						ddSettings = await databaseContext
							.DreamDaemonSettings
							.AsQueryable()
							.Where(x => x.InstanceId == metadata.Id)
							.Select(x => new Models.DreamDaemonSettings
							{
								StartupTimeout = x.StartupTimeout,
								LogOutput = x.LogOutput,
							})
							.FirstOrDefaultAsync(cancellationToken);
						if (ddSettings == default)
							throw new JobException(ErrorCode.InstanceMissingDreamDaemonSettings);

						dreamMakerSettings = await databaseContext
							.DreamMakerSettings
							.AsQueryable()
							.Where(x => x.InstanceId == metadata.Id)
							.FirstAsync(cancellationToken);
						if (dreamMakerSettings == default)
							throw new JobException(ErrorCode.InstanceMissingDreamMakerSettings);

						repositorySettings = await databaseContext
							.RepositorySettings
							.AsQueryable()
							.Where(x => x.InstanceId == metadata.Id)
							.Select(x => new Models.RepositorySettings
							{
								AccessToken = x.AccessToken,
								AccessUser = x.AccessUser,
								ShowTestMergeCommitters = x.ShowTestMergeCommitters,
								PushTestMergeCommits = x.PushTestMergeCommits,
								PostTestMergeComment = x.PostTestMergeComment,
							})
							.FirstOrDefaultAsync(cancellationToken);
						if (repositorySettings == default)
							throw new JobException(ErrorCode.InstanceMissingRepositorySettings);

						repo = await repositoryManager.LoadRepository(cancellationToken);
						try
						{
							if (repo == null)
								throw new JobException(ErrorCode.RepoMissing);

							remoteDeploymentManager = remoteDeploymentManagerFactory
								.CreateRemoteDeploymentManager(metadata, repo.RemoteGitProvider!.Value);

							var repoSha = repo.Head;
							repoOwner = repo.RemoteRepositoryOwner;
							repoName = repo.RemoteRepositoryName;
							revInfo = await databaseContext
								.RevisionInformations
								.AsQueryable()
								.Where(x => x.CommitSha == repoSha && x.InstanceId == metadata.Id)
								.Include(x => x.ActiveTestMerges!)
									.ThenInclude(x => x.TestMerge!)
										.ThenInclude(x => x.MergedBy)
								.FirstOrDefaultAsync(cancellationToken);

							if (revInfo == null)
							{
								revInfo = new Models.RevisionInformation
								{
									CommitSha = repoSha,
									Timestamp = await repo.TimestampCommit(repoSha, cancellationToken),
									OriginCommitSha = repoSha,
									Instance = new Models.Instance
									{
										Id = metadata.Id,
									},
									ActiveTestMerges = new List<RevInfoTestMerge>(),
								};

								logger.LogInformation(Repository.Repository.OriginTrackingErrorTemplate, repoSha);
								databaseContext.RevisionInformations.Add(revInfo);
								databaseContext.Instances.Attach(revInfo.Instance);
								await databaseContext.Save(cancellationToken);
							}
						}
						catch
						{
							repo?.Dispose();
							throw;
						}
					});

				var likelyPushedTestMergeCommit =
					repositorySettings!.PushTestMergeCommits!.Value
					&& repositorySettings.AccessToken != null
					&& repositorySettings.AccessUser != null;
				using (repo)
					compileJob = await Compile(
						job,
						revInfo!,
						dreamMakerSettings!,
						ddSettings!,
						repo!,
						remoteDeploymentManager!,
						progressReporter,
						averageSpan,
						likelyPushedTestMergeCommit,
						cancellationToken);

				var activeCompileJob = await compileJobConsumer.LatestCompileJob();
				try
				{
					await databaseContextFactory.UseContext(
						async databaseContext =>
						{
							var fullJob = compileJob.Job;
							compileJob.Job = new Models.Job(job.Require(x => x.Id));
							var fullRevInfo = compileJob.RevisionInformation;
							compileJob.RevisionInformation = new Models.RevisionInformation
							{
								Id = revInfo!.Id,
							};

							databaseContext.Jobs.Attach(compileJob.Job);
							databaseContext.RevisionInformations.Attach(compileJob.RevisionInformation);
							databaseContext.CompileJobs.Add(compileJob);

							// The difficulty with compile jobs is they have a two part commit
							await databaseContext.Save(cancellationToken);
							logger.LogTrace("Created CompileJob {compileJobId}", compileJob.Id);
							try
							{
								var chatNotificationAction = currentChatCallback!(null, compileJob.Output!);
								await compileJobConsumer.LoadCompileJob(compileJob, chatNotificationAction, cancellationToken);
							}
							catch
							{
								// So we need to un-commit the compile job if the above throws
								databaseContext.CompileJobs.Remove(compileJob);

								// DCT: Cancellation token is for job, operation must run regardless
								await databaseContext.Save(CancellationToken.None);
								throw;
							}

							compileJob.Job = fullJob;
							compileJob.RevisionInformation = fullRevInfo;
						});
				}
				catch (Exception ex)
				{
					await CleanupFailedCompile(compileJob, remoteDeploymentManager!, ex);
					throw;
				}

				var commentsTask = remoteDeploymentManager!.PostDeploymentComments(
					compileJob,
					activeCompileJob?.RevisionInformation,
					repositorySettings,
					repoOwner,
					repoName,
					cancellationToken);

				var eventTask = eventConsumer.HandleEvent(EventType.DeploymentComplete, Enumerable.Empty<string>(), false, cancellationToken);

				try
				{
					await ValueTaskExtensions.WhenAll(commentsTask, eventTask);
				}
				catch (Exception ex)
				{
					throw new JobException(ErrorCode.PostDeployFailure, ex);
				}
				finally
				{
					currentChatCallback = null;
				}
			}
			catch (Exception ex)
			{
				currentChatCallback?.Invoke(
					FormatExceptionForUsers(ex),
					currentDreamMakerOutput!);

				throw;
			}
			finally
			{
				deploying = false;
			}
		}
#pragma warning restore CA1506

		/// <summary>
		/// Calculate the average length of a deployment using a given <paramref name="databaseContext"/>.
		/// </summary>
		/// <param name="databaseContext">The <see cref="IDatabaseContext"/> to retrieve previous deployment <see cref="Job"/>s from.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in the average <see cref="TimeSpan"/> of the 10 previous deployments or <see langword="null"/> if there are none.</returns>
		async ValueTask<TimeSpan?> CalculateExpectedDeploymentTime(IDatabaseContext databaseContext, CancellationToken cancellationToken)
		{
			var previousCompileJobs = await databaseContext
				.CompileJobs
				.AsQueryable()
				.Where(x => x.Job.Instance!.Id == metadata.Id)
				.OrderByDescending(x => x.Job.StoppedAt)
				.Take(10)
				.Select(x => new
				{
					StoppedAt = x.Job.StoppedAt!.Value,
					StartedAt = x.Job.StartedAt!.Value,
				})
				.ToListAsync(cancellationToken);

			TimeSpan? averageSpan = null;
			if (previousCompileJobs.Count != 0)
			{
				var totalSpan = TimeSpan.Zero;
				foreach (var previousCompileJob in previousCompileJobs)
					totalSpan += previousCompileJob.StoppedAt - previousCompileJob.StartedAt;
				averageSpan = totalSpan / previousCompileJobs.Count;
			}

			return averageSpan;
		}

		/// <summary>
		/// Run the compile implementation.
		/// </summary>
		/// <param name="job">The currently running <see cref="Job"/>.</param>
		/// <param name="revisionInformation">The <see cref="RevisionInformation"/>.</param>
		/// <param name="dreamMakerSettings">The <see cref="Api.Models.Internal.DreamMakerSettings"/>.</param>
		/// <param name="launchParameters">The <see cref="DreamDaemonLaunchParameters"/>.</param>
		/// <param name="repository">The <see cref="IRepository"/>.</param>
		/// <param name="remoteDeploymentManager">The <see cref="IRemoteDeploymentManager"/>.</param>
		/// <param name="progressReporter">The <see cref="JobProgressReporter"/> to report progress of the operation.</param>
		/// <param name="estimatedDuration">The optional estimated <see cref="TimeSpan"/> of the compilation.</param>
		/// <param name="localCommitExistsOnRemote">Whether or not the <paramref name="repository"/>'s current commit exists on the remote repository.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in the completed <see cref="CompileJob"/>.</returns>
		async ValueTask<Models.CompileJob> Compile(
			Models.Job job,
			Models.RevisionInformation revisionInformation,
			Api.Models.Internal.DreamMakerSettings dreamMakerSettings,
			DreamDaemonLaunchParameters launchParameters,
			IRepository repository,
			IRemoteDeploymentManager remoteDeploymentManager,
			JobProgressReporter progressReporter,
			TimeSpan? estimatedDuration,
			bool localCommitExistsOnRemote,
			CancellationToken cancellationToken)
		{
			logger.LogTrace("Begin Compile");

			using var progressCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

			progressReporter.StageName = "Reserving BYOND version";
			var progressTask = ProgressTask(progressReporter, estimatedDuration, progressCts.Token);
			try
			{
				using var engineLock = await engineManager.UseExecutables(null, null, cancellationToken);
				currentChatCallback = chatManager.QueueDeploymentMessage(
					revisionInformation,
					engineLock.Version,
					DateTimeOffset.UtcNow + estimatedDuration,
					repository.RemoteRepositoryOwner,
					repository.RemoteRepositoryName,
					localCommitExistsOnRemote);

				var compileJob = new Models.CompileJob(job, revisionInformation, engineLock.Version.ToString())
				{
					DirectoryName = Guid.NewGuid(),
					DmeName = dreamMakerSettings.ProjectName,
					RepositoryOrigin = repository.Origin.ToString(),
				};

				progressReporter.StageName = "Creating remote deployment notification";
				await remoteDeploymentManager.StartDeployment(
					repository,
					compileJob,
					cancellationToken);

				logger.LogTrace("Deployment will timeout at {timeoutTime}", DateTimeOffset.UtcNow + dreamMakerSettings.Timeout!.Value);
				using var timeoutTokenSource = new CancellationTokenSource(dreamMakerSettings.Timeout.Value);
				var timeoutToken = timeoutTokenSource.Token;
				using (timeoutToken.Register(() => logger.LogWarning("Deployment timed out!")))
				{
					using var combinedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(timeoutToken, cancellationToken);
					try
					{
						await RunCompileJob(
							progressReporter,
							compileJob,
							dreamMakerSettings,
							launchParameters,
							engineLock,
							repository,
							remoteDeploymentManager,
							combinedTokenSource.Token);
					}
					catch (OperationCanceledException) when (timeoutToken.IsCancellationRequested)
					{
						throw new JobException(ErrorCode.DeploymentTimeout);
					}
				}

				return compileJob;
			}
			catch (OperationCanceledException)
			{
				// DCT: Cancellation token is for job, delaying here is fine
				progressReporter.StageName = "Running CompileCancelled event";
				await eventConsumer.HandleEvent(EventType.CompileCancelled, Enumerable.Empty<string>(), true, CancellationToken.None);
				throw;
			}
			finally
			{
				progressCts.Cancel();
				await progressTask;
			}
		}

		/// <summary>
		/// Executes and populate a given <paramref name="job"/>.
		/// </summary>
		/// <param name="progressReporter">The <see cref="JobProgressReporter"/> for the operation.</param>
		/// <param name="job">The <see cref="CompileJob"/> to run and populate.</param>
		/// <param name="dreamMakerSettings">The <see cref="Api.Models.Internal.DreamMakerSettings"/> to use.</param>
		/// <param name="launchParameters">The <see cref="DreamDaemonLaunchParameters"/> to use.</param>
		/// <param name="engineLock">The <see cref="IEngineExecutableLock"/> to use.</param>
		/// <param name="repository">The <see cref="IRepository"/> to use.</param>
		/// <param name="remoteDeploymentManager">The <see cref="IRemoteDeploymentManager"/> to use.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask"/> representing the running operation.</returns>
		async ValueTask RunCompileJob(
			JobProgressReporter progressReporter,
			Models.CompileJob job,
			Api.Models.Internal.DreamMakerSettings dreamMakerSettings,
			DreamDaemonLaunchParameters launchParameters,
			IEngineExecutableLock engineLock,
			IRepository repository,
			IRemoteDeploymentManager remoteDeploymentManager,
			CancellationToken cancellationToken)
		{
			var outputDirectory = job.DirectoryName!.Value.ToString();
			logger.LogTrace("Compile output GUID: {dirGuid}", outputDirectory);

			try
			{
				// copy the repository
				logger.LogTrace("Copying repository to game directory");
				progressReporter.StageName = "Copying repository";
				var resolvedOutputDirectory = ioManager.ResolvePath(outputDirectory);
				var repoOrigin = repository.Origin;
				var repoReference = repository.Reference;
				using (repository)
					await repository.CopyTo(resolvedOutputDirectory, cancellationToken);

				// repository closed now

				// run precompile scripts
				progressReporter.StageName = "Running PreCompile event";
				await eventConsumer.HandleEvent(
					EventType.CompileStart,
					new List<string>
					{
						resolvedOutputDirectory,
						repoOrigin.ToString(),
						engineLock.Version.ToString(),
						repoReference,
					},
					true,
					cancellationToken);

				// determine the dme
				progressReporter.StageName = "Determining .dme";
				if (job.DmeName == null)
				{
					logger.LogTrace("Searching for available .dmes");
					var foundPaths = await ioManager.GetFilesWithExtension(resolvedOutputDirectory, DmeExtension, true, cancellationToken);
					var foundPath = foundPaths.FirstOrDefault();
					if (foundPath == default)
						throw new JobException(ErrorCode.DeploymentNoDme);
					job.DmeName = foundPath.Substring(
						resolvedOutputDirectory.Length + 1,
						foundPath.Length - resolvedOutputDirectory.Length - DmeExtension.Length - 2); // +1 for . in extension
				}
				else
				{
					var targetDme = ioManager.ConcatPath(outputDirectory, String.Join('.', job.DmeName, DmeExtension));
					var targetDmeExists = await ioManager.FileExists(targetDme, cancellationToken);
					if (!targetDmeExists)
						throw new JobException(ErrorCode.DeploymentMissingDme);
				}

				logger.LogDebug("Selected \"{dmeName}.dme\" for compilation!", job.DmeName);

				progressReporter.StageName = "Modifying .dme";
				await ModifyDme(job, cancellationToken);

				// run precompile scripts
				progressReporter.StageName = "Running PreDreamMaker event";
				await eventConsumer.HandleEvent(
					EventType.PreDreamMaker,
					new List<string>
					{
						resolvedOutputDirectory,
						repoOrigin.ToString(),
						engineLock.Version.ToString(),
					},
					true,
					cancellationToken);

				// run compiler
				progressReporter.StageName = "Running Compiler";
				var compileSuceeded = await RunDreamMaker(engineLock, job, dreamMakerSettings.CompilerAdditionalArguments, cancellationToken);

				// Session takes ownership of the lock and Disposes it so save this for later
				var engineVersion = engineLock.Version;

				// verify api
				try
				{
					if (!compileSuceeded)
						throw new JobException(
							ErrorCode.DeploymentExitCode,
							new JobException($"Compilation failed:{Environment.NewLine}{Environment.NewLine}{job.Output}"));

					progressReporter.StageName = "Validating DMAPI";
					await VerifyApi(
						launchParameters.StartupTimeout!.Value,
						dreamMakerSettings.ApiValidationSecurityLevel!.Value,
						job,
						engineLock,
						dreamMakerSettings.ApiValidationPort!.Value,
						dreamMakerSettings.RequireDMApiValidation!.Value,
						launchParameters.LogOutput!.Value,
						cancellationToken);
				}
				catch (JobException)
				{
					// DD never validated or compile failed
					progressReporter.StageName = "Running CompileFailure event";
					await eventConsumer.HandleEvent(
						EventType.CompileFailure,
						new List<string>
						{
							resolvedOutputDirectory,
							compileSuceeded ? "1" : "0",
							engineVersion.ToString(),
						},
						true,
						cancellationToken);
					throw;
				}

				progressReporter.StageName = "Running CompileComplete event";
				await eventConsumer.HandleEvent(
					EventType.CompileComplete,
					new List<string>
					{
						resolvedOutputDirectory,
						engineVersion.ToString(),
					},
					true,
					cancellationToken);

				logger.LogTrace("Applying static game file symlinks...");
				progressReporter.StageName = "Symlinking GameStaticFiles";

				// symlink in the static data
				await configuration.SymlinkStaticFilesTo(resolvedOutputDirectory, cancellationToken);

				logger.LogDebug("Compile complete!");
			}
			catch (Exception ex)
			{
				progressReporter.StageName = "Cleaning output directory";
				await CleanupFailedCompile(job, remoteDeploymentManager, ex);
				throw;
			}
		}

		/// <summary>
		/// Gradually triggers a given <paramref name="progressReporter"/> over a given <paramref name="estimatedDuration"/>.
		/// </summary>
		/// <param name="progressReporter">The <see cref="JobProgressReporter"/> to report progress of the operation.</param>
		/// <param name="estimatedDuration">A <see cref="TimeSpan"/> representing the duration to give progress over if any.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask"/> representing the running operation.</returns>
		async ValueTask ProgressTask(JobProgressReporter progressReporter, TimeSpan? estimatedDuration, CancellationToken cancellationToken)
		{
			double? lastReport = estimatedDuration.HasValue ? 0 : null;
			progressReporter.ReportProgress(lastReport);

			var minimumSleepInterval = TimeSpan.FromMilliseconds(250);
			var sleepInterval = estimatedDuration.HasValue ? estimatedDuration.Value / 100 : minimumSleepInterval;

			if (estimatedDuration.HasValue)
			{
				logger.LogDebug("Compile is expected to take: {estimatedDuration}", estimatedDuration);
			}
			else
			{
				logger.LogTrace("No metric to estimate compile time.");
			}

			try
			{
				for (var iteration = 0; iteration < (estimatedDuration.HasValue ? 99 : Int32.MaxValue); ++iteration)
				{
					if (estimatedDuration.HasValue)
					{
						var nextInterval = DateTimeOffset.UtcNow + sleepInterval;
						do
						{
							var remainingSleepThisInterval = nextInterval - DateTimeOffset.UtcNow;
							var nextSleepSpan = remainingSleepThisInterval < minimumSleepInterval ? minimumSleepInterval : remainingSleepThisInterval;

							await asyncDelayer.Delay(nextSleepSpan, cancellationToken);
							progressReporter.ReportProgress(lastReport);
						}
						while (DateTimeOffset.UtcNow < nextInterval);
					}
					else
						await asyncDelayer.Delay(minimumSleepInterval, cancellationToken);

					lastReport = estimatedDuration.HasValue ? sleepInterval * (iteration + 1) / estimatedDuration.Value : null;
					progressReporter.ReportProgress(lastReport);
				}
			}
			catch (OperationCanceledException ex)
			{
				logger.LogTrace(ex, "ProgressTask aborted.");
			}
			catch (Exception ex)
			{
				logger.LogError(ex, "ProgressTask crashed!");
			}
		}

		/// <summary>
		/// Run a quick DD instance to test the DMAPI is installed on the target code.
		/// </summary>
		/// <param name="timeout">The timeout in seconds for validation.</param>
		/// <param name="securityLevel">The <see cref="DreamDaemonSecurity"/> level to use to validate the API.</param>
		/// <param name="job">The <see cref="CompileJob"/> for the operation.</param>
		/// <param name="engineLock">The current <see cref="IEngineExecutableLock"/>.</param>
		/// <param name="portToUse">The port to use for API validation.</param>
		/// <param name="requireValidate">If the API validation is required to complete the deployment.</param>
		/// <param name="logOutput">If output should be logged to the DreamDaemon Diagnostics folder.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask"/> representing the running operation.</returns>
		async ValueTask VerifyApi(
			uint timeout,
			DreamDaemonSecurity securityLevel,
			Models.CompileJob job,
			IEngineExecutableLock engineLock,
			ushort portToUse,
			bool requireValidate,
			bool logOutput,
			CancellationToken cancellationToken)
		{
			logger.LogTrace("Verifying {possiblyRequired}DMAPI...", requireValidate ? "required " : String.Empty);
			var launchParameters = new DreamDaemonLaunchParameters
			{
				AllowWebClient = false,
				Port = portToUse,
				OpenDreamTopicPort = 0,
				SecurityLevel = securityLevel,
				Visibility = DreamDaemonVisibility.Invisible,
				StartupTimeout = timeout,
				TopicRequestTimeout = 0, // not used
				HealthCheckSeconds = 0, // not used
				StartProfiler = false,
				LogOutput = logOutput,
				MapThreads = 1, // lowest possible amount
			};

			job.MinimumSecurityLevel = securityLevel; // needed for the TempDmbProvider

			ApiValidationStatus validationStatus;
			await using (var provider = new TemporaryDmbProvider(
				ioManager.ResolvePath(job.DirectoryName!.Value.ToString()),
				job,
				engineLock.Version))
			await using (var controller = await sessionControllerFactory.LaunchNew(provider, engineLock, launchParameters, true, cancellationToken))
			{
				var launchResult = await controller.LaunchResult.WaitAsync(cancellationToken);

				if (launchResult.StartupTime.HasValue)
					await controller.Lifetime.WaitAsync(cancellationToken);

				if (!controller.Lifetime.IsCompleted)
					await controller.DisposeAsync();

				validationStatus = controller.ApiValidationStatus;

				logger.LogTrace("API validation status: {validationStatus}", validationStatus);

				job.DMApiVersion = controller.DMApiVersion;
			}

			switch (validationStatus)
			{
				case ApiValidationStatus.RequiresUltrasafe:
					job.MinimumSecurityLevel = DreamDaemonSecurity.Ultrasafe;
					return;
				case ApiValidationStatus.RequiresSafe:
					job.MinimumSecurityLevel = DreamDaemonSecurity.Safe;
					return;
				case ApiValidationStatus.RequiresTrusted:
					job.MinimumSecurityLevel = DreamDaemonSecurity.Trusted;
					return;
				case ApiValidationStatus.NeverValidated:
					if (requireValidate)
						throw new JobException(ErrorCode.DeploymentNeverValidated);
					job.MinimumSecurityLevel = DreamDaemonSecurity.Ultrasafe;
					break;
				case ApiValidationStatus.BadValidationRequest:
				case ApiValidationStatus.Incompatible:
					throw new JobException(ErrorCode.DeploymentInvalidValidation);
				case ApiValidationStatus.UnaskedValidationRequest:
				default:
					throw new InvalidOperationException(
						$"Session controller returned unexpected ApiValidationStatus: {validationStatus}");
			}
		}

		/// <summary>
		/// Compiles a .dme with DreamMaker.
		/// </summary>
		/// <param name="engineLock">The <see cref="IEngineExecutableLock"/> to use.</param>
		/// <param name="job">The <see cref="CompileJob"/> for the operation.</param>
		/// <param name="additionalCompilerArguments">Additional arguments to be added to the compiler.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in <see langword="true"/> if compilation succeeded, <see langword="false"/> otherwise.</returns>
		async ValueTask<bool> RunDreamMaker(
			IEngineExecutableLock engineLock,
			Models.CompileJob job,
			string? additionalCompilerArguments,
			CancellationToken cancellationToken)
		{
			var environment = await engineLock.LoadEnv(logger, true, cancellationToken);
			var arguments = engineLock.FormatCompilerArguments($"{job.DmeName}.{DmeExtension}", additionalCompilerArguments);

			await using var dm = await processExecutor.LaunchProcess(
				engineLock.CompilerExePath,
				ioManager.ResolvePath(
					job.DirectoryName!.Value.ToString()),
				arguments,
				cancellationToken,
				environment,
				readStandardHandles: true,
				noShellExecute: true);

			if (sessionConfiguration.LowPriorityDeploymentProcesses)
				dm.AdjustPriority(false);

			int exitCode;
			using (cancellationToken.Register(() => dm.Terminate()))
				exitCode = (await dm.Lifetime).Value;
			cancellationToken.ThrowIfCancellationRequested();

			logger.LogDebug("DreamMaker exit code: {exitCode}", exitCode);
			job.Output = $"{await dm.GetCombinedOutput(cancellationToken)}{Environment.NewLine}{Environment.NewLine}Exit Code: {exitCode}";
			logger.LogDebug("DreamMaker output: {newLine}{output}", Environment.NewLine, job.Output);

			currentDreamMakerOutput = job.Output;
			return exitCode == 0;
		}

		/// <summary>
		/// Adds server side includes to the .dme being compiled.
		/// </summary>
		/// <param name="job">The <see cref="CompileJob"/> for the operation.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask"/> representing the running operation.</returns>
		async ValueTask ModifyDme(Models.CompileJob job, CancellationToken cancellationToken)
		{
			var dmeFileName = String.Join('.', job.DmeName, DmeExtension);
			var stringDirectoryName = job.DirectoryName!.Value.ToString();
			var dmePath = ioManager.ConcatPath(stringDirectoryName, dmeFileName);
			var dmeReadTask = ioManager.ReadAllBytes(dmePath, cancellationToken);

			var dmeModificationsTask = configuration.CopyDMFilesTo(
				dmeFileName,
				ioManager.ResolvePath(
					ioManager.ConcatPath(
						stringDirectoryName,
						ioManager.GetDirectoryName(dmeFileName))),
				cancellationToken);

			var dmeBytes = await dmeReadTask;
			var dme = Encoding.UTF8.GetString(dmeBytes);

			var dmeModifications = await dmeModificationsTask;

			if (dmeModifications == null || dmeModifications.TotalDmeOverwrite)
			{
				if (dmeModifications != null)
					logger.LogDebug(".dme replacement configured!");
				else
					logger.LogTrace("No .dme modifications required.");
				return;
			}

			var dmeLines = new List<string>(dme.Split('\n', StringSplitOptions.None));
			for (var dmeLineIndex = 0; dmeLineIndex < dmeLines.Count; ++dmeLineIndex)
			{
				var line = dmeLines[dmeLineIndex];
				if (line.Contains("BEGIN_INCLUDE", StringComparison.Ordinal) && dmeModifications.HeadIncludeLine != null)
				{
					var headIncludeLineNumber = dmeLineIndex + 1;
					logger.LogDebug(
						"Inserting HeadInclude.dm at line {lineNumber}: {includeLine}",
						headIncludeLineNumber,
						dmeModifications.HeadIncludeLine);
					dmeLines.Insert(headIncludeLineNumber, dmeModifications.HeadIncludeLine);
					++dmeLineIndex;
				}
				else if (line.Contains("END_INCLUDE", StringComparison.Ordinal) && dmeModifications.TailIncludeLine != null)
				{
					logger.LogDebug(
						"Inserting TailInclude.dm at line {lineNumber}: {includeLine}",
						dmeLineIndex,
						dmeModifications.TailIncludeLine);
					dmeLines.Insert(dmeLineIndex, dmeModifications.TailIncludeLine);
					break;
				}
			}

			dmeBytes = Encoding.UTF8.GetBytes(String.Join('\n', dmeLines));
			await ioManager.WriteAllBytes(dmePath, dmeBytes, cancellationToken);
		}

		/// <summary>
		/// Cleans up a failed compile <paramref name="job"/>.
		/// </summary>
		/// <param name="job">The running <see cref="CompileJob"/>.</param>
		/// <param name="remoteDeploymentManager">The <see cref="IRemoteDeploymentManager"/> associated with the <paramref name="job"/>.</param>
		/// <param name="exception">The <see cref="Exception"/> that was thrown.</param>
		/// <returns>A <see cref="ValueTask"/> representing the running operation.</returns>
		ValueTask CleanupFailedCompile(Models.CompileJob job, IRemoteDeploymentManager remoteDeploymentManager, Exception exception)
		{
			async ValueTask CleanDir()
			{
				if (sessionConfiguration.DelayCleaningFailedDeployments)
				{
					logger.LogDebug("Not cleaning up errored deployment directory {guid} due to config.", job.DirectoryName);
					return;
				}

				logger.LogTrace("Cleaning compile directory...");
				var jobPath = job.DirectoryName!.Value.ToString();
				try
				{
					// DCT: None available
					await eventConsumer.HandleEvent(EventType.DeploymentCleanup, new List<string> { jobPath }, true, CancellationToken.None);
					await ioManager.DeleteDirectory(jobPath, CancellationToken.None);
				}
				catch (Exception e)
				{
					logger.LogWarning(e, "Error cleaning up compile directory {path}!", ioManager.ResolvePath(jobPath));
				}
			}

			var dirCleanTask = CleanDir();

			var failRemoteDeployTask = remoteDeploymentManager.FailDeployment(
				job,
				FormatExceptionForUsers(exception),
				CancellationToken.None); // DCT: None available

			return ValueTaskExtensions.WhenAll(
				dirCleanTask,
				failRemoteDeployTask);
		}
	}
}
