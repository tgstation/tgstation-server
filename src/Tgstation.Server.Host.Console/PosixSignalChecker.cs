using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using Mono.Unix;
using Mono.Unix.Native;

using Tgstation.Server.Host.Watchdog;

namespace Tgstation.Server.Host.Console
{
	/// <summary>
	/// <see cref="ISignalChecker"/> for checking POSIX signals.
	/// </summary>
	sealed class PosixSignalChecker : ISignalChecker
	{
		/// <summary>
		/// The <see cref="ILogger"/> for the <see cref="PosixSignalChecker"/>.
		/// </summary>
		readonly ILogger<PosixSignalChecker> logger;

		/// <summary>
		/// Initializes a new instance of the <see cref="PosixSignalChecker"/> class.
		/// </summary>
		/// <param name="logger">The value of <see cref="logger"/>.</param>
		public PosixSignalChecker(ILogger<PosixSignalChecker> logger)
		{
			this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
		}

		/// <inheritdoc />
		public async ValueTask CheckSignals(Func<string, (int, Task)> startChild, CancellationToken cancellationToken)
		{
			var (childPid, _) = startChild?.Invoke(null) ?? throw new ArgumentNullException(nameof(startChild));
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
