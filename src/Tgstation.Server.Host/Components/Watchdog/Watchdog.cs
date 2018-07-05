using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Api.Models.Internal;
using Tgstation.Server.Host.Core;
using Tgstation.Server.Host.Models;

namespace Tgstation.Server.Host.Components.Watchdog
{
	/// <inheritdoc />
	sealed class Watchdog : IWatchdog
	{
		/// <inheritdoc />
		public bool Running { get; private set; }

		/// <inheritdoc />
		public bool AlphaIsActive { get; private set; }

		/// <inheritdoc />
		public LaunchResult LastLaunchResult { get; private set; }

		/// <inheritdoc />
		public Models.CompileJob LiveCompileJob { get; private set; }

		/// <inheritdoc />
		public Models.CompileJob StagedCompileJob { get; private set; }

		/// <inheritdoc />
		public DreamDaemonLaunchParameters ActiveLaunchParameters { get; private set; }

		/// <inheritdoc />
		public DreamDaemonLaunchParameters LastLaunchParameters { get; private set; }

		/// <inheritdoc />
		public RebootState? RebootState => Running ? (RebootState?)(AlphaIsActive ? alphaServer.RebootState : bravoServer.RebootState) : null;

		/// <summary>
		/// The <see cref="IChat"/> for the <see cref="Watchdog"/>
		/// </summary>
		readonly IChat chat;

		/// <summary>
		/// The <see cref="ISessionControllerFactory"/> for the <see cref="Watchdog"/>
		/// </summary>
		readonly ISessionControllerFactory sessionControllerFactory;

		/// <summary>
		/// The <see cref="IEventConsumer"/> for the <see cref="Watchdog"/>
		/// </summary>
		readonly IEventConsumer eventConsumer;

		/// <summary>
		/// The <see cref="IInteropRegistrar"/> for the <see cref="Watchdog"/>
		/// </summary>
		readonly IInteropRegistrar interopRegistrar;

		/// <summary>
		/// The <see cref="IDmbFactory"/> for the <see cref="Watchdog"/>
		/// </summary>
		readonly IDmbFactory dmbFactory;

		/// <summary>
		/// The <see cref="SemaphoreSlim"/> for the <see cref="Watchdog"/>
		/// </summary>
		readonly SemaphoreSlim semaphore;

		CancellationTokenSource monitorCts;
		Task monitorTask;

		ISessionController alphaServer;
		ISessionController bravoServer;

		/// <summary>
		/// If the servers should be released instead of shutdown
		/// </summary>
		bool releaseServers;

		/// <summary>
		/// Construct a <see cref="IWatchdog"/>
		/// </summary>
		/// <param name="chat">The value of <see cref="chat"/></param>
		/// <param name="sessionControllerFactory">The value of <see cref="sessionControllerFactory"/></param>
		/// <param name="dmbFactory">The value of <see cref="dmbFactory"/></param>
		/// <param name="eventConsumer">The value of <see cref="eventConsumer"/></param>
		/// <param name="interopRegistrar">The value of <see cref="interopRegistrar"/></param>
		/// <param name="serverUpdater">The <see cref="IServerUpdater"/> for the <see cref="Watchdog"/></param>
		/// <param name="initialLaunchParameters">The initial value of <see cref="ActiveLaunchParameters"/></param>
		public Watchdog(IChat chat, ISessionControllerFactory sessionControllerFactory, IDmbFactory dmbFactory, IEventConsumer eventConsumer, IInteropRegistrar interopRegistrar, IServerUpdater serverUpdater, DreamDaemonLaunchParameters initialLaunchParameters)
		{
			this.chat = chat ?? throw new ArgumentNullException(nameof(chat));
			this.sessionControllerFactory = sessionControllerFactory ?? throw new ArgumentNullException(nameof(sessionControllerFactory));
			this.dmbFactory = dmbFactory ?? throw new ArgumentNullException(nameof(dmbFactory));
			this.eventConsumer = eventConsumer ?? throw new ArgumentNullException(nameof(eventConsumer));
			this.interopRegistrar = interopRegistrar ?? throw new ArgumentNullException(nameof(interopRegistrar));

			if(serverUpdater == null)
				throw new ArgumentNullException(nameof(serverUpdater));

			serverUpdater.RegisterForUpdate(() => releaseServers = true);

			AlphaIsActive = true;
			ActiveLaunchParameters = initialLaunchParameters;
			releaseServers = false;
			semaphore = new SemaphoreSlim(1);
		}

