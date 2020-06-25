using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Api.Models;
using Tgstation.Server.Api.Models.Internal;
using Tgstation.Server.Host.Components.Chat;
using Tgstation.Server.Host.Components.Deployment;
using Tgstation.Server.Host.Components.Events;
using Tgstation.Server.Host.Components.Session;
using Tgstation.Server.Host.Core;
using Tgstation.Server.Host.Database;
using Tgstation.Server.Host.IO;
using Tgstation.Server.Host.Jobs;

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
		public override RebootState? RebootState => Status != WatchdogStatus.Offline ? (AlphaIsActive ? alphaServer?.RebootState : bravoServer?.RebootState) : null;

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
		/// <param name="chat">The <see cref="IChatManager"/> for the <see cref="WatchdogBase"/>.</param>
		/// <param name="sessionControllerFactory">The <see cref="ISessionControllerFactory"/> for the <see cref="WatchdogBase"/>.</param>
		/// <param name="dmbFactory">The <see cref="IDmbFactory"/> for the <see cref="WatchdogBase"/>.</param>
		/// <param name="reattachInfoHandler">The <see cref="IReattachInfoHandler"/> for the <see cref="WatchdogBase"/>.</param>
		/// <param name="databaseContextFactory">The <see cref="IDatabaseContextFactory"/> for the <see cref="WatchdogBase"/>.</param>
		/// <param name="jobManager">The <see cref="IJobManager"/> for the <see cref="WatchdogBase"/>.</param>
		/// <param name="serverControl">The <see cref="IServerControl"/> for the <see cref="WatchdogBase"/>.</param>
		/// <param name="asyncDelayer">The <see cref="IAsyncDelayer"/> for the <see cref="WatchdogBase"/>.</param>
		/// <param name="diagnosticsIOManager">The <see cref="IIOManager"/> for the <see cref="WatchdogBase"/>.</param>
		/// <param name="eventConsumer">The <see cref="IEventConsumer"/> for the <see cref="WatchdogBase"/>.</param>
		/// <param name="logger">The <see cref="ILogger"/> for the <see cref="WatchdogBase"/>.</param>
		/// <param name="initialLaunchParameters">The <see cref="DreamDaemonLaunchParameters"/> for the <see cref="WatchdogBase"/>.</param>
		/// <param name="instance">The <see cref="Api.Models.Instance"/> for the <see cref="WatchdogBase"/>.</param>
		/// <param name="autoStart">The autostart value for the <see cref="WatchdogBase"/>.</param>
		public ExperimentalWatchdog(
			IChatManager chat,
			ISessionControllerFactory sessionControllerFactory,
			IDmbFactory dmbFactory,
			IReattachInfoHandler reattachInfoHandler,
			IDatabaseContextFactory databaseContextFactory,
			IJobManager jobManager,
			IServerControl serverControl,
			IAsyncDelayer asyncDelayer,
			IIOManager diagnosticsIOManager,
			IEventConsumer eventConsumer,
			ILogger<ExperimentalWatchdog> logger,
			DreamDaemonLaunchParameters initialLaunchParameters,
			Api.Models.Instance instance, bool autoStart)
			: base(
				 chat,
				 sessionControllerFactory,
				 dmbFactory,
				 reattachInfoHandler,
				 databaseContextFactory,
				 jobManager,
				 serverControl,
				 asyncDelayer,
				 diagnosticsIOManager,
				 eventConsumer,
				 logger,
				 initialLaunchParameters,
				 instance,
				 autoStart)
		{
			alphaIsActive = true;
		}

		/// <inheritdoc />
		#pragma warning disable CA1502 // TODO: Decomplexify
		protected override async Task HandleMonitorWakeup(MonitorActivationReason activationReason, MonitorState monitorState, CancellationToken cancellationToken)
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
					monitorState.InactiveServer = await SessionControllerFactory.LaunchNew(
						newDmb,
						null,
						ActiveLaunchParameters,
						false,
						!monitorState.ActiveServer.IsPrimary,
						false,
						cancellationToken)
						.ConfigureAwait(false);
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
							throw new InvalidOperationException("Watchdog double crit-fail!"); // just give up, if THAT compile job is failing then the ActiveServer is gonna crash soon too or already has

						monitorState.InactiveServer = await SessionControllerFactory.LaunchNew(
							dmbBackup,
							null,
							ActiveLaunchParameters,
							false,
							!monitorState.ActiveServer.IsPrimary,
							false,
							cancellationToken)
							.ConfigureAwait(false);
						monitorState.InactiveServer.SetHighPriority();
						await Chat.SendWatchdogMessage(
							"Staging newest DMB on inactive server failed: {0} Falling back to previous dmb...",
							false,
							cancellationToken).ConfigureAwait(false);
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
						await Chat.SendWatchdogMessage(
							"Attempted reboot of inactive server failed. Watchdog will reset when active server fails or exits",
							false,
							cancellationToken).ConfigureAwait(false);
						return;
					}
				}

				Logger.LogInformation("Successfully relaunched inactive server!");
				monitorState.RebootingInactiveServer = true;
			}

			static string ExitWord(ISessionController controller) => controller.TerminationWasRequested ? "exited" : "crashed";

			// reason handling
			switch (activationReason)
			{
				case MonitorActivationReason.ActiveServerCrashed:
					if (monitorState.ActiveServer.RebootState == Session.RebootState.Shutdown)
					{
						// the time for graceful shutdown is now
						await Chat.SendWatchdogMessage(
							String.Format(
								CultureInfo.InvariantCulture,
								"Active server {0}! Shutting down due to graceful termination request...",
								ExitWord(monitorState.ActiveServer)),
							false,
							cancellationToken).ConfigureAwait(false);
						monitorState.NextAction = MonitorAction.Exit;
						break;
					}

					if (FullRestartDeadInactive())
					{
						// tell chat about it and go ahead
						await Chat.SendWatchdogMessage(
							String.Format(
								CultureInfo.InvariantCulture,
								"Active server {0}! Inactive server unable to online!",
								ExitWord(monitorState.ActiveServer)),
							false,
							cancellationToken).ConfigureAwait(false);

						// we've already been set to restart
						break;
					}

					// tell chat about it
					await Chat.SendWatchdogMessage(
						String.Format(
							CultureInfo.InvariantCulture,
							"Active server {0}! Onlining inactive server...",
							ExitWord(monitorState.ActiveServer)),
						false,
						cancellationToken)
						.ConfigureAwait(false);

					// try to activate the inactive server
					if (!await MakeInactiveActive().ConfigureAwait(false))
						break; // failing that, we've already been set to restart

					// bring up another inactive server
					await UpdateAndRestartInactiveServer(true).ConfigureAwait(false);
					break;
				case MonitorActivationReason.InactiveServerCrashed:
					// just announce and try to bring it back
					await Chat.SendWatchdogMessage(
						String.Format(
							CultureInfo.InvariantCulture,
							"Inactive server {0}! Rebooting...",
							ExitWord(monitorState.InactiveServer)),
						false,
						cancellationToken)
						.ConfigureAwait(false);
					await UpdateAndRestartInactiveServer(false).ConfigureAwait(false);
					break;
				case MonitorActivationReason.ActiveServerRebooted:
					// ideal goal: active server just closed its port
					// tell inactive server to open it's port and that's now the active server
					var rebootState = monitorState.ActiveServer.RebootState;
					monitorState.ActiveServer.ResetRebootState(); // the DMAPI has already done this internally

					if (FullRestartDeadInactive() && rebootState != Session.RebootState.Shutdown)
						break; // full restart if the inactive server is being fucky

					// what matters here is the RebootState
					var restartOnceSwapped = false;

					switch (rebootState)
					{
						case Session.RebootState.Normal:
							// life as normal
							break;
						case Session.RebootState.Restart:
							// reboot the current active server once the inactive one activates
							restartOnceSwapped = true;
							break;
						case Session.RebootState.Shutdown:
							// graceful shutdown time
							await Chat.SendWatchdogMessage(
								"Active server rebooted! Shutting down due to graceful termination request...",
								false,
								cancellationToken)
								.ConfigureAwait(false);
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
				case MonitorActivationReason.Heartbeat:
				default:
					throw new InvalidOperationException(
						String.Format(
							CultureInfo.InvariantCulture,
							"Invalid monitor activation reason: {0}!",
							activationReason));
			}
		}
