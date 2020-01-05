using Byond.TopicSender;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Api.Models.Internal;
using Tgstation.Server.Host.Components.Chat;
using Tgstation.Server.Host.Components.Compiler;
using Tgstation.Server.Host.Core;

namespace Tgstation.Server.Host.Components.Watchdog
{
	/// <summary>
	/// A <see cref="IWatchdog"/> that tries to manage 2 servers at once for maximum uptime.
	/// </summary>
	sealed class ExperimentalWatchdog : WatchdogBase
	{
		/// <summary>
		/// The time in seconds to wait from starting <see cref="alphaServer"/> to start <see cref="bravoServer"/>. Does not take responsiveness into account
		/// </summary>
		const int AlphaBravoStartupSeperationInterval = 10; // TODO: Make this configurable

		/// <inheritdoc />
		public override bool AlphaIsActive => alphaIsActive;

		/// <inheritdoc />
		public override Models.CompileJob ActiveCompileJob => (AlphaIsActive ? alphaServer : bravoServer)?.Dmb.CompileJob;

		/// <inheritdoc />
		public override RebootState? RebootState => Running ? (AlphaIsActive ? alphaServer?.RebootState : bravoServer?.RebootState) : null;

		/// <summary>
		/// Server designation alpha
		/// </summary>
		ISessionController alphaServer;

		/// <summary>
		/// Server designation bravo
		/// </summary>
		ISessionController bravoServer;

		/// <summary>
		/// Backing field for <see cref="AlphaIsActive"/>.
		/// </summary>
		bool alphaIsActive;

		/// <summary>
		/// Initializes a new instance of the <see cref="ExperimentalWatchdog"/> <see langword="class"/>.
		/// </summary>
		/// <param name="chat">The <see cref="IChat"/> for the <see cref="WatchdogBase"/>.</param>
		/// <param name="sessionControllerFactory">The <see cref="ISessionControllerFactory"/> for the <see cref="WatchdogBase"/>.</param>
		/// <param name="dmbFactory">The <see cref="IDmbFactory"/> for the <see cref="WatchdogBase"/>.</param>
		/// <param name="reattachInfoHandler">The <see cref="IReattachInfoHandler"/> for the <see cref="WatchdogBase"/>.</param>
		/// <param name="databaseContextFactory">The <see cref="IDatabaseContextFactory"/> for the <see cref="WatchdogBase"/>.</param>
		/// <param name="byondTopicSender">The <see cref="IByondTopicSender"/> for the <see cref="WatchdogBase"/>.</param>
		/// <param name="eventConsumer">The <see cref="IEventConsumer"/> for the <see cref="WatchdogBase"/>.</param>
		/// <param name="jobManager">The <see cref="IJobManager"/> for the <see cref="WatchdogBase"/>.</param>
		/// <param name="serverControl">The <see cref="IServerControl"/> for the <see cref="WatchdogBase"/>.</param>
		/// <param name="asyncDelayer">The <see cref="IAsyncDelayer"/> for the <see cref="WatchdogBase"/>.</param>
		/// <param name="logger">The <see cref="ILogger"/> for the <see cref="WatchdogBase"/>.</param>
		/// <param name="initialLaunchParameters">The <see cref="DreamDaemonLaunchParameters"/> for the <see cref="WatchdogBase"/>.</param>
		/// <param name="instance">The <see cref="Api.Models.Instance"/> for the <see cref="WatchdogBase"/>.</param>
		/// <param name="autoStart">The autostart value for the <see cref="WatchdogBase"/>.</param>
		public ExperimentalWatchdog(IChat chat, ISessionControllerFactory sessionControllerFactory, IDmbFactory dmbFactory, IReattachInfoHandler reattachInfoHandler, IDatabaseContextFactory databaseContextFactory, IByondTopicSender byondTopicSender, IEventConsumer eventConsumer, IJobManager jobManager, IServerControl serverControl, IAsyncDelayer asyncDelayer, ILogger<ExperimentalWatchdog> logger, DreamDaemonLaunchParameters initialLaunchParameters, Api.Models.Instance instance, bool autoStart)
			: base(
				 chat,
				 sessionControllerFactory,
				 dmbFactory,
				 reattachInfoHandler,
				 databaseContextFactory,
				 byondTopicSender,
				 eventConsumer,
				 jobManager,
				 serverControl,
				 asyncDelayer,
				 logger,
				 initialLaunchParameters,
				 instance,
				 autoStart)
		{
			alphaIsActive = true;
		}

