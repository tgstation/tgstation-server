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
using Tgstation.Server.Host.Components.Models;
using Tgstation.Server.Host.Core;
using Tgstation.Server.Host.Security;

namespace Tgstation.Server.Host.Components
{
	/// <inheritdoc />
	sealed class DreamMaker : IDreamMaker
	{
		/// <summary>
		/// Name of the primary directory used for compilation
		/// </summary>
		const string ADirectoryName = "A";

		/// <summary>
		/// The <see cref="IRepositoryManager"/> for <see cref="DreamMaker"/>
		/// </summary>
		readonly IRepositoryManager repositoryManager;
		/// <summary>
		/// The <see cref="IIOManager"/> for <see cref="DreamMaker"/>
		/// </summary>
		readonly IIOManager ioManager;
		/// <summary>
		/// The <see cref="IConfiguration"/> for <see cref="DreamMaker"/>
		/// </summary>
		readonly IConfiguration configuration;
		/// <summary>
		/// The <see cref="IDreamDaemonExecutor"/> for <see cref="DreamMaker"/>
		/// </summary>
		readonly IDreamDaemonExecutor dreamDaemonExecutor;
		/// <summary>
		/// The <see cref="IByond"/> for <see cref="DreamMaker"/>
		/// </summary>
		readonly IByond byond;
		/// <summary>
		/// The <see cref="IInterop"/> for <see cref="DreamMaker"/>
		/// </summary>
		readonly IInterop interop;
		/// <summary>
		/// The <see cref="ICryptographySuite"/> for <see cref="DreamMaker"/>
		/// </summary>
		readonly ICryptographySuite cryptographySuite;

		/// <summary>
		/// Construct <see cref="DreamMaker"/>
		/// </summary>
		/// <param name="repositoryManager">The value of <see cref="repositoryManager"/></param>
		/// <param name="ioManager">The value of <see cref="ioManager"/></param>
		/// <param name="configuration">The value of <see cref="configuration"/></param>
		/// <param name="dreamDaemonExecutor">The value of <see cref="dreamDaemonExecutor"/></param>
		/// <param name="byond">The value of <see cref="byond"/></param>
		/// <param name="interop">The value of <see cref="interop"/></param>
		/// <param name="cryptographySuite">The value of <see cref="cryptographySuite"/></param>
		public DreamMaker(IRepositoryManager repositoryManager, IIOManager ioManager, IConfiguration configuration, IDreamDaemonExecutor dreamDaemonExecutor, IByond byond, IInterop interop, ICryptographySuite cryptographySuite)
		{
			this.repositoryManager = repositoryManager ?? throw new ArgumentNullException(nameof(repositoryManager));
			this.ioManager = ioManager ?? throw new ArgumentNullException(nameof(ioManager));
			this.configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
			this.dreamDaemonExecutor = dreamDaemonExecutor ?? throw new ArgumentNullException(nameof(dreamDaemonExecutor));
			this.byond = byond ?? throw new ArgumentNullException(nameof(byond));
			this.interop = interop ?? throw new ArgumentNullException(nameof(interop));
			this.cryptographySuite = cryptographySuite ?? throw new ArgumentNullException(nameof(cryptographySuite));
		}

