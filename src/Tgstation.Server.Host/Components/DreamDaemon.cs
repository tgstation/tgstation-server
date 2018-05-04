using System;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Api.Models;
using Tgstation.Server.Api.Models.Internal;

namespace Tgstation.Server.Host.Components
{
	/// <inheritdoc />
	sealed class DreamDaemon : IDreamDaemon, ILaunchParametersFactory, IDisposable
	{
		/// <inheritdoc />
		public bool Running { get; private set; }

		/// <inheritdoc />
		public ushort? CurrentPort { get; private set; }

		/// <inheritdoc />
		public string AccessToken { get; private set; }

		/// <inheritdoc />
		public DreamDaemonSecurity? CurrentSecurity { get; private set; }

		/// <inheritdoc />
		public bool SoftRebooting { get; private set; }

		/// <inheritdoc />
		public bool SoftStopping { get; private set; }

		/// <summary>
		/// The <see cref="IEventConsumer"/> for <see cref="DreamDaemon"/>
		/// </summary>
		readonly IEventConsumer eventConsumer;
		/// <summary>
		/// The <see cref="IInterop"/> for <see cref="DreamDaemon"/>
		/// </summary>
		readonly IInterop interop;
		/// <summary>
		/// The <see cref="IWatchdog"/> for <see cref="DreamDaemon"/>
		/// </summary>
		readonly IWatchdog watchdog;

		/// <summary>
		/// Used for write control to class variables
		/// </summary>
		readonly SemaphoreSlim semaphore;

		/// <summary>
		/// If <see cref="StartAsync(CancellationToken)"/> should start DD
		/// </summary>
		readonly bool autoStart;

		/// <summary>
		/// The current <see cref="DreamDaemonLaunchParameters"/>
		/// </summary>
		DreamDaemonLaunchParameters currentLaunchParameters;

		/// <summary>
		/// Construct <see cref="DreamDaemon"/>
		/// </summary>
		/// <param name="eventConsumer">The value of <see cref="eventConsumer"/></param>
		/// <param name="interop">The value of <see cref="interop"/></param>
		/// <param name="watchdog">The value of <see cref="watchdog"/></param>
		/// <param name="initialSettings">The initial value of <see cref="currentLaunchParameters"/> and <see cref="autoStart"/></param>
		public DreamDaemon(IEventConsumer eventConsumer, IInterop interop, IWatchdog watchdog, DreamDaemonSettings initialSettings)
		{
			this.eventConsumer = eventConsumer ?? throw new ArgumentNullException(nameof(eventConsumer));
			this.interop = interop ?? throw new ArgumentNullException(nameof(interop));
			this.watchdog = watchdog ?? throw new ArgumentNullException(nameof(watchdog));
			currentLaunchParameters = initialSettings ?? throw new ArgumentNullException(nameof(initialSettings));
			
			autoStart = initialSettings.AutoStart;

			semaphore = new SemaphoreSlim(1);
		}

		/// <inheritdoc />
		public void Dispose() => semaphore.Dispose();

		/// <inheritdoc />
		public async Task CancelGracefulActions(CancellationToken cancellationToken)
		{
			await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
			try
			{
				SoftRebooting = false;
				SoftStopping = false;
			}
			finally
			{
				semaphore.Release();
			}
		}

		/// <inheritdoc />
		public async Task ChangeSettings(DreamDaemonLaunchParameters launchParameters, CancellationToken cancellationToken)
		{
			if (launchParameters == null)
				throw new ArgumentNullException(nameof(launchParameters));
			Task launchTask;
			await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
			try
			{
				currentLaunchParameters = launchParameters;
				if (!Running)
					launchTask = Launch(launchParameters, cancellationToken);
				else
					launchTask = Restart(true, cancellationToken);
			}
			finally
			{
				semaphore.Release();
			}
			await launchTask.ConfigureAwait(false);
		}

		/// <inheritdoc />
		public async Task Launch(DreamDaemonLaunchParameters launchParameters, CancellationToken cancellationToken)
		{
			if (launchParameters == null)
				throw new ArgumentNullException(nameof(launchParameters));
			await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
			Task watchdogStartup;
			try
			{
				if (Running)
					throw new InvalidOperationException("DreamDaemon already running!");
				Running = true;
				await eventConsumer.HandleEvent(EventType.DDLaunched, null, cancellationToken).ConfigureAwait(false);
				watchdogStartup = watchdog.Start(this, cancellationToken);
			}
			finally
			{
				semaphore.Release();
			}
			//important to leave the lock so the watchdog can enter it
			await watchdogStartup.ConfigureAwait(false);
		}

		/// <inheritdoc />
		public async Task Restart(bool graceful, CancellationToken cancellationToken)
		{
			await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
			try
			{
				if (graceful)
				{
					if (!Running)
						throw new InvalidOperationException("DreamDaemon not running!");
					if (SoftStopping)
						throw new InvalidOperationException("DreamDaemon has a graceful stop queued!");
					SoftRebooting = true;
					return;
				}
				await eventConsumer.HandleEvent(EventType.DDRestart, null, cancellationToken).ConfigureAwait(false);
				if (Running)
					await watchdog.Stop().ConfigureAwait(false);
				await Launch(currentLaunchParameters, cancellationToken).ConfigureAwait(false);
			}
			finally
			{
				semaphore.Release();
			}
		}

		/// <inheritdoc />
		public Task StartAsync(CancellationToken cancellationToken) => autoStart ? Launch(currentLaunchParameters, cancellationToken) : Task.CompletedTask;

		/// <inheritdoc />
		public Task StopAsync(CancellationToken cancellationToken) => Terminate(false, cancellationToken);

		/// <inheritdoc />
		public async Task Terminate(bool graceful, CancellationToken cancellationToken)
		{
			await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
			try
			{
				if (graceful)
				{
					if (!Running)
						throw new InvalidOperationException("DreamDaemon not running!");
					SoftRebooting = false;
					SoftStopping = true;
					return;
				}
				await eventConsumer.HandleEvent(EventType.DDTerminated, null, cancellationToken).ConfigureAwait(false);
				await watchdog.Stop().ConfigureAwait(false);
			}
			finally
			{
				semaphore.Release();
			}
		}

		/// <inheritdoc />
		public async Task<DreamDaemonLaunchParameters> GetLaunchParameters(CancellationToken cancellationToken)
		{
			await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
			try
			{
				return currentLaunchParameters;
			}
			finally
			{
				semaphore.Release();
			}
		}
	}
}