		/// <summary>
		/// Handles the actions to take when the monitor has to "wake up"
		/// </summary>
		/// <param name="activationReason">The <see cref="MonitorActivationReason"/> that caused the invocation</param>
		/// <param name="monitorState">The current <see cref="MonitorState"/>. Will be modified upon retrn</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		#pragma warning disable CA1502 // TODO: Decomplexify
		async Task HandlerMonitorWakeup(MonitorActivationReason activationReason, MonitorState monitorState, CancellationToken cancellationToken)
		{
			Logger.LogDebug("Monitor activation. Reason: {0}", activationReason);

			// this is where the bulk of the watchdog handling code lives and is fraught with lambdas, sorry not sorry
			// I'll do my best to walk you through it

			// returns true if the inactive server can't be used immediately
			// also sets monitor to restart if the above holds
			bool FullRestartDeadInactive()
			{
				if (monitorState.RebootingInactiveServer || monitorState.InactiveServerCritFail)
				{
					Logger.LogInformation("Inactive server is {0}! Restarting monitor...", monitorState.InactiveServerCritFail ? "critically failed" : "still rebooting");
					monitorState.NextAction = MonitorAction.Restart; // will dispose server
					return true;
				}

				return false;
			}

			// trys to set inactive server's port to the public game port
			// doesn't handle closing active server's port
			// returns true on success and swaps inactiveserver and activeserver also sets LastLaunchParameters to ActiveLaunchParameters
			// on failure, sets monitor to restart
			async Task<bool> MakeInactiveActive()
			{
				Logger.LogDebug("Setting inactive server to port {0}...", ActiveLaunchParameters.PrimaryPort.Value);
				var result = await monitorState.InactiveServer.SetPort(ActiveLaunchParameters.PrimaryPort.Value, cancellationToken).ConfigureAwait(false);

				if (!result)
				{
					Logger.LogWarning("Failed to activate inactive server! Restarting monitor...");
					monitorState.NextAction = MonitorAction.Restart; // will dispose server
					return false;
				}

				// inactive server should always be using active launch parameters
				LastLaunchParameters = ActiveLaunchParameters;

				var tmp = monitorState.ActiveServer;
				monitorState.ActiveServer = monitorState.InactiveServer;
				monitorState.InactiveServer = tmp;
				alphaIsActive = !AlphaIsActive;
				monitorState.ActiveServer.EnableCustomChatCommands();
				return true;
			}

			// Kills and tries to launch inactive server with the latest dmb
			// falls back to current dmb on failure
			// Sets critfail on inactive server failing that
			// returns false if the backup dmb was used successfully, true otherwise
			async Task UpdateAndRestartInactiveServer(bool breakAfter)
			{
				ActiveParametersUpdated = new TaskCompletionSource<object>();
				monitorState.InactiveServer.Dispose(); // kill or recycle it
				var desiredNextAction = breakAfter ? MonitorAction.Break : MonitorAction.Continue;
				monitorState.NextAction = desiredNextAction;

				Logger.LogInformation("Rebooting inactive server...");
				var newDmb = DmbFactory.LockNextDmb(1);
				try
				{
					monitorState.InactiveServer = await SessionControllerFactory.LaunchNew(ActiveLaunchParameters, newDmb, null, false, !monitorState.ActiveServer.IsPrimary, false, cancellationToken).ConfigureAwait(false);
					monitorState.InactiveServer.SetHighPriority();
				}
				catch (OperationCanceledException)
				{
					throw;
				}
				catch (Exception e)
				{
					Logger.LogError("Error occurred while recreating server! Attempting backup strategy of running DMB of running server! Exception: {0}", e.ToString());

					// ahh jeez, what do we do here?
					// this is our fault, so it should never happen but
					// idk maybe a database error while handling the newest dmb?
					// either way try to start it using the active server's dmb as a backup
					try
					{
						var dmbBackup = await DmbFactory.FromCompileJob(monitorState.ActiveServer.Dmb.CompileJob, cancellationToken).ConfigureAwait(false);

						if (dmbBackup == null) // NANI!?
							throw new JobException("Creating backup DMB provider failed!"); // just give up, if THAT compile job is failing then the ActiveServer is gonna crash soon too or already has

						monitorState.InactiveServer = await SessionControllerFactory.LaunchNew(ActiveLaunchParameters, dmbBackup, null, false, !monitorState.ActiveServer.IsPrimary, false, cancellationToken).ConfigureAwait(false);
						monitorState.InactiveServer.SetHighPriority();
						await Chat.SendWatchdogMessage("Staging newest DMB on inactive server failed: {0} Falling back to previous dmb...", cancellationToken).ConfigureAwait(false);
					}
					catch (OperationCanceledException)
					{
						throw;
					}
					catch (Exception e2)
					{
						// fuuuuucckkk
						Logger.LogError("Backup strategy failed! Monitor will restart when active server reboots! Exception: {0}", e2.ToString());
						monitorState.InactiveServerCritFail = true;
						await Chat.SendWatchdogMessage("Attempted reboot of inactive server failed. Watchdog will reset when active server fails or exits", cancellationToken).ConfigureAwait(false);
						return;
					}
				}

				Logger.LogInformation("Successfully relaunched inactive server!");
				monitorState.RebootingInactiveServer = true;
			}

			string ExitWord(ISessionController controller) => controller.TerminationWasRequested ? "exited" : "crashed";

			// reason handling
			switch (activationReason)
			{
				case MonitorActivationReason.ActiveServerCrashed:
					if (monitorState.ActiveServer.RebootState == Watchdog.RebootState.Shutdown)
					{
						// the time for graceful shutdown is now
						await Chat.SendWatchdogMessage(String.Format(CultureInfo.InvariantCulture, "Active server {0}! Exiting due to graceful termination request...", ExitWord(monitorState.ActiveServer)), cancellationToken).ConfigureAwait(false);
						DisposeAndNullControllers();
						monitorState.NextAction = MonitorAction.Exit;
						break;
					}

					if (FullRestartDeadInactive())
					{
						// tell chat about it and go ahead
						await Chat.SendWatchdogMessage(String.Format(CultureInfo.InvariantCulture, "Active server {0}! Inactive server unable to online!", ExitWord(monitorState.ActiveServer)), cancellationToken).ConfigureAwait(false);

						// we've already been set to restart
						break;
					}

					// tell chat about it
					await Chat.SendWatchdogMessage(String.Format(CultureInfo.InvariantCulture, "Active server {0}! Onlining inactive server...", ExitWord(monitorState.ActiveServer)), cancellationToken).ConfigureAwait(false);

					// try to activate the inactive server
					if (!await MakeInactiveActive().ConfigureAwait(false))
						break; // failing that, we've already been set to restart

					// bring up another inactive server
					await UpdateAndRestartInactiveServer(true).ConfigureAwait(false);
					break;
				case MonitorActivationReason.InactiveServerCrashed:
					// just announce and try to bring it back
					await Chat.SendWatchdogMessage(String.Format(CultureInfo.InvariantCulture, "Inactive server {0}! Rebooting...", ExitWord(monitorState.InactiveServer)), cancellationToken).ConfigureAwait(false);
					await UpdateAndRestartInactiveServer(false).ConfigureAwait(false);
					break;
				case MonitorActivationReason.ActiveServerRebooted:
					// ideal goal: active server just closed its port
					// tell inactive server to open it's port and that's now the active server
					var rebootState = monitorState.ActiveServer.RebootState;
					monitorState.ActiveServer.ResetRebootState(); // the DMAPI has already done this internally

					if (FullRestartDeadInactive() && rebootState != Watchdog.RebootState.Shutdown)
						break; // full restart if the inactive server is being fucky

					// what matters here is the RebootState
					var restartOnceSwapped = false;

					switch (rebootState)
					{
						case Watchdog.RebootState.Normal:
							// life as normal
							break;
						case Watchdog.RebootState.Restart:
							// reboot the current active server once the inactive one activates
							restartOnceSwapped = true;
							break;
						case Watchdog.RebootState.Shutdown:
							// graceful shutdown time
							await Chat.SendWatchdogMessage("Active server rebooted! Exiting due to graceful termination request...", cancellationToken).ConfigureAwait(false);
							DisposeAndNullControllers();
							monitorState.NextAction = MonitorAction.Exit;
							return;
						default:
							throw new InvalidOperationException($"Invalid reboot state: {rebootState}");
					}

					// are both servers now running the same CompileJob?
					var sameCompileJob = monitorState.InactiveServer.Dmb.CompileJob.Id == monitorState.ActiveServer.Dmb.CompileJob.Id;

					if (!sameCompileJob || ActiveLaunchParameters != LastLaunchParameters)
						restartOnceSwapped = true; // need a new launch to update either settings or compile job

					if (restartOnceSwapped)
						/*
						 * we need to manually restart active server
						 * just kill it here, easier that way
						 */
						monitorState.ActiveServer.Dispose();

					var activeServerStillHasPortOpen = !restartOnceSwapped && !monitorState.ActiveServer.ClosePortOnReboot;

					if (activeServerStillHasPortOpen)
						/* we didn't want active server to swap for some reason and it still has it's port open
						 * just continue as normal
						 */
						break;

					if (!await MakeInactiveActive().ConfigureAwait(false))
						break; // monitor will restart

					// servers now swapped
					// enable this now if inactive server is not still valid
					monitorState.ActiveServer.ClosePortOnReboot = restartOnceSwapped;

					if (!restartOnceSwapped)
						/*
						 * now try to reopen it on the private port
						 * failing that, just reboot it
						 */
						restartOnceSwapped = !await monitorState.InactiveServer.SetPort(ActiveLaunchParameters.SecondaryPort.Value, cancellationToken).ConfigureAwait(false);

					// break either way because any issues past this point would be solved by the reboot
					if (restartOnceSwapped) // for one reason or another
						await UpdateAndRestartInactiveServer(true).ConfigureAwait(false); // update and reboot
					else
						monitorState.NextAction = MonitorAction.Skip; // only skip checking inactive server rebooted, it's guaranteed InactiveServerStartup complete wouldn't fire this iteration
					break;
				case MonitorActivationReason.InactiveServerRebooted:
					// just don't let the active server close it's port if the inactive server isn't ready
					monitorState.RebootingInactiveServer = true;
					monitorState.InactiveServer.ResetRebootState();
					monitorState.ActiveServer.ClosePortOnReboot = false;
					monitorState.NextAction = MonitorAction.Continue;
					break;
				case MonitorActivationReason.InactiveServerStartupComplete:
					// opposite of above case
					monitorState.RebootingInactiveServer = false;
					monitorState.ActiveServer.ClosePortOnReboot = true;
					monitorState.NextAction = MonitorAction.Continue;
					break;
				case MonitorActivationReason.NewDmbAvailable:
				case MonitorActivationReason.ActiveLaunchParametersUpdated:
					// just reload the inactive server and wait for a swap to apply the changes
					await UpdateAndRestartInactiveServer(true).ConfigureAwait(false);
					break;
				default:
					Trace.Assert(false, String.Format(CultureInfo.InvariantCulture, "Invalid monitor activation reason: {0}!", activationReason));
					break;
			}
		}
		#pragma warning restore CA1502

