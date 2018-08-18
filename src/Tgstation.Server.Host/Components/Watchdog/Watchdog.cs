using Byond.TopicSender;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Api.Models.Internal;
using Tgstation.Server.Host.Components.Chat;
using Tgstation.Server.Host.Components.Compiler;
using Tgstation.Server.Host.Components.Interop;
using Tgstation.Server.Host.Core;

namespace Tgstation.Server.Host.Components.Watchdog
{
	/// <inheritdoc />
	sealed class Watchdog : IWatchdog, ICustomCommandHandler
	{
		/// <summary>
		/// The time in seconds to wait from starting <see cref="alphaServer"/> to start <see cref="bravoServer"/>. Does not take responsiveness into account
		/// </summary>
		const int AlphaBravoStartupSeperationInterval = 3;

		/// <inheritdoc />
		public bool Running { get; private set; }

		/// <inheritdoc />
		public bool AlphaIsActive { get; private set; }

		/// <inheritdoc />
		public Models.CompileJob ActiveCompileJob => (AlphaIsActive ? alphaServer : bravoServer)?.Dmb.CompileJob;

		/// <inheritdoc />
		public LaunchResult LastLaunchResult { get; private set; }

		/// <inheritdoc />
		public DreamDaemonLaunchParameters ActiveLaunchParameters { get; private set; }

		/// <inheritdoc />
		public DreamDaemonLaunchParameters LastLaunchParameters { get; private set; }

		/// <inheritdoc />
		public RebootState? RebootState => Running ? (AlphaIsActive ? alphaServer?.RebootState : bravoServer?.RebootState) : null;

		/// <summary>
		/// The <see cref="IChat"/> for the <see cref="Watchdog"/>
		/// </summary>
		readonly IChat chat;

		/// <summary>
		/// The <see cref="ISessionControllerFactory"/> for the <see cref="Watchdog"/>
		/// </summary>
		readonly ISessionControllerFactory sessionControllerFactory;

		/// <summary>
		/// The <see cref="IDmbFactory"/> for the <see cref="Watchdog"/>
		/// </summary>
		readonly IDmbFactory dmbFactory;

		/// <summary>
		/// The <see cref="ILogger{TCategoryName}"/> for the <see cref="Watchdog"/>
		/// </summary>
		readonly ILogger<Watchdog> logger;

		/// <summary>
		/// The <see cref="IReattachInfoHandler"/> for the <see cref="Watchdog"/>
		/// </summary>
		readonly IReattachInfoHandler reattachInfoHandler;

		/// <summary>
		/// The <see cref="IDatabaseContextFactory"/> for the <see cref="Watchdog"/>
		/// </summary>
		readonly IDatabaseContextFactory databaseContextFactory;

		/// <summary>
		/// The <see cref="IByondTopicSender"/> for the <see cref="Watchdog"/>
		/// </summary>
		readonly IByondTopicSender byondTopicSender;

		/// <summary>
		/// The <see cref="IEventConsumer"/> for the <see cref="Watchdog"/>
		/// </summary>
		readonly IEventConsumer eventConsumer;

		/// <summary>
		/// The <see cref="SemaphoreSlim"/> for the <see cref="Watchdog"/>
		/// </summary>
		readonly SemaphoreSlim semaphore;

		/// <summary>
		/// The <see cref="Api.Models.Instance"/> for the <see cref="Watchdog"/>
		/// </summary>
		readonly Api.Models.Instance instance;

		/// <summary>
		/// If the <see cref="Watchdog"/> should <see cref="LaunchNoLock(bool, bool, bool, CancellationToken)"/> in <see cref="StartAsync(CancellationToken)"/>
		/// </summary>
		readonly bool autoStart;

		/// <summary>
		/// The <see cref="CancellationTokenSource"/> for the monitor loop
		/// </summary>
		CancellationTokenSource monitorCts;

		/// <summary>
		/// The <see cref="Task"/> running the monitor loop
		/// </summary>
		Task monitorTask;

		/// <summary>
		/// <see cref="TaskCompletionSource{TResult}"/> that completes when <see cref="ActiveLaunchParameters"/> are changed and we are <see cref="Running"/>
		/// </summary>
		TaskCompletionSource<object> activeParametersUpdated;

