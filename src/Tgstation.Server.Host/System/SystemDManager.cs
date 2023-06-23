using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using Mono.Unix;

using Tgstation.Server.Host.Components;
using Tgstation.Server.Host.Core;
using Tgstation.Server.Host.Extensions;

namespace Tgstation.Server.Host.System
{
	/// <summary>
	/// Implements the SystemD notify service protocol.
	/// </summary>
	sealed class SystemDManager : IHostedService, IRestartHandler, IDisposable
	{
		/// <summary>
		/// The <see cref="IHostApplicationLifetime"/> for the <see cref="SystemDManager"/>.
		/// </summary>
		readonly IHostApplicationLifetime applicationLifetime;

		/// <summary>
		/// The <see cref="IInstanceManager"/> for the <see cref="SystemDManager"/>.
		/// </summary>
		readonly IInstanceManager instanceManager;

		/// <summary>
		/// The <see cref="IRestartRegistration"/> for the <see cref="SystemDManager"/>.
		/// </summary>
		readonly IRestartRegistration restartRegistration;

		/// <summary>
		/// The <see cref="ILogger"/> for the <see cref="SystemDManager"/>.
		/// </summary>
		readonly ILogger<SystemDManager> logger;

		/// <summary>
		/// The <see cref="CancellationTokenSource"/> for <see cref="runTask"/>.
		/// </summary>
		readonly CancellationTokenSource watchdogCts;

		/// <summary>
		/// The main task executing in the <see cref="SystemDManager"/>.
		/// </summary>
		Task runTask;

		/// <summary>
		/// If TGS is going to restart.
		/// </summary>
		bool restartInProgress;

		/// <summary>
		/// Initializes a new instance of the <see cref="SystemDManager"/> class.
		/// </summary>
		/// <param name="applicationLifetime">The value of <see cref="applicationLifetime"/>.</param>
		/// <param name="instanceManager">The value of <see cref="instanceManager"/>.</param>
		/// <param name="serverControl">The <see cref="IServerControl"/> used to create the <see cref="restartRegistration"/>.</param>
		/// <param name="logger">The value of <see cref="ILogger"/>.</param>
		public SystemDManager(
			IHostApplicationLifetime applicationLifetime,
			IInstanceManager instanceManager,
			IServerControl serverControl,
			ILogger<SystemDManager> logger)
		{
			this.applicationLifetime = applicationLifetime ?? throw new ArgumentNullException(nameof(applicationLifetime));
			this.instanceManager = instanceManager ?? throw new ArgumentNullException(nameof(instanceManager));

			ArgumentNullException.ThrowIfNull(serverControl);

			this.logger = logger ?? throw new ArgumentNullException(nameof(logger));

			restartRegistration = serverControl.RegisterForRestart(this);
			try
			{
				watchdogCts = new CancellationTokenSource();
			}
			catch
			{
				restartRegistration.Dispose();
				throw;
			}
		}

		/// <inheritdoc />
		public void Dispose()
		{
			restartRegistration.Dispose();
			watchdogCts.Dispose();
		}

		/// <inheritdoc />
		public Task HandleRestart(Version updateVersion, bool handlerMayDelayShutdownWithExtremelyLongRunningTasks, CancellationToken cancellationToken)
		{
			// If this is set, we know a gracefule SHUTDOWN was requested
			restartInProgress = !handlerMayDelayShutdownWithExtremelyLongRunningTasks;
			return Task.CompletedTask;
		}

		/// <inheritdoc />
		public Task StartAsync(CancellationToken cancellationToken)
		{
			if (SendSDNotify("WATCHDOG=1"))
			{
				logger.LogDebug("SystemD detected");
				runTask = RunAsync(watchdogCts.Token);
			}
			else
			{
				logger.LogDebug("SystemD not detected");
				runTask = Task.CompletedTask;
			}

			return Task.CompletedTask;
		}

		/// <inheritdoc />
		public async Task StopAsync(CancellationToken cancellationToken)
		{
			watchdogCts.Cancel();
			await runTask.WithToken(cancellationToken);
		}

		/// <summary>
		/// Runs the <see cref="SystemDManager"/>.
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task"/> representing the running operation.</returns>
		async Task RunAsync(CancellationToken cancellationToken)
		{
			if (applicationLifetime.ApplicationStarted.IsCancellationRequested)
				throw new InvalidOperationException("RunAsync called after application started!");

			logger.LogTrace("Installing lifetime handlers...");

			var readyCounts = 0;
			void CheckReady()
			{
				if (Interlocked.Increment(ref readyCounts) < 2)
					return;

				SendSDNotify("READY=1");
			}

			applicationLifetime.ApplicationStarted.Register(() => CheckReady());
			applicationLifetime.ApplicationStopping.Register(
				() => SendSDNotify(
					restartInProgress
						? "RELOADING=1"
						: "STOPPING=1"));

			try
			{
				await instanceManager.Ready.WithToken(cancellationToken);
				CheckReady();

				var watchdogUsec = Environment.GetEnvironmentVariable("WATCHDOG_USEC");
				if (String.IsNullOrWhiteSpace(watchdogUsec))
				{
					logger.LogDebug("WATCHDOG_USEC not present, not starting watchdog loop");
					return;
				}

				logger.LogDebug("Starting watchdog loop with interval of {usec}us", watchdogUsec);

				var microseconds = UInt64.Parse(watchdogUsec, CultureInfo.InvariantCulture);
				var milliseconds = (int)(microseconds / 1000);
				if (milliseconds == 0)
					milliseconds = 1;

				while (!cancellationToken.IsCancellationRequested)
				{
					await Task.Delay(milliseconds, cancellationToken);

					logger.LogTrace("Sending sd_notify WATCHDOG=1");
					var result = NativeMethods.sd_notify(0, "WATCHDOG=1");
					if (result <= 0)
						logger.LogError(new UnixIOException(result), "sd_notify READY=1 failed!");
				}
			}
			catch (OperationCanceledException ex)
			{
				logger.LogTrace(ex, "Watchdog loop cancelled!");
			}
			catch (Exception ex)
			{
				logger.LogError(ex, "Watchdog loop crashed!");
			}

			logger.LogDebug("Exited watchdog loop");
		}

		/// <summary>
		/// Send a sd_notify <paramref name="command"/>.
		/// </summary>
		/// <param name="command">The <see cref="string"/> to send via sd_notify.</param>
		/// <returns><see langword="true"/> if the command succeeded, <see langword="false"/> otherwise.</returns>
		bool SendSDNotify(string command)
		{
			logger.LogTrace("Sending sd_notify {message}...", command);
			var result = NativeMethods.sd_notify(0, command);
			if (result > 0)
				return true;

			if (result < 0)
				logger.LogError(new UnixIOException(result), "sd_notify READY=1 failed!");
			else
				logger.LogTrace("Could not send sd_notify {message}. Socket closed!", command);

			return false;
		}
	}
}
