using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Octokit;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Api.Models;
using Tgstation.Server.Api.Models.Internal;
using Tgstation.Server.Host.Components.Byond;
using Tgstation.Server.Host.Components.Chat;
using Tgstation.Server.Host.Components.Events;
using Tgstation.Server.Host.Components.Repository;
using Tgstation.Server.Host.Components.Session;
using Tgstation.Server.Host.Core;
using Tgstation.Server.Host.Database;
using Tgstation.Server.Host.Extensions;
using Tgstation.Server.Host.IO;
using Tgstation.Server.Host.Jobs;
using Tgstation.Server.Host.Models;
using Tgstation.Server.Host.System;

namespace Tgstation.Server.Host.Components.Deployment
{
	/// <inheritdoc />
	sealed class DreamMaker : IDreamMaker
	{
		/// <summary>
		/// Extension for .dmbs
		/// </summary>
		public const string DmbExtension = ".dmb";

		/// <summary>
		/// Extension for .dmes
		/// </summary>
		const string DmeExtension = "dme";

		/// <summary>
		/// The <see cref="IByondManager"/> for <see cref="DreamMaker"/>
		/// </summary>
		readonly IByondManager byond;

		/// <summary>
		/// The <see cref="IIOManager"/> for <see cref="DreamMaker"/>
		/// </summary>
		readonly IIOManager ioManager;

		/// <summary>
		/// The <see cref="StaticFiles.IConfiguration"/> for <see cref="DreamMaker"/>
		/// </summary>
		readonly StaticFiles.IConfiguration configuration;

		/// <summary>
		/// The <see cref="ISessionControllerFactory"/> for <see cref="DreamMaker"/>
		/// </summary>
		readonly ISessionControllerFactory sessionControllerFactory;

		/// <summary>
		/// The <see cref="IEventConsumer"/> for <see cref="DreamMaker"/>
		/// </summary>
		readonly IEventConsumer eventConsumer;

		/// <summary>
		/// The <see cref="IChatManager"/> for <see cref="DreamMaker"/>
		/// </summary>
		readonly IChatManager chatManager;

		/// <summary>
		/// The <see cref="IProcessExecutor"/> for <see cref="DreamMaker"/>
		/// </summary>
		readonly IProcessExecutor processExecutor;

		/// <summary>
		/// The <see cref="IRepositoryManager"/> for <see cref="DreamMaker"/>.
		/// </summary>
		readonly IRepositoryManager repositoryManager;

		/// <summary>
		/// The <see cref="IGitHubClientFactory"/> for <see cref="DreamMaker"/>.
		/// </summary>
		readonly IGitHubClientFactory gitHubClientFactory;

		/// <summary>
		/// The <see cref="ICompileJobSink"/> for <see cref="DreamMaker"/>.
		/// </summary>
		readonly ICompileJobSink compileJobConsumer;

		/// <summary>
		/// The <see cref="ILogger"/> for <see cref="DreamMaker"/>
		/// </summary>
		readonly ILogger<DreamMaker> logger;

		/// <summary>
		/// The <see cref="Instance"/> <see cref="DreamMaker"/> belongs to.
		/// </summary>
		readonly Api.Models.Instance metadata;

		/// <summary>
		/// <see langword="lock"/> <see cref="object"/> for <see cref="deploying"/>.
		/// </summary>
		readonly object deploymentLock;

		Func<string, string, Task> currentChatCallback;

		string currentDreamMakerOutput;

		/// <summary>
		/// If a compile job is running
		/// </summary>
		bool deploying;

