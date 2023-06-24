using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using Mono.Unix;
using Mono.Unix.Native;

namespace Tgstation.Server.Host.Watchdog
{
	/// <summary>
	/// Helper for checking POSIX signals.
	/// </summary>
	static class SignalChecker
	{
		/// <summary>
		/// Forwards certain signals to a given <paramref name="childPid"/>.
		/// </summary>
		/// <param name="logger">The <see cref="ILogger"/> to write to.</param>
		/// <param name="childPid">The <see cref="System.Diagnostics.Process.Id"/> of the process to forward signals to.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task"/> representing the running operation.</returns>
		public static async Task CheckSignals(ILogger logger, int childPid, CancellationToken cancellationToken)
		{
			var signalTcs = new TaskCompletionSource<Signum>();
			async Task<Signum?> CheckSignal(Signum signum)
			{
				try
				{
					using var unixSignal = new UnixSignal(signum);
					if (!unixSignal.IsSet)
					{
						logger.LogTrace("Waiting for {signum}...", signum);
						while (!unixSignal.IsSet)
							await Task.Delay(TimeSpan.FromMilliseconds(250), cancellationToken);

						logger.LogTrace("{signum} received!", signum);
					}
					else
						logger.LogDebug("{signum} has already been sent", signum);

					signalTcs.TrySetResult(signum);
				}
				catch (OperationCanceledException)
				{
				}

				return signum;
			}

			var tasks = new[]
			{
				CheckSignal(Signum.SIGUSR1),
				CheckSignal(Signum.SIGUSR2),
			};
			var completedTask = await Task.WhenAny(tasks);
			if (cancellationToken.IsCancellationRequested)
			{
				await Task.WhenAll(tasks);
				return;
			}

			var signalReceived = await completedTask;
			logger.LogInformation("Received {signalReceived}, forwarding to main TGS process!", signalReceived);
			var result = Syscall.kill(childPid, signalReceived.Value);
			if (result != 0)
				logger.LogWarning(
					new UnixIOException(Stdlib.GetLastError()),
					"Failed to forward {signalReceived}!",
					signalReceived);

			// forward the other signal if necessary
			await Task.WhenAll(tasks);
			if (cancellationToken.IsCancellationRequested)
				return;

			var otherTask = tasks[0] == completedTask
				? tasks[1]
				: tasks[0];

			signalReceived = await otherTask;
			logger.LogInformation("Received {signalReceived}, forwarding to main TGS process!", signalReceived);
			result = Syscall.kill(childPid, signalReceived.Value);
			if (result != 0)
				logger.LogWarning(
					new UnixIOException(Stdlib.GetLastError()),
					"Failed to forward {signalReceived}!",
					signalReceived);
		}
	}
}