		/// <summary>
		/// Server designation alpha
		/// </summary>
		ISessionController alphaServer;
		/// <summary>
		/// Server designation bravo
		/// </summary>
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
		/// <param name="serverUpdater">The <see cref="IServerControl"/> for the <see cref="Watchdog"/></param>
		/// <param name="logger">The value of <see cref="logger"/></param>
		/// <param name="reattachInfoHandler">The value of <see cref="reattachInfoHandler"/></param>
		/// <param name="databaseContextFactory">The value of <see cref="databaseContextFactory"/></param>
		/// <param name="byondTopicSender">The value of <see cref="byondTopicSender"/></param>
		/// <param name="initialLaunchParameters">The initial value of <see cref="ActiveLaunchParameters"/></param>
		/// <param name="instance">The value of <see cref="instance"/></param>
		/// <param name="autoStart">The value of <see cref="autoStart"/></param>
		/// <param name="eventConsumer">The value of <see cref="eventConsumer"/></param>
		public Watchdog(IChat chat, ISessionControllerFactory sessionControllerFactory, IDmbFactory dmbFactory, IServerControl serverUpdater, ILogger<Watchdog> logger, IReattachInfoHandler reattachInfoHandler, IDatabaseContextFactory databaseContextFactory, IByondTopicSender byondTopicSender, IEventConsumer eventConsumer, DreamDaemonLaunchParameters initialLaunchParameters, Api.Models.Instance instance, bool autoStart)
		{
			this.chat = chat ?? throw new ArgumentNullException(nameof(chat));
			this.sessionControllerFactory = sessionControllerFactory ?? throw new ArgumentNullException(nameof(sessionControllerFactory));
			this.dmbFactory = dmbFactory ?? throw new ArgumentNullException(nameof(dmbFactory));
			this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
			this.reattachInfoHandler = reattachInfoHandler ?? throw new ArgumentNullException(nameof(reattachInfoHandler));
			this.databaseContextFactory = databaseContextFactory ?? throw new ArgumentNullException(nameof(databaseContextFactory));
			this.byondTopicSender = byondTopicSender ?? throw new ArgumentNullException(nameof(byondTopicSender));
			this.eventConsumer = eventConsumer ?? throw new ArgumentNullException(nameof(eventConsumer));
			this.instance = instance ?? throw new ArgumentNullException(nameof(instance));
			this.autoStart = autoStart;

			if (serverUpdater == null)
				throw new ArgumentNullException(nameof(serverUpdater));

			serverUpdater.RegisterForRestart(() => releaseServers = true);

			chat.RegisterCommandHandler(this);

			AlphaIsActive = true;
			ActiveLaunchParameters = initialLaunchParameters;
			releaseServers = false;
			semaphore = new SemaphoreSlim(1);
			activeParametersUpdated = new TaskCompletionSource<object>();
		}

		/// <inheritdoc />
		public void Dispose()
		{
			DisposeAndNullControllers();
			semaphore.Dispose();
		}

		/// <summary>
		/// Call <see cref="IDisposable.Dispose"/> on <see cref="alphaServer"/> and <see cref="bravoServer"/> and set them to <see langword="null"/>
		/// </summary>
		void DisposeAndNullControllers()
		{
			alphaServer?.Dispose();
			alphaServer = null;
			bravoServer?.Dispose();
			bravoServer = null;
			Running = false;
		}

		/// <summary>
		/// Implementation of <see cref="Restart(bool, CancellationToken)"/>. Does not lock <see cref="semaphore"/>
		/// </summary>
		async Task<WatchdogLaunchResult> RestartNoLock(bool graceful, CancellationToken cancellationToken)
		{
			var running = Running;
			if (!graceful || !running)
			{
				Task chatTask;
				if (running)
				{
					chatTask = chat.SendWatchdogMessage("Manual restart triggered...", cancellationToken);
					await TerminateNoLock(false, false, cancellationToken).ConfigureAwait(false);
				}
				else
					chatTask = Task.CompletedTask;
				var result = await LaunchNoLock(true, !running, false, cancellationToken).ConfigureAwait(false);
				await chatTask.ConfigureAwait(false);
				return result;
			}
			var toReboot = AlphaIsActive ? alphaServer : bravoServer;
			var other = AlphaIsActive ? bravoServer : alphaServer;
			if (toReboot != null)
				//todo, log the result
				await toReboot.SetRebootState(Components.Watchdog.RebootState.Restart, cancellationToken).ConfigureAwait(false);
			return null;
		}		

