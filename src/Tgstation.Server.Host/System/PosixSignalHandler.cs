using System;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Mono.Unix;
using Mono.Unix.Native;

using Tgstation.Server.Helpers;
using Tgstation.Server.Host.Core;

namespace Tgstation.Server.Host.System
{
	/// <summary>
	/// Handles POSIX signals.
	/// </summary>
	[UnsupportedOSPlatform("windows")]
	sealed class PosixSignalHandler : IHostedService, IAsyncDisposable
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
		/// The thread used to check the signal. See http://docs.go-mono.com/?link=T%3aMono.Unix.UnixSignal.
		/// </summary>
		CancellableTask? signalCheckerTask;

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
		}

		/// <inheritdoc />
		public async ValueTask DisposeAsync()
		{
			if (signalCheckerTask != null)
				await signalCheckerTask.DisposeAsync().ConfigureAwait(false);
		}

		/// <inheritdoc />
		public Task StartAsync(CancellationToken cancellationToken)
		{
			if (signalCheckerTask != null)
				throw new InvalidOperationException("Attempted to start PosixSignalHandler twice!");

			signalCheckerTask = new CancellableTask(token => SignalChecker(token));

			return Task.CompletedTask;
		}

		/// <inheritdoc />
		public async Task StopAsync(CancellationToken cancellationToken)
		{
			if (signalCheckerTask?.Task.IsCompleted != false)
				return;

			logger.LogTrace("Joining SignalCheckerThread...");
			await signalCheckerTask.DisposeAsync().ConfigureAwait(false);
		}

		/// <summary>
		/// Thread for listening to signal.
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task"/> representing the running operation.</returns>
		async Task SignalChecker(CancellationToken cancellationToken)
		{
			try
			{
				logger.LogTrace("Started SignalChecker");

				using var unixSignal = new UnixSignal(Signum.SIGUSR1);
				if (!unixSignal.IsSet)
				{
					logger.LogTrace("Waiting for SIGUSR1...");
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
