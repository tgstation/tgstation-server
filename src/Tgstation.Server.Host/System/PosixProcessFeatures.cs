using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Mono.Unix;
using Mono.Unix.Native;

using Tgstation.Server.Api.Models;
using Tgstation.Server.Host.IO;
using Tgstation.Server.Host.Jobs;

namespace Tgstation.Server.Host.System
{
	/// <inheritdoc />
	sealed class PosixProcessFeatures : IProcessFeatures, IHostedService
	{
		/// <summary>
		/// Difference from <see cref="baselineOomAdjust"/> to set our own oom_score_adj to. 1 higher host watchdog.
		/// </summary>
		const short SelfOomAdjust = 1;

		/// <summary>
		/// Difference from <see cref="baselineOomAdjust"/> to set the oom_score_adj of child processes to. 1 higher than ourselves.
		/// </summary>
		const short ChildProcessOomAdjust = SelfOomAdjust + 1;

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
		/// The original value of oom_score_adj as read from the /proc/ filesystem. Inherited from parent process.
		/// </summary>
		short baselineOomAdjust;

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

		/// <summary>
		/// Gets potential paths to the gcore executable.
		/// </summary>
		/// <returns>The potential paths to the gcore executable.</returns>
		static IEnumerable<string> GetPotentialGCorePaths()
		{
			var enviromentPath = Environment.GetEnvironmentVariable("PATH");
			IEnumerable<string> enumerator;
			if (enviromentPath == null)
				enumerator = Enumerable.Empty<string>();
			else
			{
				var paths = enviromentPath.Split(';');
				enumerator = paths
					.Select(x => x.Split(':'))
					.SelectMany(x => x);
			}

			var exeName = "gcore";

			enumerator = enumerator
				.Concat(new List<string>(2)
				{
					"/usr/bin",
					"/usr/share/bin",
					"/bin",
				});

			enumerator = enumerator.Select(x => Path.Combine(x, exeName));

			return enumerator;
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
		public async ValueTask CreateDump(global::System.Diagnostics.Process process, string outputFile, bool minidump, CancellationToken cancellationToken)
		{
			ArgumentNullException.ThrowIfNull(process);
			ArgumentNullException.ThrowIfNull(outputFile);

			string? gcorePath = null;
			foreach (var path in GetPotentialGCorePaths())
				if (await ioManager.FileExists(path, cancellationToken))
				{
					gcorePath = path;
					break;
				}

			if (gcorePath == null)
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

			string? output;
			int exitCode;
			await using (var gcoreProc = await lazyLoadedProcessExecutor.Value.LaunchProcess(
				gcorePath,
				Environment.CurrentDirectory,
				$"{(!minidump ? "-a " : String.Empty)}-o {outputFile} {process.Id}",
				cancellationToken,
				readStandardHandles: true,
				noShellExecute: true))
			{
				using (cancellationToken.Register(() => gcoreProc.Terminate()))
					exitCode = (await gcoreProc.Lifetime).Value;

				output = await gcoreProc.GetCombinedOutput(cancellationToken);
				logger.LogDebug("gcore output:{newline}{output}", Environment.NewLine, output);
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

		/// <inheritdoc />
		public async ValueTask<int> HandleProcessStart(global::System.Diagnostics.Process process, CancellationToken cancellationToken)
		{
			ArgumentNullException.ThrowIfNull(process);
			var pid = process.Id;
			try
			{
				// make sure all processes we spawn are killed _before_ us
				await AdjustOutOfMemoryScore(pid, ChildProcessOomAdjust, cancellationToken);
			}
			catch (Exception ex) when (ex is not OperationCanceledException)
			{
				logger.LogWarning(ex, "Failed to adjust OOM killer score for pid {pid}!", pid);
			}

			return pid;
		}

		/// <inheritdoc />
		public async Task StartAsync(CancellationToken cancellationToken)
		{
			// let this all throw
			string originalString;
			{
				// can't use ReadAllBytes here, /proc files have 0 length so the buffer is initialized to empty
				// https://stackoverflow.com/questions/12237712/how-can-i-show-the-size-of-files-in-proc-it-should-not-be-size-zero
				await using var fileStream = ioManager.CreateAsyncSequentialReadStream(
					"/proc/self/oom_score_adj");
				using var reader = new StreamReader(fileStream, Encoding.UTF8, leaveOpen: true);
				originalString = await reader.ReadToEndAsync(cancellationToken);
			}

			var trimmedString = originalString.Trim();

			logger.LogTrace("Original oom_score_adj is \"{original}\"", trimmedString);

			var originalOomAdjust = Int16.Parse(trimmedString, CultureInfo.InvariantCulture);
			baselineOomAdjust = Math.Clamp(originalOomAdjust, (short)-1000, (short)1000);

			if (baselineOomAdjust == 1000)
				if (originalOomAdjust != baselineOomAdjust)
					logger.LogWarning("oom_score_adj is at it's limit of 1000 (Clamped from {original}). TGS cannot guarantee the kill order of its parent/child processes!", originalOomAdjust);
				else
					logger.LogWarning("oom_score_adj is at it's limit of 1000. TGS cannot guarantee the kill order of its parent/child processes!");

			try
			{
				// we do not want to be killed before the host watchdog
				await AdjustOutOfMemoryScore(null, SelfOomAdjust, cancellationToken);
			}
			catch (Exception ex) when (ex is not OperationCanceledException)
			{
				logger.LogWarning(ex, "Could not increase oom_score_adj!");
			}
		}

		/// <inheritdoc />
		public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

		/// <summary>
		/// Set oom_score_adj for a given <paramref name="pid"/>.
		/// </summary>
		/// <param name="pid">The <see cref="global::System.Diagnostics.Process.Id"/> or <see langword="null"/> to self adjust.</param>
		/// <param name="adjustment">The value being written to the adjustment file.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask"/> representing the running operation.</returns>
		ValueTask AdjustOutOfMemoryScore(int? pid, short adjustment, CancellationToken cancellationToken)
		{
			var adjustedValue = Math.Clamp(baselineOomAdjust + adjustment, -1000, 1000);

			var pidStr = pid.HasValue
				? pid.Value.ToString(CultureInfo.InvariantCulture)
				: "self";
			logger.LogTrace(
				"Setting oom_score_adj of {pid} to {adjustment}...", pidStr, adjustedValue);
			return ioManager.WriteAllBytes(
				$"/proc/{pidStr}/oom_score_adj",
				Encoding.UTF8.GetBytes(adjustedValue.ToString(CultureInfo.InvariantCulture)),
				cancellationToken);
		}
	}
}
