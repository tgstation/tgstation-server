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

		async Task MonitorLifetimes(CancellationToken cancellationToken)
		{
			var watchReboot = false;
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

						var activeServerExited = (alphaServerTask.IsCompleted && AlphaIsActive) || (bravoServerTask.IsCompleted && !AlphaIsActive);
						var inactiveServerExited = (alphaServerTask.IsCompleted && !AlphaIsActive) || (bravoServerTask.IsCompleted && AlphaIsActive);
						var activeServerReboot = (alphaServerReboot.IsCompleted && AlphaIsActive) || (bravoServerReboot.IsCompleted && !AlphaIsActive);

						if (watchReboot)
						{
							//waiting on the inactive server to come back
							var doContinue = true;
							var activeServerGracefulAction = activeServer.RebootState != Components.Watchdog.RebootState.Normal;
							var aServerRebooted = alphaServerReboot.IsCompleted || bravoServerReboot.IsCompleted;
							if (inactiveServer.LaunchResult.IsCompleted)
							{
								//glad to have you back
								//(cleanup the task here)
								LastLaunchResult = await inactiveServer.LaunchResult.ConfigureAwait(false);
								if (inactiveServer.Lifetime.IsCompleted)
								{
									//aww heck you died again, try to bring you back i guess...
									//transfer control to the "inactive server died" branch
									inactiveServerExited = true;
									doContinue = false;
								}
								else
									//reenable port swapping on active server
									activeServer.ClosePortOnReboot = true;
							}
							else if (activeServerGracefulAction && activeServerExited)
							{
								//ok not so bad, we either kill everything or restart everything
								if (activeServer.RebootState == Components.Watchdog.RebootState.Shutdown)
									//ez
									//let control fall through to the "crash but wanted to shutdown anyway" section
									doContinue = false;
								else
								{
									//total restart, forget about trying to make the inactive server the active one since it's not even responding yet
									var chatTask = chat.SendWatchdogMessage("Entered state where restart is requested but neither server is ready. Restarting watchdog...", cancellationToken);
									await LaunchNoLock(false, cancellationToken).ConfigureAwait(false);
									await chatTask.ConfigureAwait(false);
									if (!Running)
										return;
								}
							}
							//if the above is NOT the case that means something probably happened to the ACTIVE server
							//hopefully it was just a reboot, otherwise we're gonna need to do some weird recovery shennanigans
							else if (!aServerRebooted)
							{
								//uh oh...
								if (!activeServerExited)
									//inactive server died again
									//let control fall through to the inactive crash handler
									doContinue = false;
								else
								{
									//OH NO
									//so now we have a dead active server and an inactive server who is still booting
									//best we can do is hope it comes online so we can fix things
									var chatTask = chat.SendWatchdogMessage(String.Format(CultureInfo.InvariantCulture, "Active server crashed or exited (Code: {0})! Inactive server is still booting! Waiting...", activeServer.Lifetime.Result), cancellationToken);

									LastLaunchResult = await inactiveServer.LaunchResult.ConfigureAwait(false);

									if(inactiveServer.Lifetime.IsCompleted)
										//he's dead too jim.
								}
							}
							else
							{
								//thank god
								//doesn't matter which server rebooted, our action is still the same:
								//if it was active, tell it to not switch port
								//if it was inactive, ensure it's port is the right one
								activeServer.ClosePortOnReboot = false;
								await inactiveServer.SetPort(ActiveLaunchParameters.SecondaryPort.Value, cancellationToken).ConfigureAwait(false);
							}

							if (doContinue)
								continue;
						}

						if (activeServerExited)
						{
							//active server just went down
							switch (activeServer.RebootState)
							{
								case Components.Watchdog.RebootState.Restart:
								case Components.Watchdog.RebootState.Normal:
									//yeah he died unexpectedly
									//bring in other
									var chatTask = chat.SendWatchdogMessage(String.Format(CultureInfo.InvariantCulture, "Active server crashed or exited (Code: {0})! Activating backup, relaunching in background...", activeServer.Lifetime.Result), cancellationToken);

									activeServer.Dispose();
									var backupOnline = await inactiveServer.SetPort(ActiveLaunchParameters.PrimaryPort.Value, cancellationToken).ConfigureAwait(false);
									if (!backupOnline)
									{
										//can't bring in backup, reboot everything
										await chatTask.ConfigureAwait(false);
										await chat.SendWatchdogMessage("Backup server not responding to port switch request! Restarting watchdog...", cancellationToken).ConfigureAwait(false);
										inactiveServer.Dispose();
										Running = false;
										await LaunchNoLock(false, cancellationToken).ConfigureAwait(false);
										if (!Running)
											return;
										continue;
									}

									//make a new server with blackjack and hookers
									var nextDmb = await dmbFactory.LockNextDmb(cancellationToken).ConfigureAwait(false);
									var newServer = await sessionControllerFactory.LaunchNew(ActiveLaunchParameters, nextDmb, false, AlphaIsActive, false, cancellationToken).ConfigureAwait(false);

									if (AlphaIsActive)
										alphaServer = newServer;
									else
										bravoServer = newServer;
									watchReboot = true;
									AlphaIsActive = !AlphaIsActive;
									inactiveServer.ClosePortOnReboot = false;	//actually the active server at this point
									break;
								case Components.Watchdog.RebootState.Shutdown:
									await chat.SendWatchdogMessage(String.Format(CultureInfo.InvariantCulture, "Active server crashed or exited (Code: {0})! Not recovering to to pending shutdown.", activeServer.Lifetime.Result), cancellationToken).ConfigureAwait(false);
									DisposeAndNullControllers();
									return;
							}
						}
						else if (inactiveServerExited)
						{
							//inactive server just went down
							//bring it back
							var chatTask = chat.SendWatchdogMessage(String.Format(CultureInfo.InvariantCulture, "Inactive server crashed or exited (Code: {0})! Relaunching in background...", inactiveServer.Lifetime.Result), cancellationToken);
							var nextDmb = await dmbFactory.LockNextDmb(cancellationToken).ConfigureAwait(false);
							var newServer = await sessionControllerFactory.LaunchNew(ActiveLaunchParameters, nextDmb, false, AlphaIsActive, false, cancellationToken).ConfigureAwait(false);

							if (!AlphaIsActive)
								alphaServer = newServer;
							else
								bravoServer = newServer;

							watchReboot = true;
							activeServer.ClosePortOnReboot = false;
						}
						else if (activeServerReboot)
						{
							//try 5 times to open the inactive server on the primary port
							//might fail a couple times due to the active server in the process of closing it's own
							var I = 0;
							for (; I < 5; ++I)
								if (await inactiveServer.SetPort(ActiveLaunchParameters.PrimaryPort.Value, cancellationToken).ConfigureAwait(false))
									break;

							if(I == 5)
							{
								//uggghhh he's being a bitch
								var chatTask = chat.SendWatchdogMessage("Unable to open inactive server port! Reopening active server port...", cancellationToken);
								var result = await activeServer.SetPort(ActiveLaunchParameters.PrimaryPort.Value, cancellationToken).ConfigureAwait(false);
								if (!result)
								{
									//ok reboot time
									await chatTask.ConfigureAwait(false);
									await chat.SendWatchdogMessage("Unable to reopen active server port! Restarting watchdog...", cancellationToken).ConfigureAwait(false);
									inactiveServer.Dispose();
									Running = false;
									await LaunchNoLock(false, cancellationToken).ConfigureAwait(false);
									if (!Running)
										return;
								}
								continue;
							}

							LastLaunchParameters = ActiveLaunchParameters;
							AlphaIsActive = !AlphaIsActive;
							LiveCompileJob = inactiveServer.Dmb.CompileJob;
							StagedCompileJob = null;
							this.
						}
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