		/// <summary>
		/// Implementation of <see cref="Terminate(bool, CancellationToken)"/>. Does not lock <see cref="semaphore"/>
		/// </summary>
		/// <param name="graceful">If <see langword="true"/> the termination will be delayed until a reboot is detected in the active server's DMAPI and this function will return immediately</param>
		/// <param name="announce">If <see langword="true"/> the termination will be announced using <see cref="chat"/></param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		async Task TerminateNoLock(bool graceful, bool announce, CancellationToken cancellationToken)
		{
			if (!Running)
				return;
			if (!graceful)
			{
				var chatTask = announce ? chat.SendWatchdogMessage("Terminating...", cancellationToken) : Task.CompletedTask;
				await StopMonitor().ConfigureAwait(false);
				DisposeAndNullControllers();
				await chatTask.ConfigureAwait(false);
				return;
			}
			var toKill = AlphaIsActive ? alphaServer : bravoServer;
			var other = AlphaIsActive ? bravoServer : alphaServer;
			if (toKill != null)
				await toKill.SetRebootState(Components.Watchdog.RebootState.Shutdown, cancellationToken).ConfigureAwait(false);
		}

		/// <summary>
		/// Handles the actions to take when the monitor has to "wake up"
		/// </summary>
		/// <param name="activationReason">The <see cref="MonitorActivationReason"/> that caused the invocation</param>
		/// <param name="monitorState">The current <see cref="MonitorState"/>. Will be modified upon retrn</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		async Task HandlerMonitorWakeup(MonitorActivationReason activationReason, MonitorState monitorState, CancellationToken cancellationToken)
		{
			logger.LogInformation("Monitor activation. Reason: {0}", activationReason);

			//returns true if the inactive server can't be used immediately
			bool FullRestartDeadInactive()
			{
				if (monitorState.RebootingInactiveServer || monitorState.InactiveServerCritFail)
				{
					logger.LogInformation("Inactive server is {0}! Restarting monitor...", monitorState.InactiveServerCritFail ? "critically failed" : "still rebooting");
					monitorState.NextAction = MonitorAction.Restart;    //will dispose server
					return true;
				}
				return false;
			};

			//trys to set inactive server's port to the private port
			async Task<bool> MakeInactiveActive()
			{
				logger.LogInformation("Setting inactive server to port {0}...", ActiveLaunchParameters.PrimaryPort.Value);
				var result = await monitorState.InactiveServer.SetPort(ActiveLaunchParameters.PrimaryPort.Value, cancellationToken).ConfigureAwait(false);

				if (!result)
				{
					logger.LogWarning("Failed to activate inactive server! Restarting monitor...");
					monitorState.NextAction = MonitorAction.Restart;    //will dispose server
					return false;
				}

				// should always be set for InactiveServer
				monitorState.InactiveServer.ClosePortOnReboot = false;
				monitorState.ActiveServer.ClosePortOnReboot = true;

				//inactive server should always be using active launch parameters
				LastLaunchParameters = ActiveLaunchParameters;

				var tmp = monitorState.ActiveServer;
				monitorState.ActiveServer = monitorState.InactiveServer;
				monitorState.InactiveServer = tmp;
				AlphaIsActive = !AlphaIsActive;
				return true;
			}

			// Tries to load inactive server with latest dmb, falling back to current dmb on failure. Requires a lock on <see cref="semaphore"/>
			async Task<bool> RestartInactiveServer()
			{
				logger.LogInformation("Rebooting inactive server...");
				var newDmb = dmbFactory.LockNextDmb(1);
				bool usedMostRecentDmb;
				try
				{
					monitorState.InactiveServer = await sessionControllerFactory.LaunchNew(ActiveLaunchParameters, newDmb, null, false, !monitorState.ActiveServer.IsPrimary, false, cancellationToken).ConfigureAwait(false);
					usedMostRecentDmb = true;
				}
				catch (OperationCanceledException)
				{
					throw;
				}
				catch (Exception e)
				{
					logger.LogError("Exception occurred while recreating server! Attempting backup strategy of running DMB of running server! Exception: {0}", e.ToString());
					//ahh jeez, what do we do here?
					//this is our fault, so it should never happen but
					//idk maybe a database error while handling the newest dmb?
					//either way try to start it using the active server's dmb as a backup
					try
					{
						var dmbBackup = await dmbFactory.FromCompileJob(monitorState.ActiveServer.Dmb.CompileJob, cancellationToken).ConfigureAwait(false);

						if (dmbBackup == null)	//NANI!?
							//just give up, if THAT compile job is failing then the ActiveServer is gonna crash soon too or already has
							throw new JobException("Creating backup DMB provider failed!");

						monitorState.InactiveServer = await sessionControllerFactory.LaunchNew(ActiveLaunchParameters, dmbBackup, null, false, !monitorState.ActiveServer.IsPrimary, false, cancellationToken).ConfigureAwait(false);
						usedMostRecentDmb = false;
						await chat.SendWatchdogMessage("Staging newest DMB on inactive server failed: {0} Falling back to previous dmb...", cancellationToken).ConfigureAwait(false);
					}
					catch (OperationCanceledException)
					{
						throw;
					}
					catch (Exception e2)
					{
						//fuuuuucckkk
						logger.LogError("Backup strategy failed! Monitor will restart when active server reboots! Exception: {0}", e2.ToString());
						monitorState.InactiveServerCritFail = true;
						await chat.SendWatchdogMessage("Attempted reboot of inactive server failed. Watchdog will reset when active server fails or exits", cancellationToken).ConfigureAwait(false);
						return true;    //we didn't use the old dmb
					}
				}

				logger.LogInformation("Successfully relaunched inactive server!");
				monitorState.RebootingInactiveServer = true;
				// should always be set for InactiveServer
				monitorState.InactiveServer.ClosePortOnReboot = false;
				return usedMostRecentDmb;
			}

			async Task UpdateAndRestartInactiveServer(bool breakAfter)
			{
				//replace the notification tcs here so that the next loop will read a fresh one
				activeParametersUpdated = new TaskCompletionSource<object>();
				monitorState.InactiveServer.Dispose();  //kill or recycle it
				monitorState.NextAction = breakAfter ? MonitorAction.Break : MonitorAction.Continue;

				var usedLatestDmb = await RestartInactiveServer().ConfigureAwait(false);

				if (monitorState.NextAction == (breakAfter ? MonitorAction.Break : MonitorAction.Continue))
				{
					monitorState.ActiveServer.ClosePortOnReboot = false;
					if (monitorState.InactiveServerHasStagedDmb && !usedLatestDmb)
						monitorState.InactiveServerHasStagedDmb = false;    //don't try to load it again though
				}
			};

			//reason handling
			switch (activationReason)
			{
				case MonitorActivationReason.ActiveServerCrashed:
					if(monitorState.ActiveServer.RebootState == Components.Watchdog.RebootState.Shutdown)
					{
						await chat.SendWatchdogMessage("Active server crashed or exited! Exiting due to graceful termination request...", cancellationToken).ConfigureAwait(false);
						monitorState.NextAction = MonitorAction.Exit;
						break;
					}

					if (FullRestartDeadInactive())
					{
						await chat.SendWatchdogMessage("Active server crashed or exited! Inactive server unable to online!", cancellationToken).ConfigureAwait(false);
						break;
					}

					await chat.SendWatchdogMessage("Active server crashed or exited! Onlining inactive server...", cancellationToken).ConfigureAwait(false);
					if (!await MakeInactiveActive().ConfigureAwait(false))
						break;
					
					monitorState.ActiveServer.ClosePortOnReboot = false;
					await UpdateAndRestartInactiveServer(true).ConfigureAwait(false);
					break;
				case MonitorActivationReason.InactiveServerCrashed:
					await chat.SendWatchdogMessage("Inactive server crashed or exited! Rebooting...", cancellationToken).ConfigureAwait(false);
					await UpdateAndRestartInactiveServer(false).ConfigureAwait(false);
					break;
				case MonitorActivationReason.ActiveServerRebooted:
					if (FullRestartDeadInactive())
						break;

					//what matters here is the RebootState
					bool restartOnceSwapped = false;
					var rebootState = monitorState.ActiveServer.RebootState;
					monitorState.ActiveServer.ResetRebootState();	//the DMAPI has already done this internally

					switch (rebootState)
					{
						case Components.Watchdog.RebootState.Normal:
							break;
						case Components.Watchdog.RebootState.Restart:
							restartOnceSwapped = true;
							break;
						case Components.Watchdog.RebootState.Shutdown:
							await chat.SendWatchdogMessage("Active server rebooted! Exiting due to graceful termination request...", cancellationToken).ConfigureAwait(false);
							DisposeAndNullControllers();
							monitorState.NextAction = MonitorAction.Exit;
							return;
					}

					var sameCompileJob = monitorState.InactiveServer.Dmb.CompileJob.Id == monitorState.ActiveServer.Dmb.CompileJob.Id;
					if (sameCompileJob && monitorState.InactiveServerHasStagedDmb)
						//both servers up to date
						monitorState.InactiveServerHasStagedDmb = false;
					if (!sameCompileJob || ActiveLaunchParameters != LastLaunchParameters)
						//need a new launch in ActiveServer
						restartOnceSwapped = true;

					if (!await MakeInactiveActive().ConfigureAwait(false))
						break;

					if(!restartOnceSwapped)
						//try to reopen inactive server on the private port so it's not pinging all the time
						//failing that, just reboot it
						restartOnceSwapped = !await monitorState.InactiveServer.SetPort(ActiveLaunchParameters.SecondaryPort.Value, cancellationToken).ConfigureAwait(false);

					if (restartOnceSwapped) //for one reason or another, 
						await UpdateAndRestartInactiveServer(true).ConfigureAwait(false);	//break because worse case, active server is still booting
					else
						monitorState.NextAction = MonitorAction.Break;
					break;
				case MonitorActivationReason.InactiveServerRebooted:
					//should never happen but okay
					logger.LogWarning("Inactive server rebooted, this is a bug in DM code!");
					monitorState.RebootingInactiveServer = true;
					monitorState.InactiveServer.ResetRebootState();   //the DMAPI has already done this internally
					monitorState.ActiveServer.ClosePortOnReboot = false;
					monitorState.NextAction = MonitorAction.Continue;
					break;
				case MonitorActivationReason.InactiveServerStartupComplete:
					//eziest case of my life
					monitorState.RebootingInactiveServer = false;
					monitorState.ActiveServer.ClosePortOnReboot = true;
					monitorState.NextAction = MonitorAction.Continue;
					break;
				case MonitorActivationReason.NewDmbAvailable:
					monitorState.InactiveServerHasStagedDmb = true;
					await UpdateAndRestartInactiveServer(true).ConfigureAwait(false);	//next case does same thing
					break;
				case MonitorActivationReason.ActiveLaunchParametersUpdated:
					await UpdateAndRestartInactiveServer(false).ConfigureAwait(false);
					break;
			}
		}

