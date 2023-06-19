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
		/// The <see cref="IServerControl"/> for the <see cref="PosixSignalHandler"/>.
		/// </summary>
		readonly IServerControl serverControl;

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
		/// <param name="logger">The value of <see cref="logger"/>.</param>
		public PosixSignalHandler(IServerControl serverControl, ILogger<PosixSignalHandler> logger)
		{
			this.serverControl = serverControl ?? throw new ArgumentNullException(nameof(serverControl));
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
			await signalCheckerTask;
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
				logger.LogTrace("Waiting for SIGUSR1...");
				var cancellationToken = cancellationTokenSource.Token;

				var tcs = new TaskCompletionSource();
				using (cancellationToken.Register(() => tcs.TrySetCanceled(cancellationToken)))
				{
					ThreadPool.RegisterWaitForSingleObject(
						unixSignal,
						(o, timeout) => tcs.TrySetResult(),
						null,
						Timeout.Infinite,
						true);

					await tcs.Task;
				}

				logger.LogTrace("SIGUSR1 received!");

				logger.LogTrace("Triggering graceful shutdown...");
				await serverControl.GracefulShutdown();
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