		/// <summary>
		/// Construct <see cref="DreamMaker"/>
		/// </summary>
		/// <param name="byond">The value of <see cref="byond"/></param>
		/// <param name="ioManager">The value of <see cref="ioManager"/></param>
		/// <param name="configuration">The value of <see cref="configuration"/></param>
		/// <param name="sessionControllerFactory">The value of <see cref="sessionControllerFactory"/></param>
		/// <param name="eventConsumer">The value of <see cref="eventConsumer"/></param>
		/// <param name="chatManager">The value of <see cref="chatManager"/></param>
		/// <param name="processExecutor">The value of <see cref="processExecutor"/></param>
		/// <param name="gitHubClientFactory">The value of <see cref="gitHubClientFactory"/>.</param>
		/// <param name="compileJobConsumer">The value of <see cref="compileJobConsumer"/>.</param>
		/// <param name="repositoryManager">The value of <see cref="repositoryManager"/>.</param>
		/// <param name="logger">The value of <see cref="logger"/></param>
		/// <param name="metadata">The value of <see cref="metadata"/>.</param>
		public DreamMaker(
			IByondManager byond,
			IIOManager ioManager,
			StaticFiles.IConfiguration configuration,
			ISessionControllerFactory sessionControllerFactory,
			IEventConsumer eventConsumer,
			IChatManager chatManager,
			IProcessExecutor processExecutor,
			IGitHubClientFactory gitHubClientFactory,
			ICompileJobSink compileJobConsumer,
			IRepositoryManager repositoryManager,
			ILogger<DreamMaker> logger,
			Api.Models.Instance metadata)
		{
			this.byond = byond ?? throw new ArgumentNullException(nameof(byond));
			this.ioManager = ioManager ?? throw new ArgumentNullException(nameof(ioManager));
			this.configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
			this.sessionControllerFactory = sessionControllerFactory ?? throw new ArgumentNullException(nameof(sessionControllerFactory));
			this.eventConsumer = eventConsumer ?? throw new ArgumentNullException(nameof(eventConsumer));
			this.chatManager = chatManager ?? throw new ArgumentNullException(nameof(chatManager));
			this.processExecutor = processExecutor ?? throw new ArgumentNullException(nameof(processExecutor));
			this.gitHubClientFactory = gitHubClientFactory ?? throw new ArgumentNullException(nameof(gitHubClientFactory));
			this.compileJobConsumer = compileJobConsumer ?? throw new ArgumentNullException(nameof(compileJobConsumer));
			this.repositoryManager = repositoryManager ?? throw new ArgumentNullException(nameof(repositoryManager));
			this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
			this.metadata = metadata ?? throw new ArgumentNullException(nameof(metadata));

			deploymentLock = new object();
		}

		/// <summary>
		/// Gradually triggers a given <paramref name="progressReporter"/> over a given <paramref name="estimatedDuration"/>
		/// </summary>
		/// <param name="progressReporter">The <see cref="Action{T1}"/> to report progress</param>
		/// <param name="estimatedDuration">A <see cref="TimeSpan"/> representing the duration to give progress over</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		async Task ProgressTask(Action<int> progressReporter, TimeSpan estimatedDuration, CancellationToken cancellationToken)
		{
			progressReporter(0);
			var sleepInterval = estimatedDuration / 100;

			logger.LogDebug("Compile is expected to take: {0}", estimatedDuration);
			try
			{
				for (var I = 0; I < 99; ++I)
				{
					await Task.Delay(sleepInterval, cancellationToken).ConfigureAwait(false);
					progressReporter(I + 1);
				}
			}
			catch (OperationCanceledException) { }
		}