		/// <inheritdoc />
		public void Dispose()
		{
			DisposeAndNullControllers();
			semaphore.Dispose();
		}


		void DisposeAndNullControllers()
		{
			alphaServer?.Dispose();
			alphaServer = null;
			bravoServer?.Dispose();
			bravoServer = null;
		}

		async Task<WatchdogLaunchResult> RestartNoLock(bool graceful, CancellationToken cancellationToken)
		{
			var running = Running;
			if (!graceful || !running)
			{
				if (running)
					await Terminate(false, cancellationToken).ConfigureAwait(false);
				return await Launch(cancellationToken).ConfigureAwait(false);
			}
			var toReboot = AlphaIsActive ? alphaServer : bravoServer;
			var other = AlphaIsActive ? bravoServer : alphaServer;
			if (toReboot != null)
				//todo, log the result
				await toReboot.SetRebootState(Components.Watchdog.RebootState.Restart, cancellationToken).ConfigureAwait(false);
			return null;
		}

		async Task HandlerMonitorWakeup(MonitorActivationReason activationReason, MonitorState monitorState)
		{

		}

		async Task MonitorLifetimes(CancellationToken cancellationToken)
		{
			var state = new MonitorState();
			while(true)
			{
				try
				{
					var alphaServerTask = alphaServer.Lifetime;
					var bravoServerTask = bravoServer.Lifetime;
					var alphaServerReboot = alphaServer.OnReboot;
					var bravoServerReboot = bravoServer.OnReboot;

					var activeServer = AlphaIsActive ? alphaServer : bravoServer;
					var inactiveServer = AlphaIsActive ? bravoServer : alphaServer;

					var cancelTcs = new TaskCompletionSource<object>();
					using (cancellationToken.Register(() => cancelTcs.SetCanceled()))
					{
						var toWaitOn = Task.WhenAny(alphaServerTask, bravoServerTask, alphaServerReboot, bravoServerReboot, cancelTcs.Task);
						if (!watchReboot)
							toWaitOn = Task.WhenAny(toWaitOn, inactiveServer.LaunchResult);
						await toWaitOn.ConfigureAwait(false);
					}

					using (await SemaphoreContext.Lock(semaphore, default).ConfigureAwait(false))
					{
						state.ActiveServer = AlphaIsActive ? alphaServer : bravoServer;
						state.InactiveServer = !AlphaIsActive ? alphaServer : bravoServer;
						MonitorActivationReason activationReason;
						if (activeServer.Lifetime.IsCompleted)
							activationReason = MonitorActivationReason.ActiveServerCrashed;
						else if(inactiveServer.Lifetime.IsCompleted)
					}
				}
				catch (OperationCanceledException) { }
				catch (Exception e)
				{
					await chat.SendWatchdogMessage(String.Format(CultureInfo.InvariantCulture, "Monitor crashed, this should NEVER happen! Restarting monitor... Error: {0}", e.Message), cancellationToken).ConfigureAwait(false);
				}
			}
		}

		/// <inheritdoc />
		public async Task ChangeSettings(DreamDaemonLaunchParameters launchParameters, CancellationToken cancellationToken)
		{
			using (await SemaphoreContext.Lock(semaphore, cancellationToken).ConfigureAwait(false))
			{
				ActiveLaunchParameters = launchParameters;
				if (Running)
					await RestartNoLock(true, cancellationToken).ConfigureAwait(false);
			}
		}

		async Task StopMonitor()
		{
			if (monitorTask == null)
				return;
			monitorCts.Cancel();
			await monitorTask.ConfigureAwait(false);
			monitorCts.Dispose();
			monitorTask = null;
		}