		/// <summary>
		/// Run a quick DD instance to test the DMAPI is installed on the target code
		/// </summary>
		/// <param name="dreamDaemonPath">The path to the DreamDaemon executable</param>
		/// <param name="job">The <see cref="Host.Models.CompileJob"/> for the operation</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in <see langword="true"/> if the DMAPI was successfully validated, <see langword="false"/> otherwise</returns>
		async Task<bool> VerifyApi(string dreamDaemonPath, Host.Models.CompileJob job, CancellationToken cancellationToken)
		{
			var launchParameters = new DreamDaemonLaunchParameters
			{
				AllowWebClient = false,
				PrimaryPort = 0,	//pick any port
				SecurityLevel = DreamDaemonSecurity.Safe    //all it needs to read the file and exit
			};

			using (var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
			{
				var interopInfo = new InteropInfo
				{
					ApiValidateOnly = true,
					HostPath = Application.HostingPath,
					AccessToken = cryptographySuite.GetSecureString()
				};

				using (var control = interop.CreateRun(launchParameters.PrimaryPort, null, null))
				{
					var ddTcs = new TaskCompletionSource<object>();
					control.OnServerControl += (sender, e) => {
						if (e.EventType == ServerControlEventType.ServerUnresponsive)
							ddTcs.SetResult(null);
					};
					var dirA = ioManager.ConcatPath(job.DirectoryName.ToString(), ADirectoryName);
					var ddTestTask = dreamDaemonExecutor.RunDreamDaemon(launchParameters, null, dreamDaemonPath, new TemporaryDmbProvider(ioManager.ResolvePath(ioManager.GetDirectoryName(dirA)), ioManager.ResolvePath(ioManager.ConcatPath(dirA, String.Concat(job.DmeName, ".dmb")))), interopInfo, true, cts.Token);

					await Task.WhenAny(ddTcs.Task, ddTestTask).ConfigureAwait(false);

					if (!ddTestTask.IsCompleted)
						cts.Cancel();

					return await ddTestTask.ConfigureAwait(false) == 0 && !ddTcs.Task.IsCompleted;
				}
			}
		}

		/// <summary>
		///	Compiles a .dme with DreamMaker
		/// </summary>
		/// <param name="dreamMakerPath">The path to the DreamMaker executable</param>
		/// <param name="job">The <see cref="Host.Models.CompileJob"/> for the operation</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		async Task RunDreamMaker(string dreamMakerPath, Host.Models.CompileJob job, CancellationToken cancellationToken)
		{
			using (var dm = new Process())
			{
				dm.StartInfo.FileName = dreamMakerPath;
				dm.StartInfo.Arguments = String.Format(CultureInfo.InvariantCulture, "-clean {0}.dme", job.DmeName);
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

		async Task ModifyDme(Host.Models.CompileJob job, CancellationToken cancellationToken)
		{
			var dirA = ioManager.ConcatPath(job.DirectoryName.ToString(), ADirectoryName);
			var dmePath = ioManager.ConcatPath(dirA, String.Concat(job.DmeName, ".dme"));
			var dmeReadTask = ioManager.ReadAllBytes(dmePath, cancellationToken);

			var dmeModificationsTask = configuration.CopyDMFilesTo(ioManager.ResolvePath(dirA), cancellationToken);

			var dmeBytes = await dmeReadTask.ConfigureAwait(false);
			var dme = Encoding.UTF8.GetString(dmeBytes);

			var dmeModifications = await dmeModificationsTask.ConfigureAwait(false);

			if (!dmeModifications.Any())
				return;

			var dmeLines = new List<string>(dme.Split(new[] { Environment.NewLine }, StringSplitOptions.None));
			for (var I = 0; I < dmeLines.Count; ++I)
			{
				var line = dmeLines[I];
				if (line.Contains("BEGIN_INCLUDE"))
				{
					dmeLines.InsertRange(I + 1, dmeModifications);
					break;
				}
			}

			dmeBytes = Encoding.UTF8.GetBytes(String.Join(Environment.NewLine, dmeLines));
			await ioManager.WriteAllBytes(dmePath, dmeBytes, cancellationToken).ConfigureAwait(false);
		}

		/// <inheritdoc />
		public async Task<Host.Models.CompileJob> Compile(string dmeName, CancellationToken cancellationToken)
		{
			var job = new Host.Models.CompileJob
			{
				DirectoryName = Guid.NewGuid(),
				StartedAt = DateTimeOffset.Now,
				DmeName = dmeName
			};

			await ioManager.CreateDirectory(job.DirectoryName.ToString(), cancellationToken).ConfigureAwait(false);
			var dirA = ioManager.ConcatPath(job.DirectoryName.ToString(), ADirectoryName);
			var dirB = ioManager.ConcatPath(job.DirectoryName.ToString(), "B");

			async Task CleanupFailedCompile()
			{
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
				using (var repository = await repositoryManager.LoadRepository(cancellationToken).ConfigureAwait(false))
				{
					job.RevisionInformation = new Host.Models.RevisionInformation
					{
						Commit = repository.Head
					};
					await repository.CopyTo(fullDirA, cancellationToken).ConfigureAwait(false);
				}

				await ModifyDme(job, cancellationToken).ConfigureAwait(false);

				//run compiler, verify api
				var ddVerified = await byond.UseExecutables(async (dreamMakerPath, dreamDaemonPath) =>
				{
					await RunDreamMaker(dreamMakerPath, job, cancellationToken).ConfigureAwait(false);

					return await VerifyApi(dreamDaemonPath, job, cancellationToken).ConfigureAwait(false);
				}, true).ConfigureAwait(false);

				if(!ddVerified)
				{
					//server never validated
					job.FinishedAt = DateTimeOffset.Now;
					await CleanupFailedCompile().ConfigureAwait(false);
					return job;
				}

				job.DMApiValidated = true;

				//duplicate the dmb et al
				await ioManager.CopyDirectory(dirA, dirB, null, cancellationToken).ConfigureAwait(false);

				//symlink in the static data
				var symATask = configuration.SymlinkStaticFilesTo(fullDirA, cancellationToken);
				await configuration.SymlinkStaticFilesTo(ioManager.ResolvePath(dirB), cancellationToken).ConfigureAwait(false);
				await symATask.ConfigureAwait(false);

				job.FinishedAt = DateTimeOffset.Now;
				return job;
			}
			catch
			{
				await CleanupFailedCompile().ConfigureAwait(false);
				throw;
			}
		}
	}
}
