using System;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Api.Models;
using Tgstation.Server.Api.Models.Internal;
using Tgstation.Server.Host.Security;

namespace Tgstation.Server.Host.Components
{
	/// <inheritdoc />
	sealed class DreamDaemon : IDreamDaemon, IDisposable
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
		/// The <see cref="IByond"/> for <see cref="DreamDaemon"/>
		/// </summary>
		readonly IByond byond;
		/// <summary>
		/// The <see cref="ICryptographySuite"/> for <see cref="DreamDaemon"/>
		/// </summary>
		readonly ICryptographySuite cryptographySuite;
		/// <summary>
		/// The <see cref="IInterop"/> for <see cref="DreamDaemon"/>
		/// </summary>
		readonly IInterop interop;
		/// <summary>
		/// The <see cref="IInstanceShutdownMethod"/> for <see cref="DreamDaemon"/>
		/// </summary>
		readonly IInstanceShutdownMethod instanceShutdownMethod;
		/// <summary>
		/// The <see cref="IDreamDaemonExecutor"/> for <see cref="DreamDaemon"/>
		/// </summary>
		readonly IDreamDaemonExecutor dreamDaemonExecutor;

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
		/// The <see cref="CancellationTokenSource"/> for <see cref="watchdogTask"/>
		/// </summary>
		CancellationTokenSource watchdogCancellationTokenSource;
		/// <summary>
		/// The monitor for the DD process
		/// </summary>
		Task watchdogTask;

		/// <summary>
		/// Construct <see cref="DreamDaemon"/>
		/// </summary>
		/// <param name="eventConsumer">The value of <see cref="eventConsumer"/></param>
		/// <param name="byond">The value of <see cref="byond"/></param>
		/// <param name="cryptographySuite">The value of <see cref="cryptographySuite"/></param>
		/// <param name="interop">The value of <see cref="interop"/></param>
		/// <param name="instanceShutdownMethod">The value of <see cref="instanceShutdownMethod"/></param>
		/// <param name="dreamDaemonExecutor">The value of <see cref="dreamDaemonExecutor"/></param>
		/// <param name="initialSettings">The initial value of <see cref="currentLaunchParameters"/> and <see cref="autoStart"/></param>
		public DreamDaemon(IEventConsumer eventConsumer, IByond byond, ICryptographySuite cryptographySuite, IInterop interop, IInstanceShutdownMethod instanceShutdownMethod, IDreamDaemonExecutor dreamDaemonExecutor, DreamDaemonSettings initialSettings)
		{
			this.eventConsumer = eventConsumer ?? throw new ArgumentNullException(nameof(eventConsumer));
			this.byond = byond ?? throw new ArgumentNullException(nameof(byond));
			this.cryptographySuite = cryptographySuite ?? throw new ArgumentNullException(nameof(cryptographySuite));
			this.interop = interop ?? throw new ArgumentNullException(nameof(interop));
			this.instanceShutdownMethod = instanceShutdownMethod ?? throw new ArgumentNullException(nameof(instanceShutdownMethod));
			this.dreamDaemonExecutor = dreamDaemonExecutor ?? throw new ArgumentNullException(nameof(dreamDaemonExecutor));
			currentLaunchParameters = initialSettings ?? throw new ArgumentNullException(nameof(initialSettings));

			interop.SetServerControlHandler(OnServerControl);

			autoStart = initialSettings.AutoStart;

			semaphore = new SemaphoreSlim(1);
		}

		/// <inheritdoc />
		public void Dispose()
		{
			if (watchdogCancellationTokenSource != null)
				watchdogCancellationTokenSource.Dispose();
			semaphore.Dispose();
		}

		/// <summary>
		/// Handler for server control events
		/// </summary>
		/// <param name="serverControlEventArgs">The <see cref="ServerControlEventArgs"/></param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		async Task OnServerControl(ServerControlEventArgs serverControlEventArgs, CancellationToken cancellationToken)
		{
			await CancelGracefulActions(cancellationToken).ConfigureAwait(false);
			if (serverControlEventArgs.ProcessRestart)
				await Restart(serverControlEventArgs.Graceful, cancellationToken).ConfigureAwait(false);
			if (!serverControlEventArgs.ServerReboot)
				await Terminate(serverControlEventArgs.Graceful, cancellationToken).ConfigureAwait(false);
		}
		
		/// <summary>
		/// Main DD execution and monitoring <see cref="Task"/>
		/// </summary>
		/// <param name="launchParameters">The <see cref="DreamDaemonLaunchParameters"/> for the run</param>
		/// <param name="onSuccessfulStartup">The <see cref="TaskCompletionSource{TResult}"/> to be completed when the server initially starts</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		async Task Watchdog(DreamDaemonLaunchParameters launchParameters, TaskCompletionSource<object> onSuccessfulStartup, CancellationToken cancellationToken)
		{
			async Task RunOnce(string executablePath)
			{
				if (await byond.GetVersion(cancellationToken).ConfigureAwait(false) == null)
					throw new InvalidOperationException("No byond version installed!");

				await byond.ClearCache(cancellationToken).ConfigureAwait(false);

				string dmb = null;  //TODO
				var accessToken = cryptographySuite.GetSecureString();
				var usePrimaryPort = true;

				var ddTask = dreamDaemonExecutor.RunDreamDaemon(launchParameters, onSuccessfulStartup, executablePath, dmb, accessToken, usePrimaryPort, cancellationToken);

				await onSuccessfulStartup.Task.ConfigureAwait(false);

				interop.SetRun(usePrimaryPort ? launchParameters.PrimaryPort : launchParameters.SecondaryPort, accessToken);

				int ddExitCode;
				try
				{
					ddExitCode = await ddTask.ConfigureAwait(false);
				}
				finally
				{
					interop.SetRun(null, null);
				}

				await eventConsumer.HandleEvent(ddExitCode != 0 ? EventType.DDCrash : EventType.DDExit, null, cancellationToken).ConfigureAwait(false);
			};

			do
			{
				await byond.UseExecutable(RunOnce, false, true).ConfigureAwait(false);
			} while (!cancellationToken.IsCancellationRequested);
		}

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
			await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
			try
			{
				if (Running)
					throw new InvalidOperationException("DreamDaemon already running!");
				Running = true;
				await eventConsumer.HandleEvent(EventType.DDLaunched, null, cancellationToken).ConfigureAwait(false);
				watchdogCancellationTokenSource?.Dispose();
				watchdogCancellationTokenSource = new CancellationTokenSource();
				var startupTcs = new TaskCompletionSource<object>();
				watchdogTask = Watchdog(currentLaunchParameters, startupTcs, watchdogCancellationTokenSource.Token);
				await startupTcs.Task.ConfigureAwait(false);
			}
			finally
			{
				semaphore.Release();
			}
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
					watchdogCancellationTokenSource.Cancel();
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
				watchdogCancellationTokenSource.Cancel();
				await watchdogTask.ConfigureAwait(false);
			}
			finally
			{
				semaphore.Release();
			}
		}
	}
}