		/// <summary>
		/// Run a quick DD instance to test the DMAPI is installed on the target code
		/// </summary>
		/// <param name="timeout">The timeout in seconds for validation</param>
		/// <param name="securityLevel">The <see cref="DreamDaemonSecurity"/> level to use to validate the API</param>
		/// <param name="job">The <see cref="CompileJob"/> for the operation</param>
		/// <param name="byondLock">The current <see cref="IByondExecutableLock"/></param>
		/// <param name="portToUse">The port to use for API validation</param>
		/// <param name="requireValidate">If the API validation is required to complete the deployment.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		async Task VerifyApi(
			uint timeout,
			DreamDaemonSecurity securityLevel,
			Models.CompileJob job,
			IByondExecutableLock byondLock,
			ushort portToUse,
			bool requireValidate,
			CancellationToken cancellationToken)
		{
			logger.LogTrace("Verifying {0}DMAPI...", requireValidate ? "required " : String.Empty);
			var launchParameters = new DreamDaemonLaunchParameters
			{
				AllowWebClient = false,
				Port = portToUse,
				SecurityLevel = securityLevel,
				StartupTimeout = timeout,
				TopicRequestTimeout = 0, // not used
				HeartbeatSeconds = 0 // not used
			};

			job.MinimumSecurityLevel = securityLevel; // needed for the TempDmbProvider
			var timeoutAt = DateTimeOffset.Now.AddSeconds(timeout);

			using var provider = new TemporaryDmbProvider(ioManager.ResolvePath(job.DirectoryName.ToString()), String.Concat(job.DmeName, DmbExtension), job);
			using var controller = await sessionControllerFactory.LaunchNew(provider, byondLock, launchParameters, true, cancellationToken).ConfigureAwait(false);
			var launchResult = await controller.LaunchResult.ConfigureAwait(false);

			var now = DateTimeOffset.Now;
			if (now < timeoutAt && launchResult.StartupTime.HasValue)
			{
				var timeoutTask = Task.Delay(timeoutAt - now, cancellationToken);

				await Task.WhenAny(controller.Lifetime, timeoutTask).ConfigureAwait(false);
				cancellationToken.ThrowIfCancellationRequested();
			}

			if (!controller.Lifetime.IsCompleted)
			{
				if (requireValidate)
					throw new JobException(ErrorCode.DreamMakerNeverValidated);
				controller.Dispose();
			}

			var validationStatus = controller.ApiValidationStatus;
			logger.LogTrace("API validation status: {0}", validationStatus);

			job.DMApiVersion = controller.DMApiVersion;
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
						throw new JobException(ErrorCode.DreamMakerNeverValidated);
					job.MinimumSecurityLevel = DreamDaemonSecurity.Ultrasafe;
					break;
				case ApiValidationStatus.BadValidationRequest:
				case ApiValidationStatus.Incompatible:
					throw new JobException(ErrorCode.DreamMakerInvalidValidation);
				case ApiValidationStatus.UnaskedValidationRequest:
				default:
					throw new InvalidOperationException(
						$"Session controller returned unexpected ApiValidationStatus: {validationStatus}");
			}
		}

		/// <summary>
		/// Compiles a .dme with DreamMaker
		/// </summary>
		/// <param name="dreamMakerPath">The path to the DreamMaker executable</param>
		/// <param name="job">The <see cref="CompileJob"/> for the operation</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		async Task<int> RunDreamMaker(string dreamMakerPath, Models.CompileJob job, CancellationToken cancellationToken)
		{
			using var dm = processExecutor.LaunchProcess(
				dreamMakerPath,
				ioManager.ResolvePath(
					job.DirectoryName.ToString()),
				$"-clean {job.DmeName}.{DmeExtension}",
				true,
				true,
				true);
			int exitCode;
			using (cancellationToken.Register(() => dm.Terminate()))
				exitCode = await dm.Lifetime.ConfigureAwait(false);
			cancellationToken.ThrowIfCancellationRequested();

			logger.LogDebug("DreamMaker exit code: {0}", exitCode);
			currentDreamMakerOutput = job.Output = dm.GetCombinedOutput();
			logger.LogDebug("DreamMaker output: {0}{1}", Environment.NewLine, job.Output);
			return exitCode;
		}

		/// <summary>
		/// Adds server side includes to the .dme being compiled
		/// </summary>
		/// <param name="job">The <see cref="CompileJob"/> for the operation</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		async Task ModifyDme(Models.CompileJob job, CancellationToken cancellationToken)
		{
			var dmeFileName = String.Join('.', job.DmeName, DmeExtension);
			var dmePath = ioManager.ConcatPath(job.DirectoryName.ToString(), dmeFileName);
			var dmeReadTask = ioManager.ReadAllBytes(dmePath, cancellationToken);

			var dmeModificationsTask = configuration.CopyDMFilesTo(dmeFileName, ioManager.ResolvePath(job.DirectoryName.ToString()), cancellationToken);

			var dmeBytes = await dmeReadTask.ConfigureAwait(false);
			var dme = Encoding.UTF8.GetString(dmeBytes);

			var dmeModifications = await dmeModificationsTask.ConfigureAwait(false);

			if (dmeModifications == null || dmeModifications.TotalDmeOverwrite)
			{
				if (dmeModifications != null)
					logger.LogDebug(".dme replacement configured!");
				else
					logger.LogTrace("No .dme modifications required.");
				return;
			}

			if (dmeModifications.HeadIncludeLine != null)
				logger.LogDebug("Head .dme include line: {0}", dmeModifications.HeadIncludeLine);
			if (dmeModifications.TailIncludeLine != null)
				logger.LogDebug("Tail .dme include line: {0}", dmeModifications.TailIncludeLine);

			var dmeLines = new List<string>(dme.Split(new[] { Environment.NewLine }, StringSplitOptions.None));
			for (var I = 0; I < dmeLines.Count; ++I)
			{
				var line = dmeLines[I];
				if (line.Contains("BEGIN_INCLUDE", StringComparison.Ordinal) && dmeModifications.HeadIncludeLine != null)
				{
					dmeLines.Insert(I + 1, dmeModifications.HeadIncludeLine);
					++I;
				}
				else if (line.Contains("END_INCLUDE", StringComparison.Ordinal) && dmeModifications.TailIncludeLine != null)
				{
					dmeLines.Insert(I, dmeModifications.TailIncludeLine);
					break;
				}
			}

			dmeBytes = Encoding.UTF8.GetBytes(String.Join(Environment.NewLine, dmeLines));
			await ioManager.WriteAllBytes(dmePath, dmeBytes, cancellationToken).ConfigureAwait(false);
		}

		/// <summary>
		/// Cleans up a failed compile <paramref name="job"/>
		/// </summary>
		/// <param name="job">The running <see cref="CompileJob"/></param>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		async Task CleanupFailedCompile(Models.CompileJob job)
		{
			logger.LogTrace("Cleaning compile directory...");
			var jobPath = job.DirectoryName.ToString();
			try
			{
				await ioManager.DeleteDirectory(jobPath, CancellationToken.None).ConfigureAwait(false);
			}
			catch (Exception e)
			{
				logger.LogWarning("Error cleaning up compile directory {0}! Exception: {1}", ioManager.ResolvePath(jobPath), e);
			}
		}

		/// <summary>
		/// Executes and populate a given <paramref name="job"/>
		/// </summary>
		/// <param name="job">The <see cref="CompileJob"/> to run and populate</param>
		/// <param name="dreamMakerSettings">The <see cref="Api.Models.DreamMaker"/> settings to use</param>
		/// <param name="byondLock">The <see cref="IByondExecutableLock"/> to use</param>
		/// <param name="repository">The <see cref="IRepository"/> to use</param>
		/// <param name="apiValidateTimeout">The timeout for validating the DMAPI</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		async Task RunCompileJob(Models.CompileJob job, Api.Models.DreamMaker dreamMakerSettings, IByondExecutableLock byondLock, IRepository repository, uint apiValidateTimeout, CancellationToken cancellationToken)
		{
			var outputDirectory = job.DirectoryName.ToString();
			logger.LogTrace("Compile output GUID: {0}", outputDirectory);

			try
			{
				// copy the repository
				logger.LogTrace("Copying repository to game directory...");
				var resolvedOutputDirectory = ioManager.ResolvePath(outputDirectory);
				var repoOrigin = repository.Origin;
				using (repository)
					await repository.CopyTo(resolvedOutputDirectory, cancellationToken).ConfigureAwait(false);

				// repository closed now

				// run precompile scripts
				await eventConsumer.HandleEvent(EventType.CompileStart, new List<string> { resolvedOutputDirectory, repoOrigin }, cancellationToken).ConfigureAwait(false);

				// determine the dme
				if (job.DmeName == null)
				{
					logger.LogTrace("Searching for available .dmes...");
					var foundPaths = await ioManager.GetFilesWithExtension(resolvedOutputDirectory, DmeExtension, true, cancellationToken).ConfigureAwait(false);
					var foundPath = foundPaths.FirstOrDefault();
					if (foundPath == default)
						throw new JobException(ErrorCode.DreamMakerNoDme);
					job.DmeName = foundPath.Substring(
						resolvedOutputDirectory.Length + 1,
						foundPath.Length - resolvedOutputDirectory.Length - DmeExtension.Length - 2); // +1 for . in extension
				}
				else
				{
					var targetDme = ioManager.ConcatPath(outputDirectory, String.Join('.', job.DmeName, DmeExtension));
					var targetDmeExists = await ioManager.FileExists(targetDme, cancellationToken).ConfigureAwait(false);
					if (!targetDmeExists)
						throw new JobException(ErrorCode.DreamMakerMissingDme);
				}

				logger.LogDebug("Selected {0}.dme for compilation!", job.DmeName);

				await ModifyDme(job, cancellationToken).ConfigureAwait(false);

				// run compiler
				var exitCode = await RunDreamMaker(byondLock.DreamMakerPath, job, cancellationToken).ConfigureAwait(false);

				// verify api
				try
				{
					if (exitCode != 0)
						throw new JobException(
							ErrorCode.DreamMakerExitCode,
							new JobException($"Exit code: {exitCode}{Environment.NewLine}{Environment.NewLine}{job.Output}"));

					await VerifyApi(
						apiValidateTimeout,
						dreamMakerSettings.ApiValidationSecurityLevel.Value,
						job,
						byondLock,
						dreamMakerSettings.ApiValidationPort.Value,
						dreamMakerSettings.RequireDMApiValidation.Value,
						cancellationToken)
						.ConfigureAwait(false);
				}
				catch (JobException)
				{
					// DD never validated or compile failed
					await eventConsumer.HandleEvent(EventType.CompileFailure, new List<string> { resolvedOutputDirectory, exitCode == 0 ? "1" : "0" }, cancellationToken).ConfigureAwait(false);
					throw;
				}

				logger.LogTrace("Running post compile event...");
				await eventConsumer.HandleEvent(EventType.CompileComplete, new List<string> { resolvedOutputDirectory }, cancellationToken).ConfigureAwait(false);

				logger.LogTrace("Applying static game file symlinks...");

				// symlink in the static data
				await configuration.SymlinkStaticFilesTo(resolvedOutputDirectory, cancellationToken).ConfigureAwait(false);

				logger.LogDebug("Compile complete!");
			}
			catch
			{
				await CleanupFailedCompile(job).ConfigureAwait(false);
				throw;
			}
		}

		/// <inheritdoc />
		#pragma warning disable CA1506
		public async Task DeploymentProcess(
			Models.Job job,
			IDatabaseContextFactory databaseContextFactory,
			Action<int> progressReporter,
			CancellationToken cancellationToken)
		{
#pragma warning disable IDE0016 // Use 'throw' expression
			if (job == null)
				throw new ArgumentNullException(nameof(job));
#pragma warning restore IDE0016 // Use 'throw' expression
			if (databaseContextFactory == null)
				throw new ArgumentNullException(nameof(databaseContextFactory));
			if (progressReporter == null)
				throw new ArgumentNullException(nameof(progressReporter));

			lock (deploymentLock)
			{
				if (deploying)
					throw new JobException(ErrorCode.DreamMakerCompileJobInProgress);
				deploying = true;
			}

			currentChatCallback = null;
			currentDreamMakerOutput = null;
			Models.CompileJob compileJob = null;
			try
			{
				string repoOwner = null;
				string repoName = null;
				TimeSpan? averageSpan = null;
				Models.RepositorySettings repositorySettings = null;
				Models.DreamDaemonSettings ddSettings = null;
				DreamMakerSettings dreamMakerSettings = null;
				IRepository repo = null;
				Models.RevisionInformation revInfo = null;
				await databaseContextFactory.UseContext(
					async databaseContext =>
					{
						averageSpan = await CalculateExpectedDeploymentTime(databaseContext, cancellationToken).ConfigureAwait(false);

						ddSettings = await databaseContext
							.DreamDaemonSettings
							.AsQueryable()
							.Where(x => x.InstanceId == metadata.Id)
							.Select(x => new Models.DreamDaemonSettings
							{
								StartupTimeout = x.StartupTimeout,
							})
							.FirstOrDefaultAsync(cancellationToken)
							.ConfigureAwait(false);
						if (ddSettings == default)
							throw new JobException(ErrorCode.InstanceMissingDreamDaemonSettings);

						dreamMakerSettings = await databaseContext
							.DreamMakerSettings
							.AsQueryable()
							.Where(x => x.InstanceId == metadata.Id)
							.FirstAsync(cancellationToken)
							.ConfigureAwait(false);
						if (dreamMakerSettings == default)
							throw new JobException(ErrorCode.InstanceMissingDreamMakerSettings);

						repositorySettings = await databaseContext
							.RepositorySettings
							.AsQueryable()
							.Where(x => x.InstanceId == metadata.Id)
							.Select(x => new Models.RepositorySettings
							{
								AccessToken = x.AccessToken,
								ShowTestMergeCommitters = x.ShowTestMergeCommitters,
								PushTestMergeCommits = x.PushTestMergeCommits,
								PostTestMergeComment = x.PostTestMergeComment
							})
							.FirstOrDefaultAsync(cancellationToken)
							.ConfigureAwait(false);
						if (repositorySettings == default)
							throw new JobException(ErrorCode.InstanceMissingRepositorySettings);

						repo = await repositoryManager.LoadRepository(cancellationToken).ConfigureAwait(false);
						try
						{
							if (repo == null)
								throw new JobException(ErrorCode.RepoMissing);

							if (repo.IsGitHubRepository)
							{
								repoOwner = repo.GitHubOwner;
								repoName = repo.GitHubRepoName;
							}

							var repoSha = repo.Head;
							revInfo = await databaseContext
								.RevisionInformations
								.AsQueryable()
								.Where(x => x.CommitSha == repoSha && x.Instance.Id == metadata.Id)
								.Include(x => x.ActiveTestMerges)
								.ThenInclude(x => x.TestMerge)
								.ThenInclude(x => x.MergedBy)
								.FirstOrDefaultAsync(cancellationToken)
								.ConfigureAwait(false);

							if (revInfo == default)
							{
								revInfo = new Models.RevisionInformation
								{
									CommitSha = repoSha,
									OriginCommitSha = repoSha,
									Instance = new Models.Instance
									{
										Id = metadata.Id
									},
									ActiveTestMerges = new List<RevInfoTestMerge>()
								};

								logger.LogInformation(Repository.Repository.OriginTrackingErrorTemplate, repoSha);
								databaseContext.Instances.Attach(revInfo.Instance);
								await databaseContext.Save(cancellationToken).ConfigureAwait(false);
							}
						}
						catch
						{
							repo?.Dispose();
							throw;
						}
					})
					.ConfigureAwait(false);

				var likelyPushedTestMergeCommit =
					repositorySettings.PushTestMergeCommits.Value
					&& repositorySettings.AccessToken != null
					&& repositorySettings.AccessUser != null;
				using (repo)
					compileJob = await Compile(
						revInfo,
						dreamMakerSettings,
						ddSettings.StartupTimeout.Value,
						repo,
						progressReporter,
						averageSpan,
						likelyPushedTestMergeCommit,
						cancellationToken)
						.ConfigureAwait(false);

				var activeCompileJob = compileJobConsumer.LatestCompileJob();
				try
				{
					await databaseContextFactory.UseContext(
						async databaseContext =>
						{
							compileJob.Job = new Models.Job
							{
								Id = job.Id
							};
							compileJob.RevisionInformation = new Models.RevisionInformation
							{
								Id = revInfo.Id
							};

							databaseContext.Jobs.Attach(compileJob.Job);
							databaseContext.RevisionInformations.Attach(compileJob.RevisionInformation);
							databaseContext.CompileJobs.Add(compileJob);

							// The difficulty with compile jobs is they have a two part commit
							await databaseContext.Save(cancellationToken).ConfigureAwait(false);
							try
							{
								await compileJobConsumer.LoadCompileJob(compileJob, cancellationToken).ConfigureAwait(false);
							}
							catch
							{
								// So we need to un-commit the compile job if the above throws
								databaseContext.CompileJobs.Remove(compileJob);
								await databaseContext.Save(default).ConfigureAwait(false);
								throw;
							}
						})
						.ConfigureAwait(false);
				}
				catch
				{
					await CleanupFailedCompile(compileJob).ConfigureAwait(false);
					throw;
				}

				var commentsTask = PostDeploymentComments(
					revInfo,
					activeCompileJob?.RevisionInformation,
					repositorySettings,
					repoOwner,
					repoName,
					cancellationToken);

				var eventTask = eventConsumer.HandleEvent(EventType.DeploymentComplete, null, cancellationToken);

				var chatTask = currentChatCallback(null, compileJob.Output);
				currentChatCallback = null;

				try
				{
					await Task.WhenAll(commentsTask, eventTask, chatTask).ConfigureAwait(false);
				}
				catch (Exception ex)
				{
					throw new JobException(ErrorCode.PostDeployFailure, ex);
				}
			}
			catch (Exception ex)
			{
				if (currentChatCallback != null)
					await currentChatCallback(
						ex is OperationCanceledException
							? "The job was cancelled!"
							: ex.Message,
						currentDreamMakerOutput)
						.ConfigureAwait(false);

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
		/// <returns>A <see cref="Task{TResult}"/> resulting in the average <see cref="TimeSpan"/> of the 10 previous deployments or <see langword="null"/> if there are none.</returns>
		async Task<TimeSpan?> CalculateExpectedDeploymentTime(IDatabaseContext databaseContext, CancellationToken cancellationToken)
		{
			var previousCompileJobs = await databaseContext
				.CompileJobs
				.AsQueryable()
				.Where(x => x.Job.Instance.Id == metadata.Id)
				.OrderByDescending(x => x.Job.StoppedAt)
				.Take(10)
				.Select(x => new Models.Job
				{
					StoppedAt = x.Job.StoppedAt,
					StartedAt = x.Job.StartedAt
				})
				.ToListAsync(cancellationToken)
				.ConfigureAwait(false);

			TimeSpan? averageSpan = null;
			if (previousCompileJobs.Count != 0)
			{
				var totalSpan = TimeSpan.Zero;
				foreach (var I in previousCompileJobs)
					totalSpan += I.StoppedAt.Value - I.StartedAt.Value;
				averageSpan = totalSpan / previousCompileJobs.Count;
			}

			return averageSpan;
		}

		async Task<Models.CompileJob> Compile(
			Models.RevisionInformation revisionInformation,
			Api.Models.DreamMaker dreamMakerSettings,
			uint apiValidateTimeout,
			IRepository repository,
			Action<int> progressReporter,
			TimeSpan? estimatedDuration,
			bool localCommitExistsOnRemote,
			CancellationToken cancellationToken)
		{
			logger.LogTrace("Begin Compile");

			using var progressCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
			var progressTask = estimatedDuration.HasValue ? ProgressTask(progressReporter, estimatedDuration.Value, progressCts.Token) : Task.CompletedTask;
			try
			{
				using var byondLock = await byond.UseExecutables(null, cancellationToken).ConfigureAwait(false);
				currentChatCallback = await chatManager.SendDeploymentMessage(
					revisionInformation,
					byondLock.Version,
					DateTimeOffset.Now + estimatedDuration,
					repository.GitHubOwner,
					repository.GitHubRepoName,
					localCommitExistsOnRemote,
					cancellationToken)
					.ConfigureAwait(false);

				var job = new Models.CompileJob
				{
					DirectoryName = Guid.NewGuid(),
					DmeName = dreamMakerSettings.ProjectName,
					RevisionInformation = revisionInformation,
					ByondVersion = byondLock.Version.ToString()
				};

				await RunCompileJob(job, dreamMakerSettings, byondLock, repository, apiValidateTimeout, cancellationToken).ConfigureAwait(false);

				return job;
			}
			catch (OperationCanceledException)
			{
				await eventConsumer.HandleEvent(EventType.CompileCancelled, null, default).ConfigureAwait(false);
				throw;
			}
			finally
			{
				progressCts.Cancel();
				await progressTask.ConfigureAwait(false);
			}
		}

		/// <summary>
		/// Post deployment GitHub comments.
		/// </summary>
		/// <param name="deployedRevisionInformation">The deployed <see cref="RevisionInformation"/>.</param>
		/// <param name="previousRevisionInformation">The <see cref="RevisionInformation"/> of the previous deployment.</param>
		/// <param name="repositorySettings">The <see cref="RepositorySettings"/>.</param>
		/// <param name="repoOwner">The GitHub repostiory owner.</param>
		/// <param name="repoName">The GitHub repostiory name.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task"/> representing the running operation.</returns>
		async Task PostDeploymentComments(
			Models.RevisionInformation deployedRevisionInformation,
			Models.RevisionInformation previousRevisionInformation,
			Models.RepositorySettings repositorySettings,
			string repoOwner,
			string repoName,
			CancellationToken cancellationToken)
		{
			if (repositorySettings?.AccessToken == null)
				return;

			if ((previousRevisionInformation != null && previousRevisionInformation.CommitSha == previousRevisionInformation.CommitSha)
				|| !repositorySettings.PostTestMergeComment.Value)
				return;

			previousRevisionInformation = new Models.RevisionInformation
			{
				ActiveTestMerges = new List<RevInfoTestMerge>()
			};

			var gitHubClient = gitHubClientFactory.CreateClient(repositorySettings.AccessToken);

			async Task CommentOnPR(int prNumber, string comment)
			{
				try
				{
					await gitHubClient.Issue.Comment.Create(repoOwner, repoName, prNumber, comment)
						.WithToken(cancellationToken)
						.ConfigureAwait(false);
				}
				catch (ApiException e)
				{
					logger.LogWarning("Error posting GitHub comment! Exception: {0}", e);
				}
			}

			var tasks = new List<Task>();

			string FormatTestMerge(Models.TestMerge testMerge, bool updated) => String.Format(CultureInfo.InvariantCulture, "#### Test Merge {4}{0}{0}##### Server Instance{0}{5}{1}{0}{0}##### Revision{0}Origin: {6}{0}Pull Request: {2}{0}Server: {7}{3}",
				Environment.NewLine,
				repositorySettings.ShowTestMergeCommitters.Value ? String.Format(CultureInfo.InvariantCulture, "{0}{0}##### Merged By{0}{1}", Environment.NewLine, testMerge.MergedBy.Name) : String.Empty,
				testMerge.PullRequestRevision,
				testMerge.Comment != null ? String.Format(CultureInfo.InvariantCulture, "{0}{0}##### Comment{0}{1}", Environment.NewLine, testMerge.Comment) : String.Empty,
				updated ? "Updated" : "Deployed",
				metadata.Name,
				deployedRevisionInformation.OriginCommitSha,
				deployedRevisionInformation.CommitSha);

			// added prs
			foreach (var I in deployedRevisionInformation
				.ActiveTestMerges
				.Select(x => x.TestMerge)
				.Where(x => !previousRevisionInformation
					.ActiveTestMerges
					.Any(y => y.TestMerge.Number == x.Number)))
				tasks.Add(CommentOnPR(I.Number, FormatTestMerge(I, false)));

			// removed prs
			foreach (var I in previousRevisionInformation
			.ActiveTestMerges
			.Select(x => x.TestMerge)
				.Where(x => !deployedRevisionInformation
				.ActiveTestMerges
				.Any(y => y.TestMerge.Number == x.Number)))
				tasks.Add(CommentOnPR(I.Number, "#### Test Merge Removed"));

			// updated prs
			foreach (var I in deployedRevisionInformation
				.ActiveTestMerges
				.Select(x => x.TestMerge)
				.Where(x => previousRevisionInformation
					.ActiveTestMerges
					.Any(y => y.TestMerge.Number == x.Number)))
				tasks.Add(CommentOnPR(I.Number, FormatTestMerge(I, true)));

			if (tasks.Any())
				await Task.WhenAll(tasks).ConfigureAwait(false);
		}
	}
}