		async Task<WatchdogLaunchResult> LaunchNoLock(bool startMonitor, CancellationToken cancellationToken)
		{
			using (var alphaStartCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
			{
				if (Running)
					return null;
				//start both servers
				LastLaunchParameters = ActiveLaunchParameters;
				var dmbToUse = await dmbFactory.LockNextDmb(cancellationToken).ConfigureAwait(false);
				Task<ISessionController> alphaServerTask = null;

				try
				{
					try
					{
						alphaServerTask = sessionControllerFactory.LaunchNew(ActiveLaunchParameters, dmbToUse, true, true, false, alphaStartCts.Token);
						//do a few seconds of delay so that any backends the servers use know that alpha came first
						await Task.Delay(5000, cancellationToken).ConfigureAwait(false);
						var bravoServerTask = sessionControllerFactory.LaunchNew(ActiveLaunchParameters, dmbToUse, false, false, false, cancellationToken);
						bravoServer = await bravoServerTask.ConfigureAwait(false);
						alphaServer = await alphaServerTask.ConfigureAwait(false);
					}
					catch
					{
						if (alphaServerTask != null)
							if (alphaServerTask.Status == TaskStatus.RanToCompletion)
								alphaServer = await alphaServerTask.ConfigureAwait(false);
							else
							{
								alphaStartCts.Cancel();
								try
								{
									alphaServer = await alphaServerTask.ConfigureAwait(false);
								}
								catch { }
							}
						throw;
					}

					async Task<LaunchResult> CheckLaunch(ISessionController controller, string serverName)
					{
						var launch = await controller.LaunchResult.ConfigureAwait(false);
						if (launch.ExitCode.HasValue)
							//you killed us ray...
							throw new Exception(String.Format(CultureInfo.InvariantCulture, "{2} server failed to start: Exit Code: {0}, RAM: {1}, Runtime {3}ms", launch.ExitCode, launch.StartupTime, serverName, launch.StartupTime.TotalMilliseconds));
						return launch;
					}

					var alphaLrt = CheckLaunch(alphaServer, "Alpha");
					var bravoLrt = CheckLaunch(bravoServer, "Bravo");
					//now we have two booting servers, get them up and running
					var allTask = Task.WhenAll(alphaLrt, bravoLrt);

					//don't forget about the cancelationToken
					var cancelTcs = new TaskCompletionSource<object>();
					using (cancellationToken.Register(() => cancelTcs.SetCanceled()))
						await Task.WhenAny(allTask, cancelTcs.Task).ConfigureAwait(false);

					//both servers are now running, alpha is the active server, huzzah
					LiveCompileJob = dmbToUse.CompileJob;
					LastLaunchResult = alphaLrt.Result;
					StagedCompileJob = null;
					AlphaIsActive = true;
					Running = true;

					if (startMonitor)
					{
						await StopMonitor().ConfigureAwait(false);
						monitorCts = new CancellationTokenSource();
						monitorTask = MonitorLifetimes(monitorCts.Token);
					}

					return new WatchdogLaunchResult
					{
						Alpha = alphaLrt.Result,
						Bravo = bravoLrt.Result
					};
				}
				catch
				{
					DisposeAndNullControllers();
					throw;
				}
			}
		}

		/// <inheritdoc />
		public async Task<WatchdogLaunchResult> Launch(CancellationToken cancellationToken)
		{
			using (await SemaphoreContext.Lock(semaphore, cancellationToken).ConfigureAwait(false))
				return await LaunchNoLock(true, cancellationToken).ConfigureAwait(false);
		}

		/// <inheritdoc />
		public async Task<WatchdogLaunchResult> Restart(bool graceful, CancellationToken cancellationToken)
		{
			using (await SemaphoreContext.Lock(semaphore, cancellationToken).ConfigureAwait(false))
				return await RestartNoLock(graceful, cancellationToken).ConfigureAwait(false);
		}

		/// <inheritdoc />
		public async Task Terminate(bool graceful, CancellationToken cancellationToken)
		{
			using (await SemaphoreContext.Lock(semaphore, cancellationToken).ConfigureAwait(false))
			{
				if (!Running)
					return;
				await StopMonitor().ConfigureAwait(false);
				if (!graceful)
				{
					DisposeAndNullControllers();
					return;
				}
				var toKill = AlphaIsActive ? alphaServer : bravoServer;
				var other = AlphaIsActive ? bravoServer : alphaServer;
				if (toKill != null)
					await toKill.SetRebootState(Components.Watchdog.RebootState.Shutdown, cancellationToken).ConfigureAwait(false);
			}
		}

		/// <inheritdoc />
		public Task StartAsync(CancellationToken cancellationToken) => Launch(cancellationToken);

		/// <inheritdoc />
		public Task StopAsync(CancellationToken cancellationToken)
		{
			if (releaseServers)
			{
				ReattachInformation reattachInformation;
				if (AlphaIsActive)
					reattachInformation = alphaServer?.Release();
				else
					reattachInformation = bravoServer?.Release();
			}
			return Terminate(false, cancellationToken);
		}
	}
}