		/// <summary>
		/// Call <see cref="IDisposable.Dispose"/> on <see cref="alphaServer"/> and <see cref="bravoServer"/> and set them to <see langword="null"/>
		/// </summary>
		protected override void DisposeAndNullControllers()
		{
			alphaServer?.Dispose();
			alphaServer = null;
			bravoServer?.Dispose();
			bravoServer = null;
			Running = false;
		}

		/// <inheritdoc />
		#pragma warning disable CA1502 // TODO: Decomplexify
		protected override async Task MonitorLifetimes(CancellationToken cancellationToken)
		{
			Logger.LogTrace("Entered MonitorLifetimes");

			// this function is responsible for calling HandlerMonitorWakeup when necessary and manitaining the MonitorState
			var iteration = 1;
			for (var monitorState = new MonitorState(); monitorState.NextAction != MonitorAction.Exit; ++iteration)
			{
				// always start out with continue
				monitorState.NextAction = MonitorAction.Continue;

				// dump some info to the logs
				Logger.LogDebug("Iteration {0} of monitor loop", iteration);
				try
				{
					if (AlphaIsActive)
						Logger.LogDebug("Alpha is the active server");
					else
						Logger.LogDebug("Bravo is the active server");

					if (monitorState.RebootingInactiveServer)
						Logger.LogDebug("Inactive server is rebooting");

					// update the monitor state with the inactive/active servers
					monitorState.ActiveServer = AlphaIsActive ? alphaServer : bravoServer;
					monitorState.InactiveServer = AlphaIsActive ? bravoServer : alphaServer;

					if (monitorState.ActiveServer.ClosePortOnReboot)
						Logger.LogDebug("Active server will close port on reboot");
					if (monitorState.InactiveServer.ClosePortOnReboot)
						Logger.LogDebug("Inactive server will close port on reboot");

					Logger.LogDebug("Active server Compile Job ID: {0}", monitorState.ActiveServer.Dmb.CompileJob.Id);
					Logger.LogDebug("Inactive server Compile Job ID: {0}", monitorState.InactiveServer.Dmb.CompileJob.Id);

					// load the activation tasks into local variables
					Task activeServerLifetime = monitorState.ActiveServer.Lifetime;
					Task inactiveServerLifetime = monitorState.InactiveServer.Lifetime;
					var activeServerReboot = monitorState.ActiveServer.OnReboot;
					var inactiveServerReboot = monitorState.InactiveServer.OnReboot;
					Task inactiveServerStartup = monitorState.RebootingInactiveServer ? monitorState.InactiveServer.LaunchResult : null;
					Task activeLaunchParametersChanged = ActiveParametersUpdated.Task;
					var newDmbAvailable = DmbFactory.OnNewerDmb;

					// cancel waiting if requested
					var cancelTcs = new TaskCompletionSource<object>();
					using (cancellationToken.Register(() => cancelTcs.SetCanceled()))
					{
						var toWaitOn = Task.WhenAny(activeServerLifetime, inactiveServerLifetime, activeServerReboot, inactiveServerReboot, newDmbAvailable, cancelTcs.Task, activeLaunchParametersChanged);
						if (monitorState.RebootingInactiveServer)
							toWaitOn = Task.WhenAny(toWaitOn, inactiveServerStartup);

						// wait for something to happen
						await toWaitOn.ConfigureAwait(false);
						cancellationToken.ThrowIfCancellationRequested();
					}

					var chatTask = Task.CompletedTask;
					using (await SemaphoreSlimContext.Lock(Semaphore, cancellationToken).ConfigureAwait(false))
					{
						// always run HandleMonitorWakeup from the context of the semaphore lock
						// multiple things may have happened, handle them one at a time
						for (var moreActivationsToProcess = true; moreActivationsToProcess && (monitorState.NextAction == MonitorAction.Continue || monitorState.NextAction == MonitorAction.Skip);)
						{
							MonitorActivationReason activationReason = default; // this will always be assigned before being used

							// process the tasks in this order and call HandlerMonitorWakup for each
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
							}

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

						// writeback alphaServer and bravoServer from monitor state in case they changesd
						alphaServer = AlphaIsActive ? monitorState.ActiveServer : monitorState.InactiveServer;
						bravoServer = !AlphaIsActive ? monitorState.ActiveServer : monitorState.InactiveServer;
					}

					// full reboot required
					if (monitorState.NextAction == MonitorAction.Restart)
					{
						Logger.LogDebug("Next state action is to restart");
						DisposeAndNullControllers();
						chatTask = Chat.SendWatchdogMessage("Restarting entirely due to complications...", cancellationToken);

						for (var retryAttempts = 1; monitorState.NextAction == MonitorAction.Restart; ++retryAttempts)
						{
							Exception launchException = null;
							using (await SemaphoreSlimContext.Lock(Semaphore, cancellationToken).ConfigureAwait(false))
								try
								{
									// use LaunchImplNoLock without announcements or restarting the monitor
									await LaunchImplNoLock(false, false, null, cancellationToken).ConfigureAwait(false);
									if (Running)
									{
										Logger.LogDebug("Relaunch successful, resetting monitor state...");
										monitorState = new MonitorState(); // clean the slate and continue
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
									Logger.LogWarning("Failed to automatically restart the watchdog! Attempt: {0}", retryAttempts);
								else
									Logger.LogWarning("Failed to automatically restart the watchdog! Attempt: {0}, Exception: {1}", retryAttempts, launchException);
								var retryDelay = Math.Min(Math.Pow(2, retryAttempts), 3600); // max of one hour, increasing by a power of 2 each time
								chatTask = Chat.SendWatchdogMessage(String.Format(CultureInfo.InvariantCulture, "Failed to restart watchdog (Attempt: {0}), retrying in {1} seconds...", retryAttempts, retryDelay), cancellationToken);
								await Task.WhenAll(AsyncDelayer.Delay(TimeSpan.FromSeconds(retryDelay), cancellationToken), chatTask).ConfigureAwait(false);
							}
						}
					}
				}
				catch (OperationCanceledException)
				{
					Logger.LogDebug("Monitor cancelled");
					break;
				}
				catch (Exception e)
				{
					// really, this should NEVER happen
					Logger.LogError("Monitor crashed! Iteration: {0}, State: {1}, Exception: {2}", iteration, JsonConvert.SerializeObject(monitorState), e);
					await Chat.SendWatchdogMessage(String.Format(CultureInfo.InvariantCulture, "Monitor crashed, this should NEVER happen! Please report this, full details in logs! Restarting monitor... Error: {0}", e.Message), cancellationToken).ConfigureAwait(false);
				}
			}

			Logger.LogTrace("Monitor exiting...");
		}
		#pragma warning restore CA1502

		/// <inheritdoc />
		#pragma warning disable CA1502 // TODO: Decomplexify
		protected override async Task InitControllers(Action callBeforeRecurse, Task chatTask, WatchdogReattachInformation reattachInfo, CancellationToken cancellationToken)
		{
			// good ole sanity, should never fucking trigger but i don't trust myself even though I should
			// TODO: Unit test this instead?
			if (alphaServer != null || bravoServer != null)
				throw new InvalidOperationException("Entered LaunchNoLock with one or more of the servers not being null!");

			// don't need a new dmb if reattaching
			var doesntNeedNewDmb = reattachInfo?.Alpha != null && reattachInfo?.Bravo != null;
			var dmbToUse = doesntNeedNewDmb ? null : DmbFactory.LockNextDmb(2);

			// if this try catches something, both servers are killed
			try
			{
				// start the alpha server task, either by launch a new process or attaching to an existing one
				// The tasks returned are mainly for writing interop files to the directories among other things and should generally never fail
				// The tasks pertaining to server startup times are in the ISessionControllers
				Task<ISessionController> alphaServerTask;
				if (!doesntNeedNewDmb)
					alphaServerTask = SessionControllerFactory.LaunchNew(ActiveLaunchParameters, dmbToUse, null, true, true, false, cancellationToken);
				else
					alphaServerTask = SessionControllerFactory.Reattach(reattachInfo.Alpha, cancellationToken);

				// retrieve the session controller
				var startTime = DateTimeOffset.Now;
				alphaServer = await alphaServerTask.ConfigureAwait(false);

				// failed reattaches will return null
				alphaServer?.SetHighPriority();

				// extra delay for total ordering
				var now = DateTimeOffset.Now;
				var delay = now - startTime;

				// definitely not if reattaching though
				if (reattachInfo == null && delay.TotalSeconds < AlphaBravoStartupSeperationInterval)
					await AsyncDelayer.Delay(startTime.AddSeconds(AlphaBravoStartupSeperationInterval) - now, cancellationToken).ConfigureAwait(false);

				// now bring bravo up
				if (!doesntNeedNewDmb)
					bravoServer = await SessionControllerFactory.LaunchNew(ActiveLaunchParameters, dmbToUse, null, false, false, false, cancellationToken).ConfigureAwait(false);
				else
					bravoServer = await SessionControllerFactory.Reattach(reattachInfo.Bravo, cancellationToken).ConfigureAwait(false);

				// failed reattaches will return null
				bravoServer?.SetHighPriority();

				// possiblity of null servers due to failed reattaches
				if (alphaServer == null || bravoServer == null)
				{
					await chatTask.ConfigureAwait(false);
					var bothServersDead = alphaServer == null && bravoServer == null;
					if (bothServersDead
						|| (alphaServer == null && reattachInfo.AlphaIsActive)
						|| (bravoServer == null && !reattachInfo.AlphaIsActive))
					{
						// we lost the active server, just restart entirely
						DisposeAndNullControllers();
						const string FailReattachMessage = "Unable to properly reattach to active server! Restarting...";
						Logger.LogWarning(FailReattachMessage);
						Logger.LogDebug(bothServersDead ? "Also could not reattach to inactive server!" : "Inactive server was reattached successfully!");
						chatTask = Chat.SendWatchdogMessage(FailReattachMessage, cancellationToken);
						callBeforeRecurse();
						await LaunchImplNoLock(true, false, null, cancellationToken).ConfigureAwait(false);
						await chatTask.ConfigureAwait(false);
						return;
					}

					// we still have the active server but the other one is dead to us, hand it off to the monitor to restart
					const string InactiveReattachFailureMessage = "Unable to reattach to inactive server. Leaving for monitor to reboot...";
					chatTask = Chat.SendWatchdogMessage(InactiveReattachFailureMessage, cancellationToken);
					Logger.LogWarning(InactiveReattachFailureMessage);

					if (reattachInfo.AlphaIsActive)
						bravoServer = SessionControllerFactory.CreateDeadSession(reattachInfo.Bravo.Dmb);
					else
						alphaServer = SessionControllerFactory.CreateDeadSession(reattachInfo.Alpha.Dmb);
				}

				var alphaLrt = CheckLaunchResult(alphaServer, "Alpha", cancellationToken);
				var bravoLrt = CheckLaunchResult(bravoServer, "Bravo", cancellationToken);

				// this task completes when both serers have finished booting
				var allTask = Task.WhenAll(alphaLrt, bravoLrt);

				await allTask.ConfigureAwait(false);

				// both servers are now running, alpha is the active server(unless reattach), huzzah
				alphaIsActive = reattachInfo?.AlphaIsActive ?? true;

				var activeServer = AlphaIsActive ? alphaServer : bravoServer;
				activeServer.EnableCustomChatCommands();
				activeServer.ClosePortOnReboot = true;
			}
			catch
			{
				if (dmbToUse != null)
				{
					// we locked 2 dmbs
					if (bravoServer == null)
					{
						// bravo didn't get control of his
						dmbToUse.Dispose();
						if (alphaServer == null)
							dmbToUse.Dispose(); // alpha didn't get control of his
					}
				}
				else if (doesntNeedNewDmb) // we have reattachInfo
					if (bravoServer == null)
					{
						// bravo didn't get control of his
						reattachInfo.Bravo?.Dmb.Dispose();
						if (alphaServer == null)
							reattachInfo.Alpha?.Dmb.Dispose(); // alpha didn't get control of his
					}

				// kill the controllers
				DisposeAndNullControllers();
				throw;
			}
		}
		#pragma warning restore CA1502

		/// <inheritdoc />
		protected override ISessionController GetActiveController() => AlphaIsActive ? alphaServer : bravoServer;

		/// <inheritdoc />
		protected override WatchdogReattachInformation CreateReattachInformation()
			=> new WatchdogReattachInformation
			{
				AlphaIsActive = AlphaIsActive,
				Alpha = alphaServer?.Release(),
				Bravo = bravoServer?.Release()
			};
	}
}
