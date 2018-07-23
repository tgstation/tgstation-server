using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Api.Models;
using Tgstation.Server.Api.Models.Internal;
using Tgstation.Server.Host.Components.Watchdog;
using Tgstation.Server.Host.Core;
using Tgstation.Server.Host.IO;

namespace Tgstation.Server.Host.Components
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
		const string DmeExtension = ".dme";

		/// <inheritdoc />
		public CompilerStatus Status { get; private set; }

		/// <summary>
		/// The <see cref="IByond"/> for <see cref="DreamMaker"/>
		/// </summary>
		readonly IByond byond;
		/// <summary>
		/// The <see cref="IIOManager"/> for <see cref="DreamMaker"/>
		/// </summary>
		readonly IIOManager ioManager;
		/// <summary>
		/// The <see cref="IConfiguration"/> for <see cref="DreamMaker"/>
		/// </summary>
		readonly IConfiguration configuration;
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
		/// The <see cref="ILogger"/> for <see cref="DreamMaker"/>
		/// </summary>
		readonly ILogger<DreamMaker> logger;

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
		/// <param name="logger">The value of <see cref="logger"/></param>
		public DreamMaker(IByond byond, IIOManager ioManager, IConfiguration configuration, ISessionControllerFactory sessionControllerFactory, ICompileJobConsumer compileJobConsumer, IApplication application, IEventConsumer eventConsumer, ILogger<DreamMaker> logger)
		{
			this.byond = byond;
			this.ioManager = ioManager ?? throw new ArgumentNullException(nameof(ioManager));
			this.configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
			this.sessionControllerFactory = sessionControllerFactory ?? throw new ArgumentNullException(nameof(sessionControllerFactory));
			this.compileJobConsumer = compileJobConsumer ?? throw new ArgumentNullException(nameof(compileJobConsumer));
			this.application = application ?? throw new ArgumentNullException(nameof(application));
			this.eventConsumer = eventConsumer ?? throw new ArgumentNullException(nameof(eventConsumer));
			this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
		}

		/// <summary>
		/// Run a quick DD instance to test the DMAPI is installed on the target code
		/// </summary>
		/// <param name="timeout">The timeout in seconds for validation</param>
		/// <param name="job">The <see cref="Models.CompileJob"/> for the operation</param>
		/// <param name="byondLock">The current <see cref="IByondExecutableLock"/></param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in <see langword="true"/> if the DMAPI was successfully validated, <see langword="false"/> otherwise</returns>
		async Task<bool> VerifyApi(int timeout, Models.CompileJob job, IByondExecutableLock byondLock, CancellationToken cancellationToken)
		{
			var launchParameters = new DreamDaemonLaunchParameters
			{
				AllowWebClient = false,
				PrimaryPort = 0,    //pick any port
				SecurityLevel = DreamDaemonSecurity.Safe,    //all it needs to read the file and exit
				StartupTimeout = timeout
			};

			var dirA = ioManager.ConcatPath(job.DirectoryName.ToString(), ADirectoryName);
			var provider = new TemporaryDmbProvider(ioManager.ResolvePath(ioManager.GetDirectoryName(dirA)), ioManager.ResolvePath(ioManager.ConcatPath(dirA, String.Concat(job.DmeName, DmbExtension))));

			var timeoutAt = DateTimeOffset.Now.AddSeconds(timeout);
			using (var controller = await sessionControllerFactory.LaunchNew(launchParameters, provider, byondLock, true, true, true, cancellationToken).ConfigureAwait(false))
			{
				var timeoutTask = Task.Delay(timeoutAt - DateTimeOffset.Now, cancellationToken);

				await Task.WhenAny(controller.Lifetime, timeoutTask).ConfigureAwait(false);

				if (!controller.Lifetime.IsCompleted)
					return false;

				return controller.ApiValidated;
			}
		}

		/// <summary>
		///	Compiles a .dme with DreamMaker
		/// </summary>
		/// <param name="dreamMakerPath">The path to the DreamMaker executable</param>
		/// <param name="job">The <see cref="Models.CompileJob"/> for the operation</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		async Task RunDreamMaker(string dreamMakerPath, Models.CompileJob job, CancellationToken cancellationToken)
		{
			using (var dm = new Process())
			{
				dm.StartInfo.FileName = dreamMakerPath;
				dm.StartInfo.Arguments = String.Format(CultureInfo.InvariantCulture, "-clean {0}{1}", job.DmeName, DmeExtension);
				dm.StartInfo.WorkingDirectory = ioManager.ResolvePath(ioManager.ConcatPath(job.DirectoryName.ToString(), ADirectoryName));
				dm.StartInfo.RedirectStandardOutput = true;
				dm.StartInfo.RedirectStandardError = true;
				var OutputList = new StringBuilder();
				var eventHandler = new DataReceivedEventHandler(
					delegate (object sender, DataReceivedEventArgs e)
					{
						OutputList.Append(Environment.NewLine);
						OutputList.Append(e.Data);
					}
				);
				dm.OutputDataReceived += eventHandler;
				dm.ErrorDataReceived += eventHandler;

				dm.EnableRaisingEvents = true;
				var dmTcs = new TaskCompletionSource<object>();
				dm.Exited += (a, b) => dmTcs.SetResult(null);

				dm.Start();
				try
				{
					using (cancellationToken.Register(() => dmTcs.SetCanceled()))
						await dmTcs.Task.ConfigureAwait(false);
				}
				finally
				{
					if (!dm.HasExited)
					{
						dm.Kill();
						dm.WaitForExit();
					}
				}

				job.Output = OutputList.ToString();
				job.ExitCode = dm.ExitCode;
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
			var dmePath = ioManager.ConcatPath(dirA, String.Concat(job.DmeName, DmeExtension));
			var dmeReadTask = ioManager.ReadAllBytes(dmePath, cancellationToken);

			var dmeModificationsTask = configuration.CopyDMFilesTo(dmePath, ioManager.ResolvePath(dirA), cancellationToken);

			var dmeBytes = await dmeReadTask.ConfigureAwait(false);
			var dme = Encoding.UTF8.GetString(dmeBytes);

			var dmeModifications = await dmeModificationsTask.ConfigureAwait(false);

			if (dmeModifications == null || dmeModifications.TotalDmeOverwrite)
				return;

			var dmeLines = new List<string>(dme.Split(new[] { Environment.NewLine }, StringSplitOptions.None));
			for (var I = 0; I < dmeLines.Count; ++I)
			{
				var line = dmeLines[I];
				if (line.Contains("BEGIN_INCLUDE") && dmeModifications.HeadIncludeLine != null)
				{
					dmeLines.Insert(I + 1, dmeModifications.HeadIncludeLine);
					++I;
				}
				else if (line.Contains("END_INCLUDE") && dmeModifications.TailIncludeLine != null)
				{
					dmeLines.Insert(I, dmeModifications.TailIncludeLine);
					break;
				}
			}

			dmeBytes = Encoding.UTF8.GetBytes(String.Join(Environment.NewLine, dmeLines));
			await ioManager.WriteAllBytes(dmePath, dmeBytes, cancellationToken).ConfigureAwait(false);
		}

		/// <inheritdoc />
		public async Task<Models.CompileJob> Compile(string projectName, int apiValidateTimeout, IRepository repository, CancellationToken cancellationToken)
		{
			logger.LogTrace("Begin Compile");
			await eventConsumer.HandleEvent(EventType.CompileStart, new List<string>{ repository.Origin }, cancellationToken).ConfigureAwait(false);
			try
			{
				Status = CompilerStatus.Copying;
				var job = new Models.CompileJob
				{
					DirectoryName = Guid.NewGuid(),
					DmeName = projectName
				};
				await ioManager.CreateDirectory(job.DirectoryName.ToString(), cancellationToken).ConfigureAwait(false);
				var dirA = ioManager.ConcatPath(job.DirectoryName.ToString(), ADirectoryName);
				var dirB = ioManager.ConcatPath(job.DirectoryName.ToString(), BDirectoryName);

				async Task CleanupFailedCompile()
				{
					Status = CompilerStatus.Cleanup;
					try
					{
						await ioManager.DeleteDirectory(job.DirectoryName.ToString(), CancellationToken.None).ConfigureAwait(false);
					}
					catch { }
				};

				try
				{
					//copy the repository
					var fullDirA = ioManager.ResolvePath(dirA);
					using (repository)
						await repository.CopyTo(fullDirA, cancellationToken).ConfigureAwait(false);

					Status = CompilerStatus.Modifying;

					if (job.DmeName == null)
					{
						job.DmeName = (await ioManager.GetFilesWithExtension(dirA, DmeExtension, cancellationToken).ConfigureAwait(false)).FirstOrDefault();
						if (job.DmeName == default)
						{
							job.Output = "Unable to find any .dme!";
							return job;
						}
					}

					await ModifyDme(job, cancellationToken).ConfigureAwait(false);

					Status = CompilerStatus.Compiling;

					//run compiler, verify api
					bool ddVerified;
					using (var byondLock = byond.UseExecutables(null))
					{
						job.ByondVersion = byondLock.Version.ToString();

						await RunDreamMaker(byondLock.DreamMakerPath, job, cancellationToken).ConfigureAwait(false);

						Status = CompilerStatus.Verifying;

						ddVerified = job.ExitCode == 0 && await VerifyApi(apiValidateTimeout, job, byondLock, cancellationToken).ConfigureAwait(false);
					}

					if (!ddVerified)
						//server never validated or compile failed
						await Task.WhenAll(CleanupFailedCompile(), eventConsumer.HandleEvent(EventType.CompileFailure, new List<string> { job.ExitCode == 0 ? "1" : "0" }, cancellationToken)).ConfigureAwait(false);
					else
					{
						job.DMApiValidated = true;

						Status = CompilerStatus.Duplicating;

						//duplicate the dmb et al
						await ioManager.CopyDirectory(dirA, dirB, null, cancellationToken).ConfigureAwait(false);

						Status = CompilerStatus.Symlinking;

						//symlink in the static data
						var symATask = configuration.SymlinkStaticFilesTo(fullDirA, cancellationToken);
						var symBTask = configuration.SymlinkStaticFilesTo(ioManager.ResolvePath(dirB), cancellationToken);

						await Task.WhenAll(symATask, symBTask).ConfigureAwait(false);
						await eventConsumer.HandleEvent(EventType.CompileComplete, null, cancellationToken).ConfigureAwait(false);
					}
					await compileJobConsumer.LoadCompileJob(job, cancellationToken).ConfigureAwait(false);
					return job;
				}
				catch
				{
					await CleanupFailedCompile().ConfigureAwait(false);
					throw;
				}
			}
			catch (OperationCanceledException)
			{
				await eventConsumer.HandleEvent(EventType.CompileCancelled, null, default).ConfigureAwait(false);
				throw;
			}
			finally
			{
				Status = CompilerStatus.Idle;
			}
		}
	}
}
