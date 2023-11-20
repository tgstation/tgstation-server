using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Mono.Unix;
using Mono.Unix.Native;

using Tgstation.Server.Api.Models;
using Tgstation.Server.Host.IO;
using Tgstation.Server.Host.Jobs;

namespace Tgstation.Server.Host.System
{
	/// <inheritdoc />
	sealed class PosixProcessFeatures : IProcessFeatures
	{
		/// <summary>
		/// <see cref="Lazy{T}"/> loaded <see cref="IProcessExecutor"/>.
		/// </summary>
		readonly Lazy<IProcessExecutor> lazyLoadedProcessExecutor;

		/// <summary>
		/// The <see cref="IIOManager"/> for the <see cref="PosixProcessFeatures"/>.
		/// </summary>
		readonly IIOManager ioManager;

		/// <summary>
		/// The <see cref="ILogger{TCategoryName}"/> for the <see cref="PosixProcessFeatures"/>.
		/// </summary>
		readonly ILogger<PosixProcessFeatures> logger;

		/// <summary>
		/// Initializes a new instance of the <see cref="PosixProcessFeatures"/> class.
		/// </summary>
		/// <param name="lazyLoadedProcessExecutor">The value of <see cref="lazyLoadedProcessExecutor"/>.</param>
		/// <param name="ioManager">The value of <see cref="ioManager"/>.</param>
		/// <param name="logger">The value of <see cref="logger"/>.</param>
		public PosixProcessFeatures(Lazy<IProcessExecutor> lazyLoadedProcessExecutor, IIOManager ioManager, ILogger<PosixProcessFeatures> logger)
		{
			this.lazyLoadedProcessExecutor = lazyLoadedProcessExecutor ?? throw new ArgumentNullException(nameof(lazyLoadedProcessExecutor));
			this.ioManager = ioManager ?? throw new ArgumentNullException(nameof(ioManager));
			this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
		}

		/// <inheritdoc />
		public void ResumeProcess(global::System.Diagnostics.Process process)
		{
			var result = Syscall.kill(process.Id, Signum.SIGCONT);
			if (result != 0)
				throw new UnixIOException(Stdlib.GetLastError());
		}

		/// <inheritdoc />
		public void SuspendProcess(global::System.Diagnostics.Process process)
		{
			var result = Syscall.kill(process.Id, Signum.SIGSTOP);
			if (result != 0)
				throw new UnixIOException(Stdlib.GetLastError());
		}

		/// <inheritdoc />
		public string GetExecutingUsername(global::System.Diagnostics.Process process)
			=> throw new NotSupportedException();

		/// <inheritdoc />
		public async ValueTask CreateDump(global::System.Diagnostics.Process process, string outputFile, CancellationToken cancellationToken)
		{
			ArgumentNullException.ThrowIfNull(process);
			ArgumentNullException.ThrowIfNull(outputFile);

			const string GCorePath = "/usr/bin/gcore";
			if (!await ioManager.FileExists(GCorePath, cancellationToken))
				throw new JobException(ErrorCode.MissingGCore);

			int pid;
			try
			{
				if (process.HasExited)
					throw new JobException(ErrorCode.GameServerOffline);

				pid = process.Id;
			}
			catch (InvalidOperationException ex)
			{
				throw new JobException(ErrorCode.GameServerOffline, ex);
			}

			string output;
			int exitCode;
			await using (var gcoreProc = lazyLoadedProcessExecutor.Value.LaunchProcess(
				GCorePath,
				Environment.CurrentDirectory,
				$"-o {outputFile} {process.Id}",
				readStandardHandles: true,
				noShellExecute: true))
			{
				using (cancellationToken.Register(() => gcoreProc.Terminate()))
					exitCode = (await gcoreProc.Lifetime).Value;

				output = await gcoreProc.GetCombinedOutput(cancellationToken);
				logger.LogDebug("gcore output:{0}{1}", Environment.NewLine, output);
			}

			if (exitCode != 0)
				throw new JobException(
					ErrorCode.GCoreFailure,
					new JobException(
						$"Exit Code: {exitCode}{Environment.NewLine}Output:{Environment.NewLine}{output}"));

			// gcore outputs name.pid so remove the pid part
			var generatedGCoreFile = $"{outputFile}.{pid}";
			await ioManager.MoveFile(generatedGCoreFile, outputFile, cancellationToken);
		}
	}
}
