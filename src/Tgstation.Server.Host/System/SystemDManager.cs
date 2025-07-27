using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using Mono.Unix;

using Tgstation.Server.Host.Components;
using Tgstation.Server.Host.Core;

namespace Tgstation.Server.Host.System
{
	/// <summary>
	/// Implements the SystemD notify service protocol.
	/// </summary>
	sealed class SystemDManager : BackgroundService, IRestartHandler, IDisposable
	{
		/// <summary>
		/// The sd_notify command for notifying the watchdog we are alive.
		/// </summary>
		const string SDNotifyWatchdog = "WATCHDOG=1";

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
		/// If TGS is going to restart.
		/// </summary>
		bool restartInProgress;

		/// <summary>
		/// Get the current total nanoseconds value of the CLOCK_MONOTONIC clock.
		/// </summary>
		/// <returns>A <see cref="long"/> representing the clock time in nanoseconds.</returns>
		/// <remarks>See https://linux.die.net/man/3/clock_gettime.</remarks>
		static long GetMonotonicUsec() => global::System.Diagnostics.Stopwatch.GetTimestamp(); // HACK: https://github.com/dotnet/runtime/blob/v10.0.0-preview.6.25358.103/src/native/libs/System.Native/pal_time.c#L84 https://github.com/dotnet/runtime/blob/v10.0.0-preview.6.25358.103/src/native/minipal/time.c#L76

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
		}

		/// <inheritdoc />
		public override void Dispose()
		{
			base.Dispose();
			restartRegistration.Dispose();
		}

		/// <inheritdoc />
		public ValueTask HandleRestart(Version? updateVersion, bool handlerMayDelayShutdownWithExtremelyLongRunningTasks, CancellationToken cancellationToken)
		{
			// If this is set, we know a gracefule SHUTDOWN was requested
			restartInProgress = !handlerMayDelayShutdownWithExtremelyLongRunningTasks;
			return ValueTask.CompletedTask;
		}

		/// <inheritdoc />
		protected override async Task ExecuteAsync(CancellationToken cancellationToken)
		{
			if (!SendSDNotify(SDNotifyWatchdog))
			{
				logger.LogDebug("SystemD not detected");
				return;
			}

			logger.LogDebug("SystemD detected");

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
						? $"RELOADING=1\nMONOTONIC_USEC={GetMonotonicUsec()}"
						: "STOPPING=1"));

			try
			{
				await instanceManager.Ready.WaitAsync(cancellationToken);
				CheckReady();

				var watchdogUsec = Environment.GetEnvironmentVariable("WATCHDOG_USEC");
				if (String.IsNullOrWhiteSpace(watchdogUsec))
				{
					logger.LogDebug("WATCHDOG_USEC not present, not starting watchdog loop");
					return;
				}

				var microseconds = UInt64.Parse(watchdogUsec, CultureInfo.InvariantCulture);
				var timeoutIntervalMillis = (int)(microseconds / 1000);

				logger.LogDebug("Starting watchdog loop with interval of {timeoutInterval}ms", timeoutIntervalMillis);

				var timeoutInterval = TimeSpan.FromMilliseconds(timeoutIntervalMillis);
				var nextExpectedTimeout = DateTimeOffset.UtcNow + timeoutInterval;
				var timeToNextExpectedTimeout = nextExpectedTimeout - DateTimeOffset.UtcNow;
				while (!cancellationToken.IsCancellationRequested)
				{
					var delayInterval = timeToNextExpectedTimeout / 2;
					await Task.Delay(delayInterval, cancellationToken);

					var notifySuccess = SendSDNotify(SDNotifyWatchdog);

					var now = DateTimeOffset.UtcNow;
					if (notifySuccess)
						nextExpectedTimeout = now + timeoutInterval;

					timeToNextExpectedTimeout = nextExpectedTimeout - now;

					if (!notifySuccess)
						logger.LogWarning("Missed systemd heartbeat! Expected timeout in {timeoutMs}ms...", timeToNextExpectedTimeout.TotalMilliseconds);
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
			int result;
			try
			{
				result = NativeMethods.sd_notify(0, command);
			}
			catch (Exception ex)
			{
				logger.LogInformation(ex, "Exception attempting to invoke sd_notify!");
				return false;
			}

			if (result > 0)
				return true;

			if (result < 0)
				logger.LogError(new UnixIOException(result), "sd_notify {message} failed!", command);
			else
				logger.LogTrace("Could not send sd_notify {message}. Socket closed!", command);

			return false;
		}
	}
}