#pragma warning restore CA1502

		/// <inheritdoc />
		protected override void DisposeAndNullControllersImpl()
		{
			alphaServer?.Dispose();
			alphaServer = null;
			bravoServer?.Dispose();
			bravoServer = null;
		}

		/// <inheritdoc />
		protected override IReadOnlyDictionary<MonitorActivationReason, Task> GetMonitoredServerTasks(MonitorState monitorState)
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

			return new Dictionary<MonitorActivationReason, Task>
			{
				{ MonitorActivationReason.ActiveServerCrashed, monitorState.ActiveServer.Lifetime },
				{ MonitorActivationReason.ActiveServerRebooted, monitorState.ActiveServer.OnReboot },
				{ MonitorActivationReason.InactiveServerCrashed, monitorState.InactiveServer.Lifetime },
				{ MonitorActivationReason.InactiveServerRebooted, monitorState.InactiveServer.OnReboot },
				{ MonitorActivationReason.InactiveServerStartupComplete, monitorState.InactiveServer.OnPrime }
			};
		}

		/// <inheritdoc />
		#pragma warning disable CA1502 // TODO: Decomplexify
		protected override async Task InitControllers(
			Task chatTask,
			DualReattachInformation reattachInfo,
			CancellationToken cancellationToken)
		{
			Debug.Assert(alphaServer == null && bravoServer == null, "Entered LaunchNoLock with one or more of the servers not being null!");

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
					alphaServerTask = SessionControllerFactory.LaunchNew(
						dmbToUse,
						null,
						ActiveLaunchParameters,
						true,
						true,
						false,
						cancellationToken);
				else
					alphaServerTask = SessionControllerFactory.Reattach(reattachInfo.Alpha, reattachInfo.TopicRequestTimeout, cancellationToken);

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
					bravoServer = await SessionControllerFactory.LaunchNew(
						dmbToUse,
						null,
						ActiveLaunchParameters,
						false,
						false,
						false,
						cancellationToken)
						.ConfigureAwait(false);
				else
					bravoServer = await SessionControllerFactory.Reattach(
						reattachInfo.Bravo,
						reattachInfo.TopicRequestTimeout,
						cancellationToken).ConfigureAwait(false);

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
						await ReattachFailure(chatTask, !bothServersDead, cancellationToken).ConfigureAwait(false);
						return;
					}

					// we still have the active server but the other one is dead to us, hand it off to the monitor to restart
					const string InactiveReattachFailureMessage = "Unable to reattach to inactive server. Leaving for monitor to reboot...";
					chatTask = Chat.SendWatchdogMessage(InactiveReattachFailureMessage, false, cancellationToken);
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
		protected override DualReattachInformation CreateReattachInformation()
			=> new DualReattachInformation
			{
				AlphaIsActive = AlphaIsActive,
				Alpha = alphaServer?.Release(),
				Bravo = bravoServer?.Release()
			};

		/// <inheritdoc />
		public override Task InstanceRenamed(string newInstanceName, CancellationToken cancellationToken)
			=> Task.WhenAll(
				alphaServer?.InstanceRenamed(newInstanceName, cancellationToken) ?? Task.CompletedTask,
				bravoServer?.InstanceRenamed(newInstanceName, cancellationToken) ?? Task.CompletedTask);
	}
}
