using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Mono.Unix;
using Mono.Unix.Native;

using Tgstation.Server.Host.Core;

namespace Tgstation.Server.Host.System
{
	/// <summary>
	/// Handles POSIX signals.
	/// </summary>
	sealed class PosixSignalHandler : IHostedService, IDisposable
	{
		/// <summary>
		/// Check for signals each time this amount of milliseconds pass.
		/// </summary>
		const int CheckDelayMs = 250;

		/// <summary>
		/// The <see cref="IServerControl"/> for the <see cref="PosixSignalHandler"/>.
		/// </summary>
		readonly IServerControl serverControl;

		/// <summary>
		/// The <see cref="IAsyncDelayer"/> for the <see cref="PosixSignalHandler"/>.
		/// </summary>
		readonly IAsyncDelayer asyncDelayer;

		/// <summary>
		/// The <see cref="ILogger"/> for the <see cref="PosixSignalHandler"/>.
		/// </summary>
		readonly ILogger<PosixSignalHandler> logger;

		/// <summary>
		/// The <see cref="CancellationTokenSource"/> used to stop the <see cref="signalCheckerTask"/>.
		/// </summary>
		readonly CancellationTokenSource cancellationTokenSource;

		/// <summary>
		/// The thread used to check the signal. See http://docs.go-mono.com/?link=T%3aMono.Unix.UnixSignal.
		/// </summary>
		Task signalCheckerTask;

		/// <summary>
		/// Initializes a new instance of the <see cref="PosixSignalHandler"/> class.
		/// </summary>
		/// <param name="serverControl">The value of <see cref="serverControl"/>.</param>
		/// <param name="asyncDelayer">The value of <see cref="asyncDelayer"/>.</param>
		/// <param name="logger">The value of <see cref="logger"/>.</param>
		public PosixSignalHandler(IServerControl serverControl, IAsyncDelayer asyncDelayer, ILogger<PosixSignalHandler> logger)
		{
			this.serverControl = serverControl ?? throw new ArgumentNullException(nameof(serverControl));
			this.asyncDelayer = asyncDelayer ?? throw new ArgumentNullException(nameof(asyncDelayer));
			this.logger = logger ?? throw new ArgumentNullException(nameof(logger));

			cancellationTokenSource = new CancellationTokenSource();
		}

		/// <inheritdoc />
		public void Dispose() => cancellationTokenSource.Dispose();

		/// <inheritdoc />
		public Task StartAsync(CancellationToken cancellationToken)
		{
			if (signalCheckerTask != null)
				throw new InvalidOperationException("Attempted to start PosixSignalHandler twice!");

			signalCheckerTask = SignalChecker();

			return Task.CompletedTask;
		}

		/// <inheritdoc />
		public async Task StopAsync(CancellationToken cancellationToken)
		{
			if (signalCheckerTask?.IsCompleted != false)
				return;

			logger.LogDebug("Stopping SignalCheckerThread...");
			cancellationTokenSource.Cancel();

			logger.LogTrace("Joining SignalCheckerThread...");
			await signalCheckerTask.ConfigureAwait(false);
		}

		/// <summary>
		/// Thread for listening to signal.
		/// </summary>
		/// <returns>A <see cref="Task"/> representing the running operation.</returns>
		async Task SignalChecker()
		{
			try
			{
				logger.LogTrace("Started SignalChecker");

				using var unixSignal = new UnixSignal(Signum.SIGUSR1);
				if (!unixSignal.IsSet)
				{
					logger.LogTrace("Waiting for SIGUSR1...");
					var cancellationToken = cancellationTokenSource.Token;
					while (!unixSignal.IsSet)
						await asyncDelayer.Delay(TimeSpan.FromMilliseconds(CheckDelayMs), cancellationToken);

					logger.LogTrace("SIGUSR1 received!");
				}
				else
					logger.LogDebug("SIGUSR1 has already been sent");

				logger.LogTrace("Triggering graceful shutdown...");
				await serverControl.GracefulShutdown().ConfigureAwait(false);
			}
			catch (OperationCanceledException ex)
			{
				logger.LogDebug(ex, "SignalChecker cancelled!");
			}
			catch (Exception ex)
			{
				logger.LogError(ex, "SignalChecker crashed!");
			}
			finally
			{
				logger.LogTrace("Exiting SignalChecker...");
			}
		}
	}
}
