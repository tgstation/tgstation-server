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
	sealed class PosixSignalHandler : IHostedService
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
		/// The thread used to check the signal. See http://docs.go-mono.com/?link=T%3aMono.Unix.UnixSignal.
		/// </summary>
		readonly Thread signalCheckerThread;

		/// <summary>
		/// Initializes a new instance of the <see cref="PosixSignalHandler"/> class.
		/// </summary>
		/// <param name="serverControl">The value of <see cref="serverControl"/>.</param>
		/// <param name="logger">The value of <see cref="logger"/>.</param>
		public PosixSignalHandler(IServerControl serverControl, ILogger<PosixSignalHandler> logger)
		{
			this.serverControl = serverControl ?? throw new ArgumentNullException(nameof(serverControl));
			this.logger = logger ?? throw new ArgumentNullException(nameof(logger));

			signalCheckerThread = new Thread(SignalCheckerThread);
		}

		/// <inheritdoc />
		public Task StartAsync(CancellationToken cancellationToken)
		{
			if (signalCheckerThread.ThreadState != ThreadState.Unstarted)
				throw new InvalidOperationException("Attempted to start PosixSignalHandler twice!");

			signalCheckerThread.Start();

			return Task.CompletedTask;
		}

		/// <inheritdoc />
		public Task StopAsync(CancellationToken cancellationToken)
		{
			if (signalCheckerThread.IsAlive)
			{
				logger.LogDebug("Interrupting SignalCheckerThread...");
				signalCheckerThread.Interrupt();
			}

			logger.LogTrace("Joining SignalCheckerThread...");
			signalCheckerThread.Join();

			return Task.CompletedTask;
		}

		/// <summary>
		/// Thread for listening to signal.
		/// </summary>
		void SignalCheckerThread()
		{
			try
			{
				logger.LogTrace("Started SignalCheckerThread");
				Thread.CurrentThread.Name = "Signal Handler Thread";

				using var unixSignal = new UnixSignal(Signum.SIGUSR1);
				if (unixSignal.Count == 0)
				{
					logger.LogTrace("Waiting for SIGUSR1...");
					unixSignal.WaitOne();
				}
				else
					logger.LogDebug("SIGUSR1 has already been sent");

				logger.LogTrace("Triggering graceful shutdown...");

				// Pains me to actually call .Wait() on a Task, but it's the nature of the blocking signal beast
				serverControl.GracefulShutdown().Wait();

				logger.LogTrace("Exiting SignalCheckerThread...");
			}
			catch (ThreadInterruptedException ex)
			{
				logger.LogDebug(ex, "SignalCheckerThread interrupt received!");
			}
			catch (Exception ex)
			{
				logger.LogError(ex, "SignalCheckerThread crashed!");
			}
		}
	}
}