		/// <summary>
		/// The loop that watches the watchdog
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		async Task MonitorLifetimes(CancellationToken cancellationToken)
		{
			logger.LogDebug("Entered MonitorLifetimes");
			var iteration = 1;
			for(var monitorState = new MonitorState(); monitorState.NextAction != MonitorAction.Exit; ++iteration)
			{
				logger.LogDebug("New iteration of monitor loop");
				try
				{
					if(AlphaIsActive)
						logger.LogDebug("Alpha is the active server");
					else
						logger.LogDebug("Bravo is the active server");

					if(monitorState.InactiveServerHasStagedDmb)
						logger.LogDebug("Inactive server has staged .dmb");
					if (monitorState.RebootingInactiveServer)
						logger.LogDebug("Inactive server is rebooting");

					monitorState.ActiveServer = AlphaIsActive ? alphaServer : bravoServer;
					monitorState.InactiveServer = AlphaIsActive ? bravoServer : alphaServer;

					var activeServerLifetime = monitorState.ActiveServer.Lifetime;
					var inactiveServerLifetime = monitorState.InactiveServer.Lifetime;
					var activeServerReboot = monitorState.ActiveServer.OnReboot;
					var inactiveServerReboot = monitorState.InactiveServer.OnReboot;
					var inactiveServerStartup = monitorState.InactiveServer.LaunchResult;
					var activeLaunchParametersChanged = activeParametersUpdated.Task;
					var newDmbAvailable = dmbFactory.OnNewerDmb;

					var cancelTcs = new TaskCompletionSource<object>();
					using (cancellationToken.Register(() => cancelTcs.SetCanceled()))
					{
						var toWaitOn = Task.WhenAny(activeServerLifetime, inactiveServerLifetime, activeServerReboot, inactiveServerReboot, newDmbAvailable, cancelTcs.Task, activeLaunchParametersChanged);
						if (monitorState.RebootingInactiveServer)
							toWaitOn = Task.WhenAny(toWaitOn, inactiveServerStartup);
						await toWaitOn.ConfigureAwait(false);
						cancellationToken.ThrowIfCancellationRequested();
					}

					var chatTask = Task.CompletedTask;
					using (await SemaphoreSlimContext.Lock(semaphore, cancellationToken).ConfigureAwait(false))
					{
						MonitorActivationReason activationReason = default;
						//multiple things may have happened, handle them one at a time
						for (var moreActivationsToProcess = true; moreActivationsToProcess && monitorState.NextAction == MonitorAction.Continue; )
						{
							if (activeServerLifetime?.IsCompleted == true)
							{
								activationReason = MonitorActivationReason.ActiveServerCrashed;
								activeServerLifetime = null;
							}
							else if (inactiveServerLifetime?.IsCompleted == true)
							{
								activationReason = MonitorActivationReason.InactiveServerCrashed;
								inactiveServerLifetime = null;
							}
							else if (activeServerReboot?.IsCompleted == true)
							{
								activationReason = MonitorActivationReason.ActiveServerRebooted;
								activeServerReboot = null;
							}
							else if (inactiveServerReboot?.IsCompleted == true)
							{
								activationReason = MonitorActivationReason.InactiveServerRebooted;
								inactiveServerReboot = null;
							}
							else if (inactiveServerStartup?.IsCompleted == true)
							{
								activationReason = MonitorActivationReason.InactiveServerStartupComplete;
								inactiveServerStartup = null;
							}
							else if (newDmbAvailable?.IsCompleted == true)
							{
								activationReason = MonitorActivationReason.NewDmbAvailable;
								newDmbAvailable = null;
							}
							else if(activeLaunchParametersChanged?.IsCompleted == true)
							{
								activationReason = MonitorActivationReason.ActiveLaunchParametersUpdated;
								activeLaunchParametersChanged = null;
							}
							else
								moreActivationsToProcess = false;
						}

						await HandlerMonitorWakeup(activationReason, monitorState, cancellationToken).ConfigureAwait(false);
						//writeback alphaServer and bravoServer
						alphaServer = AlphaIsActive ? monitorState.ActiveServer : monitorState.InactiveServer;
						bravoServer = AlphaIsActive ? monitorState.ActiveServer : monitorState.InactiveServer;
					}

					//full reboot required
					if (monitorState.NextAction == MonitorAction.Restart)
					{
						logger.LogDebug("Next state action is to restart");
						DisposeAndNullControllers();
						chatTask = chat.SendWatchdogMessage("Restarting entirely due to complications...", cancellationToken);
					}

					for (var retryAttempts = 1; monitorState.NextAction == MonitorAction.Restart; ++retryAttempts)
					{
						WatchdogLaunchResult result;
						using (await SemaphoreSlimContext.Lock(semaphore, cancellationToken).ConfigureAwait(false))
						{
							result = await LaunchNoLock(false, false, false, cancellationToken).ConfigureAwait(false);
							if (Running)
								monitorState = new MonitorState();  //clean the slate
						}

							await chatTask.ConfigureAwait(false);
						if(!Running)
						{
							logger.LogWarning("Failed to automatically restart the watchdog! Alpha: {0}; Bravo: {1}", result.Alpha.ToString(), result.Bravo.ToString());
							var retryDelay = Math.Min(Math.Pow(2, retryAttempts), 3600); //max of one hour
							chatTask = chat.SendWatchdogMessage(String.Format(CultureInfo.InvariantCulture, "Failed to restart watchdog (Attempt: {0}), retrying in {1} seconds...", retryAttempts, retryDelay), cancellationToken);
							await Task.WhenAll(Task.Delay((int)retryDelay, cancellationToken), chatTask).ConfigureAwait(false);
						}
					}
				}
				catch (OperationCanceledException)
				{
					logger.LogDebug("Monitor cancelled");
					break;
				}
				catch (Exception e)
				{
					logger.LogError("Monitor crashed! Iteration: {0}, State: {1}", iteration, JsonConvert.SerializeObject(monitorState));
					await chat.SendWatchdogMessage(String.Format(CultureInfo.InvariantCulture, "Monitor crashed, this should NEVER happen! Please report this, full details in logs! Restarting monitor... Error: {0}", e.Message), cancellationToken).ConfigureAwait(false);
				}
			}
		}

