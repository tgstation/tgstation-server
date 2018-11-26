using Byond.TopicSender;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Api.Models.Internal;
using Tgstation.Server.Api.Rights;
using Tgstation.Server.Host.Components.Chat;
using Tgstation.Server.Host.Components.Compiler;
using Tgstation.Server.Host.Components.Interop;
using Tgstation.Server.Host.Core;

namespace Tgstation.Server.Host.Components.Watchdog
{
	/// <inheritdoc />
	sealed class Watchdog : IWatchdog, ICustomCommandHandler, IRestartHandler
	{
		/// <summary>
		/// The time in seconds to wait from starting <see cref="alphaServer"/> to start <see cref="bravoServer"/>. Does not take responsiveness into account
		/// </summary>
		const int AlphaBravoStartupSeperationInterval = 10;

		/// <inheritdoc />
		public bool Running { get; private set; }

		/// <inheritdoc />
		public bool AlphaIsActive { get; private set; }

		/// <inheritdoc />
		public Models.CompileJob ActiveCompileJob => (AlphaIsActive ? alphaServer : bravoServer)?.Dmb.CompileJob;

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
		/// The <see cref="IJobManager"/> for the <see cref="Watchdog"/>
		/// </summary>
		readonly IJobManager jobManager;

		/// <summary>
		/// The <see cref="IRestartRegistration"/> for the <see cref="Watchdog"/>
		/// </summary>
		readonly IRestartRegistration restartRegistration;

		/// <summary>
		/// The <see cref="IAsyncDelayer"/> for the <see cref="Watchdog"/>
		/// </summary>
		readonly IAsyncDelayer asyncDelayer;

		/// <summary>
		/// The <see cref="ILogger{TCategoryName}"/> for the <see cref="Watchdog"/>
		/// </summary>
		readonly ILogger<Watchdog> logger;

		/// <summary>
		/// The <see cref="SemaphoreSlim"/> for the <see cref="Watchdog"/>
		/// </summary>
		readonly SemaphoreSlim semaphore;

		/// <summary>
		/// The <see cref="Api.Models.Instance"/> for the <see cref="Watchdog"/>
		/// </summary>
		readonly Api.Models.Instance instance;

