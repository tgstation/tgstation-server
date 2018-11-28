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
using Tgstation.Server.Host.Core;
using Tgstation.Server.Host.IO;

namespace Tgstation.Server.Host.Components.Compiler
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
		/// The <see cref="ICompileJobConsumer"/> for <see cref="DreamMaker"/>
		/// </summary>
		readonly ICompileJobConsumer compileJobConsumer;

		/// <summary>
		/// The <see cref="IApplication"/> for <see cref="DreamMaker"/>
		/// </summary>
		readonly IApplication application;

		/// <summary>
		/// The <see cref="IEventConsumer"/> for <see cref="DreamMaker"/>
		/// </summary>
		readonly IEventConsumer eventConsumer;

		/// <summary>
		/// The <see cref="IChat"/> for <see cref="DreamMaker"/>
		/// </summary>
		readonly IChat chat;

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
		/// <param name="compileJobConsumer">The value of <see cref="compileJobConsumer"/></param>
		/// <param name="application">The value of <see cref="application"/></param>
		/// <param name="eventConsumer">The value of <see cref="eventConsumer"/></param>
		/// <param name="chat">The value of <see cref="chat"/></param>
		/// <param name="processExecutor">The value of <see cref="processExecutor"/></param>
		/// <param name="watchdog">The value of <see cref="watchdog"/></param>
		/// <param name="logger">The value of <see cref="logger"/></param>
		public DreamMaker(IByondManager byond, IIOManager ioManager, StaticFiles.IConfiguration configuration, ISessionControllerFactory sessionControllerFactory, ICompileJobConsumer compileJobConsumer, IApplication application, IEventConsumer eventConsumer, IChat chat, IProcessExecutor processExecutor, IWatchdog watchdog, ILogger<DreamMaker> logger)
		{
			this.byond = byond ?? throw new ArgumentNullException(nameof(byond));
			this.ioManager = ioManager ?? throw new ArgumentNullException(nameof(ioManager));
			this.configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
			this.sessionControllerFactory = sessionControllerFactory ?? throw new ArgumentNullException(nameof(sessionControllerFactory));
			this.compileJobConsumer = compileJobConsumer ?? throw new ArgumentNullException(nameof(compileJobConsumer));
			this.application = application ?? throw new ArgumentNullException(nameof(application));
			this.eventConsumer = eventConsumer ?? throw new ArgumentNullException(nameof(eventConsumer));
			this.chat = chat ?? throw new ArgumentNullException(nameof(chat));
			this.processExecutor = processExecutor ?? throw new ArgumentNullException(nameof(processExecutor));
			this.watchdog = watchdog ?? throw new ArgumentNullException(nameof(watchdog));
			this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
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
			var provider = new TemporaryDmbProvider(ioManager.ResolvePath(dirA), String.Concat(job.DmeName, DmbExtension), job);

			var timeoutAt = DateTimeOffset.Now.AddSeconds(timeout);
			using (var controller = await sessionControllerFactory.LaunchNew(launchParameters, provider, byondLock, true, true, true, cancellationToken).ConfigureAwait(false))
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
				}

				throw new JobException("DMAPI validation timed out!");
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
				logger.LogTrace("DreamMaker output: {0}{1}", Environment.NewLine, job.Output);
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

			if (dreamMakerSettings.ApiValidationSecurityLevel == DreamDaemonSecurity.Ultrasafe)
				throw new ArgumentOutOfRangeException(nameof(dreamMakerSettings), dreamMakerSettings, "Cannot compile with ultrasafe security!");

			logger.LogTrace("Begin Compile");

			var job = new Models.CompileJob
			{
				DirectoryName = Guid.NewGuid(),
				DmeName = dreamMakerSettings.ProjectName,
				RevisionInformation = revisionInformation
			};

			logger.LogTrace("Compile output GUID: {0}", job.DirectoryName);

			lock (this)
			{
				if (compiling)
					throw new JobException("There is already a compile job in progress!");
				compiling = true;
			}

			using (var progressCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
			{
				async Task ProgressTask()
				{
					if (!estimatedDuration.HasValue)
						return;

					progressReporter(0);
					var ct = progressCts.Token;
					var sleepInterval = estimatedDuration.Value / 100;

					try
					{
						for (var I = 0; I < 99; ++I)
						{
							await Task.Delay(sleepInterval, progressCts.Token).ConfigureAwait(false);
							progressReporter(I + 1);
						}
					}
					catch (OperationCanceledException) { }
				}

				var progressTask = ProgressTask();
				try
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

					using (var byondLock = await byond.UseExecutables(null, cancellationToken).ConfigureAwait(false))
					{
						await chat.SendUpdateMessage(String.Format(CultureInfo.InvariantCulture, "Deploying revision: {0}{1}{2} BYOND Version: {3}", commitInsert, testmergeInsert, remoteCommitInsert, byondLock.Version), cancellationToken).ConfigureAwait(false);

						async Task CleanupFailedCompile(bool cancelled)
						{
							logger.LogTrace("Cleaning compile directory...");
							var chatTask = chat.SendUpdateMessage(cancelled ? "Deploy cancelled!" : "Deploy failed!", cancellationToken);
							try
							{
								await ioManager.DeleteDirectory(job.DirectoryName.ToString(), CancellationToken.None).ConfigureAwait(false);
							}
							catch (Exception e)
							{
								logger.LogWarning("Error cleaning up compile directory {0}! Exception: {1}", ioManager.ResolvePath(job.DirectoryName.ToString()), e);
							}

							await chatTask.ConfigureAwait(false);
						}

						try
						{
							await ioManager.CreateDirectory(job.DirectoryName.ToString(), cancellationToken).ConfigureAwait(false);

							var dirA = ioManager.ConcatPath(job.DirectoryName.ToString(), ADirectoryName);
							var dirB = ioManager.ConcatPath(job.DirectoryName.ToString(), BDirectoryName);

							// copy the repository
							logger.LogTrace("Copying repository to game directory...");
							var fullDirA = ioManager.ResolvePath(dirA);
							var repoOrigin = repository.Origin;
							using (repository)
								await repository.CopyTo(fullDirA, cancellationToken).ConfigureAwait(false);

							// run precompile scripts
							var resolvedGameDirectory = ioManager.ResolvePath(ioManager.ConcatPath(job.DirectoryName.ToString(), ADirectoryName));
							await eventConsumer.HandleEvent(EventType.CompileStart, new List<string> { resolvedGameDirectory, repoOrigin }, cancellationToken).ConfigureAwait(false);

							// determine the dme
							if (job.DmeName == null)
							{
								logger.LogTrace("Searching for available .dmes...");
								var path = (await ioManager.GetFilesWithExtension(dirA, DmeExtension, cancellationToken).ConfigureAwait(false)).FirstOrDefault();
								if (path == default)
									throw new JobException("Unable to find any .dme!");
								var dmeWithExtension = ioManager.GetFileName(path);
								job.DmeName = dmeWithExtension.Substring(0, dmeWithExtension.Length - DmeExtension.Length - 1);
							}
							else if (!await ioManager.FileExists(ioManager.ConcatPath(dirA, String.Join('.', job.DmeName, DmeExtension)), cancellationToken).ConfigureAwait(false))
								throw new JobException("Unable to locate specified .dme!");

							logger.LogDebug("Selected {0}.dme for compilation!", job.DmeName);

							await ModifyDme(job, cancellationToken).ConfigureAwait(false);

							// run compiler
							job.ByondVersion = byondLock.Version.ToString();

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
								await eventConsumer.HandleEvent(EventType.CompileFailure, new List<string> { resolvedGameDirectory, exitCode == 0 ? "1" : "0" }, cancellationToken).ConfigureAwait(false);
								throw;
							}

							logger.LogTrace("Running post compile event...");
							await eventConsumer.HandleEvent(EventType.CompileComplete, new List<string> { ioManager.ResolvePath(ioManager.ConcatPath(job.DirectoryName.ToString(), ADirectoryName)) }, cancellationToken).ConfigureAwait(false);

							logger.LogTrace("Duplicating compiled game...");

							// duplicate the dmb et al
							await ioManager.CopyDirectory(dirA, dirB, null, cancellationToken).ConfigureAwait(false);

							logger.LogTrace("Applying static game file symlinks...");

							// symlink in the static data
							var symATask = configuration.SymlinkStaticFilesTo(fullDirA, cancellationToken);
							var symBTask = configuration.SymlinkStaticFilesTo(ioManager.ResolvePath(dirB), cancellationToken);

							await Task.WhenAll(symATask, symBTask).ConfigureAwait(false);

							await chat.SendUpdateMessage(String.Format(CultureInfo.InvariantCulture, "Deployment complete!{0}", watchdog.Running ? " Changes will be applied on next server reboot." : String.Empty), cancellationToken).ConfigureAwait(false);

							logger.LogDebug("Compile complete!");
							return job;
						}
						catch (Exception e)
						{
							await CleanupFailedCompile(e is OperationCanceledException).ConfigureAwait(false);
							throw;
						}
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