		async Task<bool> StopMonitor()
		{
			logger.LogTrace("StopMonitor");
			if (monitorTask == null)
				return false;
			monitorCts.Cancel();
			await monitorTask.ConfigureAwait(false);
			monitorCts.Dispose();
			monitorTask = null;
			return true;
		}


		/// <inheritdoc />
		public async Task ChangeSettings(DreamDaemonLaunchParameters launchParameters, CancellationToken cancellationToken)
		{
			using (await SemaphoreSlimContext.Lock(semaphore, cancellationToken).ConfigureAwait(false))
			{
				ActiveLaunchParameters = launchParameters;
				if (Running)
					//queue an update
					activeParametersUpdated.TrySetResult(null);
			}
		}

		async Task<WatchdogLaunchResult> LaunchNoLock(bool startMonitor, bool announce, bool doReattach, CancellationToken cancellationToken)
		{
			logger.LogTrace("Begin LaunchNoLock");
			using (var alphaStartCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
			{
				if (Running)
				{
					logger.LogTrace("Aborted due to already running!");
					return null;
				}
				
				Task chatTask;
				//this is necessary, the monitor could be in it's sleep loop trying to restart
				if (startMonitor && await StopMonitor().ConfigureAwait(false))
					chatTask = chat.SendWatchdogMessage("Automatic retry sequence cancelled by manual launch. Restarting...", cancellationToken);
				else if (announce)
					chatTask = chat.SendWatchdogMessage("Starting...", cancellationToken);
				else
					chatTask = Task.CompletedTask;
				//start both servers
				LastLaunchParameters = ActiveLaunchParameters;
				try
				{
					//good ole sanity
					if (alphaServer != null || bravoServer != null)
						throw new InvalidOperationException("Entered LaunchNoLock with one or more of the servers not being null!");

					var reattachInfo = doReattach ? await reattachInfoHandler.Load(cancellationToken).ConfigureAwait(false) : null;
					var doesntNeedNewDmb = doReattach && reattachInfo.Alpha != null && reattachInfo.Bravo != null;
					var dmbToUse = doesntNeedNewDmb ? null : dmbFactory.LockNextDmb(2);

					try
					{
						Task<ISessionController> alphaServerTask;
						if (!doesntNeedNewDmb)
							alphaServerTask = sessionControllerFactory.LaunchNew(ActiveLaunchParameters, dmbToUse, null, true, true, false, alphaStartCts.Token);
						else
							alphaServerTask = sessionControllerFactory.Reattach(reattachInfo.Alpha, cancellationToken);

						//wait until this boy officially starts so as not to confuse the servers as to who came first
						var startTime = DateTimeOffset.Now;
						alphaServer = await alphaServerTask.ConfigureAwait(false);
						alphaServer.SetHighPriority();

						//extra delay for total ordering
						var now = DateTimeOffset.Now;
						var delay = now - startTime;

						if (delay.TotalSeconds < AlphaBravoStartupSeperationInterval)
							await Task.Delay(startTime.AddSeconds(AlphaBravoStartupSeperationInterval) - now, cancellationToken).ConfigureAwait(false);

						Task<ISessionController> bravoServerTask;
						if (!doesntNeedNewDmb)
							bravoServerTask = sessionControllerFactory.LaunchNew(ActiveLaunchParameters, dmbToUse, null, false, false, false, cancellationToken);
						else
							bravoServerTask = sessionControllerFactory.Reattach(reattachInfo.Bravo, cancellationToken);

						bravoServer = await bravoServerTask.ConfigureAwait(false);
						bravoServer.SetHighPriority();

						async Task<LaunchResult> CheckLaunch(ISessionController controller, string serverName)
						{
							var launch = await controller.LaunchResult.ConfigureAwait(false);
							if (launch.ExitCode.HasValue)
								//you killed us ray...
								throw new JobException(String.Format(CultureInfo.InvariantCulture, "{1} server failed to start: {0}", launch.ToString(), serverName));
							if (!launch.StartupTime.HasValue)
								throw new JobException(String.Format(CultureInfo.InvariantCulture, "{1} server timed out on startup: {0}s", launch.ToString(), ActiveLaunchParameters.StartupTimeout.Value));
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
						cancellationToken.ThrowIfCancellationRequested();

						//both servers are now running, alpha is the active server, huzzah
						AlphaIsActive = doReattach ? reattachInfo.AlphaIsActive : true;
						LastLaunchResult = alphaLrt.Result;
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
						if (!doesntNeedNewDmb && (alphaServer == null && bravoServer == null))
						{
							dmbToUse.Dispose(); //guaranteed to not be null here
							dmbToUse.Dispose();	//yes, dispose it twice. See the definition of IDmbFactory.LockNextDmb(), we called it with 2 locks
						}
						DisposeAndNullControllers();
						throw;
					}
				}
				catch (Exception e)
				{
					logger.LogWarning("Failed to start watchdog: {0}", e.ToString());
					throw;
				}
				finally
				{
					try
					{
						await chatTask.ConfigureAwait(false);
					}
					catch (OperationCanceledException) { }
				}
			}
		}

		/// <inheritdoc />
		public async Task<WatchdogLaunchResult> Launch(CancellationToken cancellationToken)
		{
			using (await SemaphoreSlimContext.Lock(semaphore, cancellationToken).ConfigureAwait(false))
				return await LaunchNoLock(true, true, false, cancellationToken).ConfigureAwait(false);
		}

		/// <inheritdoc />
		public async Task<WatchdogLaunchResult> Restart(bool graceful, CancellationToken cancellationToken)
		{
			using (await SemaphoreSlimContext.Lock(semaphore, cancellationToken).ConfigureAwait(false))
				return await RestartNoLock(graceful, cancellationToken).ConfigureAwait(false);
		}

		/// <inheritdoc />
		public async Task Terminate(bool graceful, CancellationToken cancellationToken)
		{
			using (await SemaphoreSlimContext.Lock(semaphore, cancellationToken).ConfigureAwait(false))
				await TerminateNoLock(graceful, true, cancellationToken).ConfigureAwait(false);
		}

		/// <inheritdoc />
		public async Task StartAsync(CancellationToken cancellationToken)
		{
			if (autoStart)
				await LaunchNoLock(true, true, true, cancellationToken).ConfigureAwait(false);
		}

		/// <inheritdoc />
		public async Task StopAsync(CancellationToken cancellationToken)
		{
			if (releaseServers && Running)
			{
				var reattachInformation = new WatchdogReattachInformation
				{
					AlphaIsActive = AlphaIsActive
				};
				reattachInformation.Alpha = alphaServer?.Release();
				reattachInformation.Bravo = bravoServer?.Release();
				await reattachInfoHandler.Save(reattachInformation, cancellationToken).ConfigureAwait(false);
			}
			await Terminate(false, cancellationToken).ConfigureAwait(false);
		}

		/// <inheritdoc />
		public async Task<bool> HandleEvent(EventType eventType, IEnumerable<string> parameters, CancellationToken cancellationToken)
		{
			string results;
			using (await SemaphoreSlimContext.Lock(semaphore, cancellationToken).ConfigureAwait(false))
			{
				if (!Running)
					return true;

				var builder = new StringBuilder(Constants.DMTopicEvent);
				builder.Append("&");
				var notification = new EventNotification
				{
					Type = eventType,
					Parameters = parameters
				};
				var json = JsonConvert.SerializeObject(notification);
				builder.Append(byondTopicSender.SanitizeString(json));

				var activeServer = AlphaIsActive ? alphaServer : bravoServer;
				results = await activeServer.SendCommand(builder.ToString(), cancellationToken).ConfigureAwait(false);
			}

			if (results == Constants.DMResponseSuccess)
				return true;

			List<Response> responses;
			try
			{
				responses = JsonConvert.DeserializeObject<List<Response>>(results);
			}
			catch
			{
				logger.LogInformation("Recieved invalid response from DD when parsing event {0}:{1}{2}", eventType, Environment.NewLine, results);
				return true;
			}

			await Task.WhenAll(responses.Select(x => chat.SendMessage(x.Message, x.ChannelIds, cancellationToken))).ConfigureAwait(false);

			return true;
		}

		/// <inheritdoc />
		public async Task<string> HandleChatCommand(string commandName, string arguments, Chat.User sender, CancellationToken cancellationToken)
		{
			using (await SemaphoreSlimContext.Lock(semaphore, cancellationToken).ConfigureAwait(false))
			{
				if (!Running)
					return "ERROR: Server offline!";

				var command = String.Format(CultureInfo.InvariantCulture, "{0}&{1}={2}", byondTopicSender.SanitizeString(Constants.DMTopicChatCommand), byondTopicSender.SanitizeString(Constants.DMParameterData), byondTopicSender.SanitizeString(JsonConvert.SerializeObject(arguments)));

				var activeServer = AlphaIsActive ? alphaServer : bravoServer;
				return await activeServer.SendCommand(command, cancellationToken).ConfigureAwait(false) ?? "ERROR: Bad topic exchange!";
			}
		}
	}
}