		/// <summary>
		/// If the <see cref="Watchdog"/> should <see cref="LaunchImplNoLock(bool, bool, WatchdogReattachInformation, CancellationToken)"/> in <see cref="StartAsync(CancellationToken)"/>
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
		/// <param name="reattachInfoHandler">The value of <see cref="reattachInfoHandler"/></param>
		/// <param name="databaseContextFactory">The value of <see cref="databaseContextFactory"/></param>
		/// <param name="byondTopicSender">The value of <see cref="byondTopicSender"/></param>
		/// <param name="eventConsumer">The value of <see cref="eventConsumer"/></param>
		/// <param name="jobManager">The value of <see cref="jobManager"/></param>
		/// <param name="serverControl">The <see cref="IServerControl"/> to populate <see cref="restartRegistration"/> with</param>
		/// <param name="asyncDelayer">The value of <see cref="asyncDelayer"/></param>
		/// <param name="logger">The value of <see cref="logger"/></param>
		/// <param name="initialLaunchParameters">The initial value of <see cref="ActiveLaunchParameters"/>. May be modified</param>
		/// <param name="instance">The value of <see cref="instance"/></param>
		/// <param name="autoStart">The value of <see cref="autoStart"/></param>
		public Watchdog(IChat chat, ISessionControllerFactory sessionControllerFactory, IDmbFactory dmbFactory, IReattachInfoHandler reattachInfoHandler, IDatabaseContextFactory databaseContextFactory, IByondTopicSender byondTopicSender, IEventConsumer eventConsumer, IJobManager jobManager, IServerControl serverControl, IAsyncDelayer asyncDelayer, ILogger<Watchdog> logger, DreamDaemonLaunchParameters initialLaunchParameters, Api.Models.Instance instance, bool autoStart)
		{
			this.chat = chat ?? throw new ArgumentNullException(nameof(chat));
			this.sessionControllerFactory = sessionControllerFactory ?? throw new ArgumentNullException(nameof(sessionControllerFactory));
			this.dmbFactory = dmbFactory ?? throw new ArgumentNullException(nameof(dmbFactory));
			this.reattachInfoHandler = reattachInfoHandler ?? throw new ArgumentNullException(nameof(reattachInfoHandler));
			this.databaseContextFactory = databaseContextFactory ?? throw new ArgumentNullException(nameof(databaseContextFactory));
			this.byondTopicSender = byondTopicSender ?? throw new ArgumentNullException(nameof(byondTopicSender));
			this.eventConsumer = eventConsumer ?? throw new ArgumentNullException(nameof(eventConsumer));
			this.jobManager = jobManager ?? throw new ArgumentNullException(nameof(jobManager));
			this.asyncDelayer = asyncDelayer ?? throw new ArgumentNullException(nameof(asyncDelayer));
			this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
			ActiveLaunchParameters = initialLaunchParameters ?? throw new ArgumentNullException(nameof(initialLaunchParameters));
			this.instance = instance ?? throw new ArgumentNullException(nameof(instance));
			this.autoStart = autoStart;

			if (serverControl == null)
				throw new ArgumentNullException(nameof(serverControl));
			
			chat.RegisterCommandHandler(this);

			AlphaIsActive = true;
			ActiveLaunchParameters = initialLaunchParameters;
			releaseServers = false;
			activeParametersUpdated = new TaskCompletionSource<object>();

			restartRegistration = serverControl.RegisterForRestart(this);
			try
			{
				semaphore = new SemaphoreSlim(1);
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
			DisposeAndNullControllers();
			semaphore.Dispose();
			restartRegistration.Dispose();
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
				LastLaunchParameters = null;
				await chatTask.ConfigureAwait(false);
				return;
			}

			//merely set the reboot state
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
			logger.LogDebug("Monitor activation. Reason: {0}", activationReason);

			//this is where the bulk of the watchdog handling code lives and is fraught with lambdas, sorry not sorry
			//I'll do my best to walk you through it

			//returns true if the inactive server can't be used immediately
			//also sets monitor to restart if the above holds
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

			//trys to set inactive server's port to the public game port
			//doesn't handle closing active server's port
			//returns true on success and swaps inactiveserver and activeserver also sets LastLaunchParameters to ActiveLaunchParameters
			//on failure, sets monitor to restart
			async Task<bool> MakeInactiveActive()
			{
				logger.LogDebug("Setting inactive server to port {0}...", ActiveLaunchParameters.PrimaryPort.Value);
				var result = await monitorState.InactiveServer.SetPort(ActiveLaunchParameters.PrimaryPort.Value, cancellationToken).ConfigureAwait(false);

				if (!result)
				{
					logger.LogWarning("Failed to activate inactive server! Restarting monitor...");
					monitorState.NextAction = MonitorAction.Restart;    //will dispose server
					return false;
				}

				//inactive server should always be using active launch parameters
				LastLaunchParameters = ActiveLaunchParameters;

				var tmp = monitorState.ActiveServer;
				monitorState.ActiveServer = monitorState.InactiveServer;
				monitorState.InactiveServer = tmp;
				AlphaIsActive = !AlphaIsActive;
				monitorState.ActiveServer.EnableCustomChatCommands();
				return true;
			}

			// Kills and tries to launch inactive server with the latest dmb
			// falls back to current dmb on failure
			// Sets critfail on inactive server failing that
			// returns false if the backup dmb was used successfully, true otherwise
			async Task UpdateAndRestartInactiveServer(bool breakAfter)
			{
				activeParametersUpdated = new TaskCompletionSource<object>();
				monitorState.InactiveServer.Dispose();  //kill or recycle it
				var desiredNextAction = breakAfter ? MonitorAction.Break : MonitorAction.Continue;
				monitorState.NextAction = desiredNextAction;

				logger.LogInformation("Rebooting inactive server...");
				var newDmb = dmbFactory.LockNextDmb(1);
				try
				{
					monitorState.InactiveServer = await sessionControllerFactory.LaunchNew(ActiveLaunchParameters, newDmb, null, false, !monitorState.ActiveServer.IsPrimary, false, cancellationToken).ConfigureAwait(false);
					monitorState.InactiveServer.SetHighPriority();
				}
				catch (OperationCanceledException)
				{
					throw;
				}
				catch (Exception e)
				{
					logger.LogError("Error occurred while recreating server! Attempting backup strategy of running DMB of running server! Exception: {0}", e.ToString());
					//ahh jeez, what do we do here?
					//this is our fault, so it should never happen but
					//idk maybe a database error while handling the newest dmb?
					//either way try to start it using the active server's dmb as a backup
					try
					{
						var dmbBackup = await dmbFactory.FromCompileJob(monitorState.ActiveServer.Dmb.CompileJob, cancellationToken).ConfigureAwait(false);

						if (dmbBackup == null)  //NANI!?
												//just give up, if THAT compile job is failing then the ActiveServer is gonna crash soon too or already has
							throw new JobException("Creating backup DMB provider failed!");

						monitorState.InactiveServer = await sessionControllerFactory.LaunchNew(ActiveLaunchParameters, dmbBackup, null, false, !monitorState.ActiveServer.IsPrimary, false, cancellationToken).ConfigureAwait(false);
						monitorState.InactiveServer.SetHighPriority();
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
						return;
					}
				}

				logger.LogInformation("Successfully relaunched inactive server!");
				monitorState.RebootingInactiveServer = true;
			}

			string ExitWord(ISessionController controller) => controller.TerminationWasRequested ? "exited" : "crashed";

			//reason handling
			switch (activationReason)
			{
				case MonitorActivationReason.ActiveServerCrashed:
					if (monitorState.ActiveServer.RebootState == Components.Watchdog.RebootState.Shutdown)
					{
						//the time for graceful shutdown is now
						await chat.SendWatchdogMessage(String.Format(CultureInfo.InvariantCulture, "Active server {0}! Exiting due to graceful termination request...", ExitWord(monitorState.ActiveServer)), cancellationToken).ConfigureAwait(false);
						DisposeAndNullControllers();
						monitorState.NextAction = MonitorAction.Exit;
						break;
					}

					if (FullRestartDeadInactive())
					{
						//tell chat about it and go ahead
						await chat.SendWatchdogMessage(String.Format(CultureInfo.InvariantCulture, "Active server {0}! Inactive server unable to online!", ExitWord(monitorState.ActiveServer)), cancellationToken).ConfigureAwait(false);
						//we've already been set to restart
						break;
					}

					//tell chat about it
					await chat.SendWatchdogMessage(String.Format(CultureInfo.InvariantCulture, "Active server {0}! Onlining inactive server...", ExitWord(monitorState.ActiveServer)), cancellationToken).ConfigureAwait(false);

					//try to active the inactive server
					if (!await MakeInactiveActive().ConfigureAwait(false))
						//failing that, we've already been set to restart
						break;

					//bring up another inactive server
					await UpdateAndRestartInactiveServer(true).ConfigureAwait(false);
					break;
				case MonitorActivationReason.InactiveServerCrashed:
					//just announce and try to bring it back
					await chat.SendWatchdogMessage(String.Format(CultureInfo.InvariantCulture, "Inactive server {0}! Rebooting...", ExitWord(monitorState.InactiveServer)), cancellationToken).ConfigureAwait(false);
					await UpdateAndRestartInactiveServer(false).ConfigureAwait(false);
					break;
				case MonitorActivationReason.ActiveServerRebooted:
					//ideal goal: active server just closed its port
					//tell inactive server to open it's port and that's now the active server
					var rebootState = monitorState.ActiveServer.RebootState;
					monitorState.ActiveServer.ResetRebootState();   //the DMAPI has already done this internally

					if (FullRestartDeadInactive() && rebootState != Components.Watchdog.RebootState.Shutdown)
						//full restart if the inactive server is being fucky
						break;

					//what matters here is the RebootState
					var restartOnceSwapped = false;

					switch (rebootState)
					{
						case Components.Watchdog.RebootState.Normal:
							//life as normal
							break;
						case Components.Watchdog.RebootState.Restart:
							//reboot the current active server once the inactive one activates
							restartOnceSwapped = true;
							break;
						case Components.Watchdog.RebootState.Shutdown:
							//graceful shutdown time
							await chat.SendWatchdogMessage("Active server rebooted! Exiting due to graceful termination request...", cancellationToken).ConfigureAwait(false);
							DisposeAndNullControllers();
							monitorState.NextAction = MonitorAction.Exit;
							return;
					}

					//are both servers now running the same CompileJob?
					var sameCompileJob = monitorState.InactiveServer.Dmb.CompileJob.Id == monitorState.ActiveServer.Dmb.CompileJob.Id;
					
					if (!sameCompileJob || ActiveLaunchParameters != LastLaunchParameters)
						//need a new launch to update either settings or compile job
						restartOnceSwapped = true;

					if (restartOnceSwapped)
						//we need to manually restart active server
						//just kill it here, easier that way
						monitorState.ActiveServer.Dispose();

					var activeServerStillHasPortOpen = !restartOnceSwapped && !monitorState.ActiveServer.ClosePortOnReboot;

					if (activeServerStillHasPortOpen)
						//we didn't want active server to swap for some reason and it still has it's port open
						//just continue as normal
						break;
					
					if (!await MakeInactiveActive().ConfigureAwait(false))
						//monitor will restart
						break;

					//servers now swapped

					//enable this now if inactive server is not still valid
					monitorState.ActiveServer.ClosePortOnReboot = restartOnceSwapped;

					if (!restartOnceSwapped)
						//now try to reopen it on the private port
						//failing that, just reboot it
						restartOnceSwapped = !await monitorState.InactiveServer.SetPort(ActiveLaunchParameters.SecondaryPort.Value, cancellationToken).ConfigureAwait(false);

					//break either way because any issues past this point would be solved by the reboot
					if (restartOnceSwapped) 
						//for one reason or another
						//update and reboot
						await UpdateAndRestartInactiveServer(true).ConfigureAwait(false);
					else
						//only skip checking inactive server rebooted, it's guaranteed InactiveServerStartup complete wouldn't fire this iteration
						monitorState.NextAction = MonitorAction.Skip;
					break;
				case MonitorActivationReason.InactiveServerRebooted:
					//just don't let the active server close it's port if the inactive server isn't ready
					monitorState.RebootingInactiveServer = true;
					monitorState.InactiveServer.ResetRebootState();
					monitorState.ActiveServer.ClosePortOnReboot = false;
					monitorState.NextAction = MonitorAction.Continue;
					break;
				case MonitorActivationReason.InactiveServerStartupComplete:
					//opposite of above case
					monitorState.RebootingInactiveServer = false;
					monitorState.ActiveServer.ClosePortOnReboot = true;
					monitorState.NextAction = MonitorAction.Continue;
					break;
				case MonitorActivationReason.NewDmbAvailable:
				case MonitorActivationReason.ActiveLaunchParametersUpdated:
					//just reload the inactive server and wait for a swap to apply the changes
					await UpdateAndRestartInactiveServer(true).ConfigureAwait(false);
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
			logger.LogTrace("Entered MonitorLifetimes");

			//this function is responsible for calling HandlerMonitorWakeup when necessary and manitaining the MonitorState

			var iteration = 1;
			for (var monitorState = new MonitorState(); monitorState.NextAction != MonitorAction.Exit; ++iteration)
			{
				//always start out with continue
				monitorState.NextAction = MonitorAction.Continue;

				//dump some info to the logs
				logger.LogDebug("Iteration {0} of monitor loop", iteration);
				try
				{
					if (AlphaIsActive)
						logger.LogDebug("Alpha is the active server");
					else
						logger.LogDebug("Bravo is the active server");
					
					
					if (monitorState.RebootingInactiveServer)
						logger.LogDebug("Inactive server is rebooting");

					//update the monitor state with the inactive/active servers
					monitorState.ActiveServer = AlphaIsActive ? alphaServer : bravoServer;
					monitorState.InactiveServer = AlphaIsActive ? bravoServer : alphaServer;

					if (monitorState.ActiveServer.ClosePortOnReboot)
						logger.LogDebug("Active server will close port on reboot");
					if (monitorState.InactiveServer.ClosePortOnReboot)
						logger.LogDebug("Inactive server will close port on reboot");

					logger.LogDebug("Active server Compile Job ID: {0}", monitorState.ActiveServer.Dmb.CompileJob.Id);
					logger.LogDebug("Inactive server Compile Job ID: {0}", monitorState.InactiveServer.Dmb.CompileJob.Id);

					//load the activation tasks into local variables
					Task activeServerLifetime = monitorState.ActiveServer.Lifetime;
					Task inactiveServerLifetime = monitorState.InactiveServer.Lifetime;
					var activeServerReboot = monitorState.ActiveServer.OnReboot;
					var inactiveServerReboot = monitorState.InactiveServer.OnReboot;
					Task inactiveServerStartup = monitorState.RebootingInactiveServer ? monitorState.InactiveServer.LaunchResult : null;
					Task activeLaunchParametersChanged = activeParametersUpdated.Task;
					var newDmbAvailable = dmbFactory.OnNewerDmb;

					//cancel waiting if requested
					var cancelTcs = new TaskCompletionSource<object>();
					using (cancellationToken.Register(() => cancelTcs.SetCanceled()))
					{
						var toWaitOn = Task.WhenAny(activeServerLifetime, inactiveServerLifetime, activeServerReboot, inactiveServerReboot, newDmbAvailable, cancelTcs.Task, activeLaunchParametersChanged);
						if (monitorState.RebootingInactiveServer)
							toWaitOn = Task.WhenAny(toWaitOn, inactiveServerStartup);
						//wait for something to happen
						await toWaitOn.ConfigureAwait(false);
						cancellationToken.ThrowIfCancellationRequested();
					}

					var chatTask = Task.CompletedTask;
					using (await SemaphoreSlimContext.Lock(semaphore, cancellationToken).ConfigureAwait(false))
					{
						//always run HandleMonitorWakeup from the context of the semaphore lock
						//multiple things may have happened, handle them one at a time
						for (var moreActivationsToProcess = true; moreActivationsToProcess && (monitorState.NextAction == MonitorAction.Continue || monitorState.NextAction == MonitorAction.Skip);)
						{
							MonitorActivationReason activationReason = default; //this will always be assigned before being used

							//process the tasks in this order and call HandlerMonitorWakup for each

							bool CheckActivationReason(ref Task task, MonitorActivationReason testActivationReason)
							{
								var taskCompleted = task?.IsCompleted == true;
								task = null;
								if (monitorState.NextAction == MonitorAction.Skip)
									monitorState.NextAction = MonitorAction.Continue;
								else if (taskCompleted)
								{
									activationReason = testActivationReason;
									return true;
								}
								return false; 
							};

							if (CheckActivationReason(ref activeServerLifetime, MonitorActivationReason.ActiveServerCrashed)
								|| CheckActivationReason(ref inactiveServerLifetime, MonitorActivationReason.InactiveServerCrashed)
								|| CheckActivationReason(ref activeServerReboot, MonitorActivationReason.ActiveServerRebooted)
								|| CheckActivationReason(ref inactiveServerReboot, MonitorActivationReason.InactiveServerRebooted)
								|| CheckActivationReason(ref inactiveServerStartup, MonitorActivationReason.InactiveServerStartupComplete)
								|| CheckActivationReason(ref newDmbAvailable, MonitorActivationReason.NewDmbAvailable)
								|| CheckActivationReason(ref activeLaunchParametersChanged, MonitorActivationReason.ActiveLaunchParametersUpdated))
								await HandlerMonitorWakeup(activationReason, monitorState, cancellationToken).ConfigureAwait(false);
							else
								moreActivationsToProcess = false;
						}

						//writeback alphaServer and bravoServer from monitor state in case they changesd
						alphaServer = AlphaIsActive ? monitorState.ActiveServer : monitorState.InactiveServer;
						bravoServer = !AlphaIsActive ? monitorState.ActiveServer : monitorState.InactiveServer;
					}

					//full reboot required
					if (monitorState.NextAction == MonitorAction.Restart)
					{
						logger.LogDebug("Next state action is to restart");
						DisposeAndNullControllers();
						chatTask = chat.SendWatchdogMessage("Restarting entirely due to complications...", cancellationToken);

						for (var retryAttempts = 1; monitorState.NextAction == MonitorAction.Restart; ++retryAttempts)
						{
							Exception launchException = null;
							using (await SemaphoreSlimContext.Lock(semaphore, cancellationToken).ConfigureAwait(false))
								try
								{
									//use LaunchImplNoLock without announcements or restarting the monitor
									await LaunchImplNoLock(false, false, null, cancellationToken).ConfigureAwait(false);
									if (Running)
									{
										logger.LogDebug("Relaunch successful, resetting monitor state...");
										monitorState = new MonitorState();  //clean the slate and continue
									}
								}
								catch (OperationCanceledException)
								{
									throw;
								}
								catch (Exception e)
								{
									launchException = e;
								}

							await chatTask.ConfigureAwait(false);
							if (!Running)
							{
								if (launchException == null)
									logger.LogWarning("Failed to automatically restart the watchdog! Attempt: {0}", retryAttempts);
								else
									logger.LogWarning("Failed to automatically restart the watchdog! Attempt: {0}, Exception: {1}", retryAttempts, launchException);
								var retryDelay = Math.Min(Math.Pow(2, retryAttempts), 3600); //max of one hour, increasing by a power of 2 each time
								chatTask = chat.SendWatchdogMessage(String.Format(CultureInfo.InvariantCulture, "Failed to restart watchdog (Attempt: {0}), retrying in {1} seconds...", retryAttempts, retryDelay), cancellationToken);
								await Task.WhenAll(asyncDelayer.Delay(TimeSpan.FromSeconds(retryDelay), cancellationToken), chatTask).ConfigureAwait(false);
							}
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
					//really, this should NEVER happen
					logger.LogError("Monitor crashed! Iteration: {0}, State: {1}, Exception: {2}", iteration, JsonConvert.SerializeObject(monitorState), e);
					await chat.SendWatchdogMessage(String.Format(CultureInfo.InvariantCulture, "Monitor crashed, this should NEVER happen! Please report this, full details in logs! Restarting monitor... Error: {0}", e.Message), cancellationToken).ConfigureAwait(false);
				}
			}
			logger.LogTrace("Monitor exiting...");
		}

		/// <summary>
		/// Stops <see cref="MonitorLifetimes(CancellationToken)"/>. Doesn't kill the servers
		/// </summary>
		/// <returns><see langword="true"/> if the monitor was running, <see langword="false"/> otherwise</returns>
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
				if (launchParameters.Match(ActiveLaunchParameters))
					return;
				ActiveLaunchParameters = launchParameters;
				if (Running)
					//queue an update
					activeParametersUpdated.TrySetResult(null);
			}
		}

		/// <summary>
		/// Launches the <see cref="Watchdog"/>
		/// </summary>
		/// <param name="startMonitor">If <see cref="MonitorLifetimes(CancellationToken)"/> should be started by this function</param>
		/// <param name="announce">If the launch should be announced to chat by this function</param>
		/// <param name="reattachInfo"><see cref="WatchdogReattachInformation"/> to use, if any</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		async Task LaunchImplNoLock(bool startMonitor, bool announce, WatchdogReattachInformation reattachInfo, CancellationToken cancellationToken)
		{
			logger.LogTrace("Begin LaunchNoLock");

			if (Running)
				throw new JobException("Watchdog already running!");

			Task chatTask;
			//this is necessary, the monitor could be in it's sleep loop trying to restart, if so cancel THAT monitor and start our own with blackjack and hookers
			if (startMonitor && await StopMonitor().ConfigureAwait(false))
				chatTask = chat.SendWatchdogMessage("Automatic retry sequence cancelled by manual launch. Restarting...", cancellationToken);
			else if (announce)
				//simple announce
				chatTask = chat.SendWatchdogMessage(reattachInfo == null ? "Starting..." : "Reattaching...", cancellationToken);
			else
				//no announce
				chatTask = Task.CompletedTask;
			
			//since neither server is running, this is safe to do
			LastLaunchParameters = ActiveLaunchParameters;

			//for when we call ourself and want to not catch thrown exceptions
			var ignoreNestedException = false;
			try
			{
				//good ole sanity, should never fucking trigger but i don't trust myself even though I should
				if (alphaServer != null || bravoServer != null)
					throw new InvalidOperationException("Entered LaunchNoLock with one or more of the servers not being null!");
				
				//don't need a new dmb if reattaching
				var doesntNeedNewDmb = reattachInfo?.Alpha != null && reattachInfo?.Bravo != null;
				var dmbToUse = doesntNeedNewDmb ? null : dmbFactory.LockNextDmb(2);

				//if this try catches something, both servers are killed
				try
				{
					//start the alpha server task, either by launch a new process or attaching to an existing one
					//The tasks returned are mainly for writing interop files to the directories among other things and should generally never fail
					//The tasks pertaining to server startup times are in the ISessionControllers
					Task<ISessionController> alphaServerTask;
					if (!doesntNeedNewDmb)
						alphaServerTask = sessionControllerFactory.LaunchNew(ActiveLaunchParameters, dmbToUse, null, true, true, false, cancellationToken);
					else
						alphaServerTask = sessionControllerFactory.Reattach(reattachInfo.Alpha, cancellationToken);

					//retrieve the session controller
					var startTime = DateTimeOffset.Now;
					alphaServer = await alphaServerTask.ConfigureAwait(false);
					//failed reattaches will return null
					alphaServer?.SetHighPriority();

					//extra delay for total ordering
					var now = DateTimeOffset.Now;
					var delay = now - startTime;

					//definitely not if reattaching though
					if (reattachInfo == null && delay.TotalSeconds < AlphaBravoStartupSeperationInterval)
						await asyncDelayer.Delay(startTime.AddSeconds(AlphaBravoStartupSeperationInterval) - now, cancellationToken).ConfigureAwait(false);

					//now bring bravo up
					if (!doesntNeedNewDmb)
						bravoServer = await sessionControllerFactory.LaunchNew(ActiveLaunchParameters, dmbToUse, null, false, false, false, cancellationToken).ConfigureAwait(false);
					else
						bravoServer = await sessionControllerFactory.Reattach(reattachInfo.Bravo, cancellationToken).ConfigureAwait(false);
					//failed reattaches will return null
					bravoServer?.SetHighPriority();

					//possiblity of null servers due to failed reattaches
					if (alphaServer == null || bravoServer == null)
					{
						await chatTask.ConfigureAwait(false);
						var bothServersDead = alphaServer == null && bravoServer == null;
						if (bothServersDead
							|| (alphaServer == null && reattachInfo.AlphaIsActive)
							|| (bravoServer == null && !reattachInfo.AlphaIsActive))
						{
							//we lost the active server, just restart entirely
							DisposeAndNullControllers();
							const string FailReattachMessage = "Unable to properly reattach to active server! Restarting...";
							logger.LogWarning(FailReattachMessage);
							logger.LogDebug(bothServersDead ? "Also could not reattach to inactive server!" : "Inactive server was reattached successfully!");
							chatTask = chat.SendWatchdogMessage(FailReattachMessage, cancellationToken);
							ignoreNestedException = true;
							await LaunchImplNoLock(true, false, null, cancellationToken).ConfigureAwait(false);
							return;
						}

						//we still have the active server but the other one is dead to us, hand it off to the monitor to restart
						const string InactiveReattachFailureMessage = "Unable to reattach to inactive server. Leaving for monitor to reboot...";
						chatTask = chat.SendWatchdogMessage(InactiveReattachFailureMessage, cancellationToken);
						logger.LogWarning(InactiveReattachFailureMessage);

						if (reattachInfo.AlphaIsActive)
							bravoServer = sessionControllerFactory.CreateDeadSession(reattachInfo.Bravo.Dmb);
						else
							alphaServer = sessionControllerFactory.CreateDeadSession(reattachInfo.Alpha.Dmb);
					}

					//throws a JobException if something went wrong with a launch
					//Dead sessions won't trigger this
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
					//this task completes when both serers have finished booting
					var allTask = Task.WhenAll(alphaLrt, bravoLrt);

					//don't forget about the cancellationToken
					var cancelTcs = new TaskCompletionSource<object>();
					using (cancellationToken.Register(() => cancelTcs.SetCanceled()))
						await Task.WhenAny(allTask, cancelTcs.Task).ConfigureAwait(false);
					cancellationToken.ThrowIfCancellationRequested();

					await allTask.ConfigureAwait(false);

					//both servers are now running, alpha is the active server(unless reattach), huzzah
					AlphaIsActive = reattachInfo?.AlphaIsActive ?? true;

					var activeServer = AlphaIsActive ? alphaServer : bravoServer;
					activeServer.EnableCustomChatCommands();
					activeServer.ClosePortOnReboot = true;

					logger.LogInformation("Launched servers successfully");
					Running = true;

					if (startMonitor)
					{
						monitorCts = new CancellationTokenSource();
						monitorTask = MonitorLifetimes(monitorCts.Token);
					}
				}
				catch
				{
					if (dmbToUse != null)
					{
						//we locked 2 dmbs
						if (bravoServer == null)
						{
							//bravo didn't get control of his
							dmbToUse.Dispose();
							if (alphaServer == null)
								//alpha didn't get control of his
								dmbToUse.Dispose();
						}
					}
					else if (doesntNeedNewDmb)
						//we have reattachInfo
						if (bravoServer == null)
						{
							//bravo didn't get control of his
							reattachInfo.Bravo?.Dmb.Dispose();
							if (alphaServer == null)
								//alpha didn't get control of his
								reattachInfo.Alpha?.Dmb.Dispose();
						}
					//kill the controllers
					DisposeAndNullControllers();
					throw;
				}
			}
			catch (Exception e)
			{
				//don't try to send chat tasks or warning logs if were suppressing exceptions or cancelled
				if (!ignoreNestedException && !cancellationToken.IsCancellationRequested)
				{
					var originalChatTask = chatTask;
					async Task ChainChatTaskWithErrorMessage()
					{
						await originalChatTask.ConfigureAwait(false);
						await chat.SendWatchdogMessage("Startup failed!", cancellationToken).ConfigureAwait(false);
					}
					chatTask = ChainChatTaskWithErrorMessage();
					logger.LogWarning("Failed to start watchdog: {0}", e.ToString());
				}
				throw;
			}
			finally
			{
				//finish the chat task that's in flight
				try
				{
					await chatTask.ConfigureAwait(false);
				}
				catch (OperationCanceledException) { }
			}
		}

		/// <inheritdoc />
		public async Task Launch(CancellationToken cancellationToken)
		{
			using (await SemaphoreSlimContext.Lock(semaphore, cancellationToken).ConfigureAwait(false))
				await LaunchImplNoLock(true, true, null, cancellationToken).ConfigureAwait(false);
		}

		/// <inheritdoc />
		public async Task ResetRebootState(CancellationToken cancellationToken)
		{
			using (await SemaphoreSlimContext.Lock(semaphore, cancellationToken).ConfigureAwait(false))
			{
				if (!Running)
					return;
				var toClear = AlphaIsActive ? alphaServer : bravoServer;
				if (toClear != null)
					toClear.ResetRebootState();
			}
		}

		/// <inheritdoc />
		public async Task Restart(bool graceful, CancellationToken cancellationToken)
		{
			logger.LogTrace("Begin Restart. Graceful: {0}", graceful);
			using (await SemaphoreSlimContext.Lock(semaphore, cancellationToken).ConfigureAwait(false))
			{
				if (!graceful || !Running)
				{
					Task chatTask;
					if (Running)
					{
						chatTask = chat.SendWatchdogMessage("Manual restart triggered...", cancellationToken);
						await TerminateNoLock(false, false, cancellationToken).ConfigureAwait(false);
					}
					else
						chatTask = Task.CompletedTask;
					await LaunchImplNoLock(true, !Running, null, cancellationToken).ConfigureAwait(false);
					await chatTask.ConfigureAwait(false);
				}
				var toReboot = AlphaIsActive ? alphaServer : bravoServer;
				if (toReboot != null)
				{
					if (!await toReboot.SetRebootState(Components.Watchdog.RebootState.Restart, cancellationToken).ConfigureAwait(false))
						logger.LogWarning("Unable to send reboot state change event!");
				}
			}
		}

		/// <inheritdoc />
		public async Task Terminate(bool graceful, CancellationToken cancellationToken)
		{
			using (await SemaphoreSlimContext.Lock(semaphore, cancellationToken).ConfigureAwait(false))
				await TerminateNoLock(graceful, !releaseServers, cancellationToken).ConfigureAwait(false);
		}

		/// <inheritdoc />
		public async Task StartAsync(CancellationToken cancellationToken)
		{
			var reattachInfo = await reattachInfoHandler.Load(cancellationToken).ConfigureAwait(false);
			if (!autoStart && reattachInfo == null)
				return;

			long? adminUserId = null;

			await databaseContextFactory.UseContext(async db => adminUserId = await db.Users
			.Where(x => x.CanonicalName == Api.Models.User.AdminName.ToUpperInvariant())
			.Select(x => x.Id)
			.FirstAsync(cancellationToken).ConfigureAwait(false)).ConfigureAwait(false);
			var job = new Models.Job
			{
				StartedBy = new Models.User
				{
					Id = adminUserId.Value
				},
				Instance = new Models.Instance
				{
					Id = instance.Id
				},
				Description = "Instance startup watchdog launch",
				CancelRight = (ulong)DreamDaemonRights.Shutdown,
				CancelRightsType = RightsType.DreamDaemon
			};
			await jobManager.RegisterOperation(job, async (j, databaseContext, progressFunction, ct) =>
			{
				using (await SemaphoreSlimContext.Lock(semaphore, ct).ConfigureAwait(false))
					await LaunchImplNoLock(true, true, reattachInfo, ct).ConfigureAwait(false);
			}, cancellationToken).ConfigureAwait(false);
		}

		/// <inheritdoc />
		public async Task StopAsync(CancellationToken cancellationToken)
		{
			if (releaseServers && Running)
			{
				await StopMonitor().ConfigureAwait(false);

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
				builder.Append('&');
				var notification = new EventNotification
				{
					Type = eventType,
					Parameters = parameters
				};
				var json = JsonConvert.SerializeObject(notification);
				builder.Append(byondTopicSender.SanitizeString(Constants.DMParameterData));
				builder.Append('=');
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

				var commandObject = new ChatCommand
				{
					Command = commandName,
					Params = arguments,
					User = sender
				};

				var json = JsonConvert.SerializeObject(commandObject, new JsonSerializerSettings
				{
					ContractResolver = new CamelCasePropertyNamesContractResolver()
				});

				var command = String.Format(CultureInfo.InvariantCulture, "{0}&{1}={2}", byondTopicSender.SanitizeString(Constants.DMTopicChatCommand), byondTopicSender.SanitizeString(Constants.DMParameterData), byondTopicSender.SanitizeString(json));

				var activeServer = AlphaIsActive ? alphaServer : bravoServer;
				return await activeServer.SendCommand(command, cancellationToken).ConfigureAwait(false) ?? "ERROR: Bad topic exchange!";
			}
		}

		/// <inheritdoc />
		public async Task HandleRestart(Version updateVersion, CancellationToken cancellationToken)
		{
			releaseServers = true;
			if (Running)
				await chat.SendWatchdogMessage("Detaching...", cancellationToken).ConfigureAwait(false);
		}
	}
}
