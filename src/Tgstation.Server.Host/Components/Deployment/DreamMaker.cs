using Microsoft.Extensions.Logging;
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
using Tgstation.Server.Host.Components.Repository;
using Tgstation.Server.Host.Components.Watchdog;
using Tgstation.Server.Host.IO;
using Tgstation.Server.Host.Jobs;
using Tgstation.Server.Host.System;

namespace Tgstation.Server.Host.Components.Deployment
{
	/// <inheritdoc />
	sealed class DreamMaker : IDreamMaker
	{
		/// <summary>
		/// Name of the primary directory used for compilation
		/// </summary>
		public const string ADirectoryName = "A";

		/// <summary>
		/// Name of the secondary directory used for compilation
		/// </summary>
		public const string BDirectoryName = "B";

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
		/// The <see cref="IWatchdog"/> for <see cref="DreamMaker"/>
		/// </summary>
		readonly IWatchdog watchdog;

		/// <summary>
		/// The <see cref="ILogger"/> for <see cref="DreamMaker"/>
		/// </summary>
		readonly ILogger<DreamMaker> logger;

		/// <summary>
		/// If a compile job is running
		/// </summary>
		bool compiling;

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
		/// <param name="watchdog">The value of <see cref="watchdog"/></param>
		/// <param name="logger">The value of <see cref="logger"/></param>
		public DreamMaker(
			IByondManager byond,
			IIOManager ioManager,
			StaticFiles.IConfiguration configuration,
			ISessionControllerFactory sessionControllerFactory,
			IEventConsumer eventConsumer,
			IChatManager chatManager,
			IProcessExecutor processExecutor,
			IWatchdog watchdog,
			ILogger<DreamMaker> logger)
		{
			this.byond = byond ?? throw new ArgumentNullException(nameof(byond));
			this.ioManager = ioManager ?? throw new ArgumentNullException(nameof(ioManager));
			this.configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
			this.sessionControllerFactory = sessionControllerFactory ?? throw new ArgumentNullException(nameof(sessionControllerFactory));
			this.eventConsumer = eventConsumer ?? throw new ArgumentNullException(nameof(eventConsumer));
			this.chatManager = chatManager ?? throw new ArgumentNullException(nameof(chatManager));
			this.processExecutor = processExecutor ?? throw new ArgumentNullException(nameof(processExecutor));
			this.watchdog = watchdog ?? throw new ArgumentNullException(nameof(watchdog));
			this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
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
		/// <param name="job">The <see cref="Models.CompileJob"/> for the operation</param>
		/// <param name="byondLock">The current <see cref="IByondExecutableLock"/></param>
		/// <param name="portToUse">The port to use for API validation</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		async Task VerifyApi(uint timeout, DreamDaemonSecurity securityLevel, Models.CompileJob job, IByondExecutableLock byondLock, ushort portToUse, CancellationToken cancellationToken)
		{
			logger.LogTrace("Verifying DMAPI...");
			var launchParameters = new DreamDaemonLaunchParameters
			{
				AllowWebClient = false,
				PrimaryPort = portToUse,
				SecurityLevel = securityLevel,
				StartupTimeout = timeout
			};

			var dirA = ioManager.ConcatPath(job.DirectoryName.ToString(), ADirectoryName);

			job.MinimumSecurityLevel = securityLevel; // needed for the TempDmbProvider
			var timeoutAt = DateTimeOffset.Now.AddSeconds(timeout);

			using (var provider = new TemporaryDmbProvider(ioManager.ResolvePath(dirA), String.Concat(job.DmeName, DmbExtension), job))
			using (var controller = await sessionControllerFactory.LaunchNew(provider, byondLock, launchParameters, true, true, true, cancellationToken).ConfigureAwait(false))
			{
				var launchResult = await controller.LaunchResult.ConfigureAwait(false);

				var now = DateTimeOffset.Now;
				if (now < timeoutAt && launchResult.StartupTime.HasValue)
				{
					var timeoutTask = Task.Delay(timeoutAt - now, cancellationToken);

					await Task.WhenAny(controller.Lifetime, timeoutTask).ConfigureAwait(false);
					cancellationToken.ThrowIfCancellationRequested();
				}

				if (controller.Lifetime.IsCompleted)
				{
					var validationStatus = controller.ApiValidationStatus;
					logger.LogTrace("API validation status: {0}", validationStatus);
					switch (validationStatus)
					{
						case ApiValidationStatus.RequiresUltrasafe:
							job.MinimumSecurityLevel = DreamDaemonSecurity.Ultrasafe;
							return;
						case ApiValidationStatus.RequiresSafe:
							if (securityLevel == DreamDaemonSecurity.Ultrasafe)
								throw new JobException("This game must be run with at least the 'Safe' DreamDaemon security level!");
							job.MinimumSecurityLevel = DreamDaemonSecurity.Safe;
							return;
						case ApiValidationStatus.RequiresTrusted:
							if (securityLevel != DreamDaemonSecurity.Trusted)
								throw new JobException("This game must be run with at least the 'Trusted' DreamDaemon security level!");
							job.MinimumSecurityLevel = DreamDaemonSecurity.Trusted;
							return;
						case ApiValidationStatus.NeverValidated:
							break;
						case ApiValidationStatus.BadValidationRequest:
							throw new JobException("Recieved an unrecognized API validation request from DreamDaemon!");
						case ApiValidationStatus.UnaskedValidationRequest:
						default:
							throw new InvalidOperationException(String.Format(CultureInfo.InvariantCulture, "Session controller returned unexpected ApiValidationStatus: {0}", validationStatus));
					}

					job.DMApiVersion = controller.DMApiVersion;
				}

				throw new JobException("DMAPI validation timed out! Is the security level too high?");
			}
		}

		/// <summary>
		/// Compiles a .dme with DreamMaker
		/// </summary>
		/// <param name="dreamMakerPath">The path to the DreamMaker executable</param>
		/// <param name="job">The <see cref="Models.CompileJob"/> for the operation</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		async Task<int> RunDreamMaker(string dreamMakerPath, Models.CompileJob job, CancellationToken cancellationToken)
		{
			using (var dm = processExecutor.LaunchProcess(dreamMakerPath, ioManager.ResolvePath(ioManager.ConcatPath(job.DirectoryName.ToString(), ADirectoryName)), String.Format(CultureInfo.InvariantCulture, "-clean {0}.{1}", job.DmeName, DmeExtension), true, true))
			{
				int exitCode;
				using (cancellationToken.Register(() => dm.Terminate()))
					exitCode = await dm.Lifetime.ConfigureAwait(false);
				cancellationToken.ThrowIfCancellationRequested();

				logger.LogDebug("DreamMaker exit code: {0}", exitCode);
				job.Output = dm.GetCombinedOutput();
				logger.LogDebug("DreamMaker output: {0}{1}", Environment.NewLine, job.Output);
				return exitCode;
			}
		}

		/// <summary>
		/// Adds server side includes to the .dme being compiled
		/// </summary>
		/// <param name="job">The <see cref="Models.CompileJob"/> for the operation</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		async Task ModifyDme(Models.CompileJob job, CancellationToken cancellationToken)
		{
			var dirA = ioManager.ConcatPath(job.DirectoryName.ToString(), ADirectoryName);
			var dmeFileName = String.Join('.', job.DmeName, DmeExtension);
			var dmePath = ioManager.ConcatPath(dirA, dmeFileName);
			var dmeReadTask = ioManager.ReadAllBytes(dmePath, cancellationToken);

			var dmeModificationsTask = configuration.CopyDMFilesTo(dmeFileName, ioManager.ResolvePath(dirA), cancellationToken);

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
		/// <param name="job">The running <see cref="Models.CompileJob"/></param>
		/// <param name="cancelled">If the <paramref name="job"/> was cancelled</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		async Task CleanupFailedCompile(Models.CompileJob job, bool cancelled, CancellationToken cancellationToken)
		{
			logger.LogTrace("Cleaning compile directory...");
			var chatTask = chatManager.SendUpdateMessage(cancelled ? "Deploy cancelled!" : "Deploy failed!", cancellationToken);
			var jobPath = job.DirectoryName.ToString();
			try
			{
				await ioManager.DeleteDirectory(jobPath, CancellationToken.None).ConfigureAwait(false);
			}
			catch (Exception e)
			{
				logger.LogWarning("Error cleaning up compile directory {0}! Exception: {1}", ioManager.ResolvePath(jobPath), e);
			}

			await chatTask.ConfigureAwait(false);
		}

		/// <summary>
		/// Send a message to <see cref="chatManager"/> about a deployment
		/// </summary>
		/// <param name="revisionInformation">The <see cref="Models.RevisionInformation"/> for the deployment</param>
		/// <param name="byondLock">The <see cref="IByondExecutableLock"/> for the deployment</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		async Task SendDeploymentMessage(Models.RevisionInformation revisionInformation, IByondExecutableLock byondLock, CancellationToken cancellationToken)
		{
			var commitInsert = revisionInformation.CommitSha.Substring(0, 7);
			string remoteCommitInsert;
			if (revisionInformation.CommitSha == revisionInformation.OriginCommitSha)
			{
				commitInsert = String.Format(CultureInfo.InvariantCulture, "^{0}", commitInsert);
				remoteCommitInsert = String.Empty;
			}
			else
				remoteCommitInsert = String.Format(CultureInfo.InvariantCulture, ". Remote commit: ^{0}", revisionInformation.OriginCommitSha.Substring(0, 7));

			var testmergeInsert = revisionInformation.ActiveTestMerges.Count == 0 ? String.Empty : String.Format(CultureInfo.InvariantCulture, " (Test Merges: {0})",
				String.Join(", ", revisionInformation.ActiveTestMerges.Select(x => x.TestMerge).Select(x =>
				{
					var result = String.Format(CultureInfo.InvariantCulture, "#{0} at {1}", x.Number, x.PullRequestRevision.Substring(0, 7));
					if (x.Comment != null)
						result += String.Format(CultureInfo.InvariantCulture, " ({0})", x.Comment);
					return result;
				})));

			await chatManager.SendUpdateMessage(String.Format(CultureInfo.InvariantCulture, "Deploying revision: {0}{1}{2} BYOND Version: {3}", commitInsert, testmergeInsert, remoteCommitInsert, byondLock.Version), cancellationToken).ConfigureAwait(false);
		}

		/// <summary>
		/// Executes and populate a given <paramref name="job"/>
		/// </summary>
		/// <param name="job">The <see cref="Models.CompileJob"/> to run and populate</param>
		/// <param name="dreamMakerSettings">The <see cref="Api.Models.DreamMaker"/> settings to use</param>
		/// <param name="byondLock">The <see cref="IByondExecutableLock"/> to use</param>
		/// <param name="repository">The <see cref="IRepository"/> to use</param>
		/// <param name="apiValidateTimeout">The timeout for validating the DMAPI</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		async Task RunCompileJob(Models.CompileJob job, Api.Models.DreamMaker dreamMakerSettings, IByondExecutableLock byondLock, IRepository repository, uint apiValidateTimeout, CancellationToken cancellationToken)
		{
			var jobPath = job.DirectoryName.ToString();
			logger.LogTrace("Compile output GUID: {0}", jobPath);

			try
			{
				var dirA = ioManager.ConcatPath(jobPath, ADirectoryName);
				var dirB = ioManager.ConcatPath(jobPath, BDirectoryName);

				// copy the repository
				logger.LogTrace("Copying repository to game directory...");
				var resolvedADirectory = ioManager.ResolvePath(dirA);
				var repoOrigin = repository.Origin;
				using (repository)
					await repository.CopyTo(resolvedADirectory, cancellationToken).ConfigureAwait(false);

				// repository closed now

				// run precompile scripts
				await eventConsumer.HandleEvent(EventType.CompileStart, new List<string> { resolvedADirectory, repoOrigin }, cancellationToken).ConfigureAwait(false);

				// determine the dme
				if (job.DmeName == null)
				{
					logger.LogTrace("Searching for available .dmes...");
					var foundPaths = await ioManager.GetFilesWithExtension(dirA, DmeExtension, cancellationToken).ConfigureAwait(false);
					var foundPath = foundPaths.FirstOrDefault();
					if (foundPath == default)
						throw new JobException("Unable to find any .dme!");
					var dmeWithExtension = ioManager.GetFileName(foundPath);
					job.DmeName = dmeWithExtension.Substring(0, dmeWithExtension.Length - DmeExtension.Length - 1);
				}
				else
				{
					var targetDme = ioManager.ConcatPath(dirA, String.Join('.', job.DmeName, DmeExtension));
					var targetDmeExists = await ioManager.FileExists(targetDme, cancellationToken).ConfigureAwait(false);
					if (!targetDmeExists)
						throw new JobException("Unable to locate specified .dme!");
				}

				logger.LogDebug("Selected {0}.dme for compilation!", job.DmeName);

				await ModifyDme(job, cancellationToken).ConfigureAwait(false);

				// run compiler
				var exitCode = await RunDreamMaker(byondLock.DreamMakerPath, job, cancellationToken).ConfigureAwait(false);

				// verify api
				try
				{
					if (exitCode != 0)
						throw new JobException(String.Format(CultureInfo.InvariantCulture, "DM exited with a non-zero code: {0}{1}{2}", exitCode, Environment.NewLine, job.Output));

					await VerifyApi(apiValidateTimeout, dreamMakerSettings.ApiValidationSecurityLevel.Value, job, byondLock, dreamMakerSettings.ApiValidationPort.Value, cancellationToken).ConfigureAwait(false);
				}
				catch (JobException)
				{
					// DD never validated or compile failed
					await eventConsumer.HandleEvent(EventType.CompileFailure, new List<string> { resolvedADirectory, exitCode == 0 ? "1" : "0" }, cancellationToken).ConfigureAwait(false);
					throw;
				}

				logger.LogTrace("Running post compile event...");
				await eventConsumer.HandleEvent(EventType.CompileComplete, new List<string> { resolvedADirectory }, cancellationToken).ConfigureAwait(false);

				logger.LogTrace("Duplicating compiled game...");

				// duplicate the dmb et al
				await ioManager.CopyDirectory(dirA, dirB, null, cancellationToken).ConfigureAwait(false);

				logger.LogTrace("Applying static game file symlinks...");

				// symlink in the static data
				var symATask = configuration.SymlinkStaticFilesTo(resolvedADirectory, cancellationToken);
				var symBTask = configuration.SymlinkStaticFilesTo(ioManager.ResolvePath(dirB), cancellationToken);

				await Task.WhenAll(symATask, symBTask).ConfigureAwait(false);

				await chatManager.SendUpdateMessage(String.Format(CultureInfo.InvariantCulture, "Deployment complete!{0}", watchdog.Running ? " Changes will be applied on next server reboot." : String.Empty), cancellationToken).ConfigureAwait(false);

				logger.LogDebug("Compile complete!");
			}
			catch (Exception e)
			{
				logger.LogDebug("Cleaning up failed compile due to exception: {0}", e);
				await CleanupFailedCompile(job, e is OperationCanceledException, cancellationToken).ConfigureAwait(false);
				throw;
			}
		}

		/// <inheritdoc />
		public async Task<Models.CompileJob> Compile(Models.RevisionInformation revisionInformation, Api.Models.DreamMaker dreamMakerSettings, uint apiValidateTimeout, IRepository repository, Action<int> progressReporter, TimeSpan? estimatedDuration, CancellationToken cancellationToken)
		{
			if (revisionInformation == null)
				throw new ArgumentNullException(nameof(revisionInformation));

			if (dreamMakerSettings == null)
				throw new ArgumentNullException(nameof(dreamMakerSettings));

			if (repository == null)
				throw new ArgumentNullException(nameof(repository));

			if (progressReporter == null)
				throw new ArgumentNullException(nameof(progressReporter));

			logger.LogTrace("Begin Compile");

			lock (this)
			{
				if (compiling)
					throw new JobException("There is already a compile job in progress!");
				compiling = true;
			}

			using (var progressCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
			{
				var progressTask = estimatedDuration.HasValue ? ProgressTask(progressReporter, estimatedDuration.Value, cancellationToken) : Task.CompletedTask;
				try
				{
					using (var byondLock = await byond.UseExecutables(null, cancellationToken).ConfigureAwait(false))
					{
						await SendDeploymentMessage(revisionInformation, byondLock, cancellationToken).ConfigureAwait(false);

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
				}
				catch (OperationCanceledException)
				{
					await eventConsumer.HandleEvent(EventType.CompileCancelled, null, default).ConfigureAwait(false);
					throw;
				}
				finally
				{
					compiling = false;
					progressCts.Cancel();
					await progressTask.ConfigureAwait(false);
				}
			}
		}
	}
}
