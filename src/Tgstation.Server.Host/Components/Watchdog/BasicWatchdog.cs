using Byond.TopicSender;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Api.Models.Internal;
using Tgstation.Server.Host.Components.Chat;
using Tgstation.Server.Host.Components.Deployment;
using Tgstation.Server.Host.Core;
using Tgstation.Server.Host.Database;
using Tgstation.Server.Host.Jobs;

namespace Tgstation.Server.Host.Components.Watchdog
{
	/// <summary>
	/// A <see cref="IWatchdog"/> that manages one server.
	/// </summary>
	class BasicWatchdog : WatchdogBase
	{
		/// <inheritdoc />
		public sealed override bool AlphaIsActive => true;

		/// <inheritdoc />
		public sealed override Models.CompileJob ActiveCompileJob => Server?.Dmb.CompileJob;

		/// <inheritdoc />
		protected override string DeploymentTimeWhileRunning => "immediately";

		/// <inheritdoc />
		public sealed override RebootState? RebootState => Server?.RebootState;

		/// <summary>
		/// The single <see cref="ISessionController"/>.
		/// </summary>
		protected ISessionController Server { get; private set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="BasicWatchdog"/> <see langword="class"/>.
		/// </summary>
		/// <param name="chat">The <see cref="IChatManager"/> for the <see cref="WatchdogBase"/>.</param>
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
		public BasicWatchdog(
			IChatManager chat,
			ISessionControllerFactory sessionControllerFactory,
			IDmbFactory dmbFactory,
			IReattachInfoHandler reattachInfoHandler,
			IDatabaseContextFactory databaseContextFactory,
			IByondTopicSender byondTopicSender,
			IEventConsumer eventConsumer,
			IJobManager jobManager,
			IServerControl serverControl,
			IAsyncDelayer asyncDelayer,
			ILogger<BasicWatchdog> logger,
			DreamDaemonLaunchParameters initialLaunchParameters,
			Api.Models.Instance instance,
			bool autoStart)
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
		{ }

		async Task<MonitorAction> HandleMonitorWakeup(MonitorActivationReason reason, CancellationToken cancellationToken)
		{
			switch (reason)
			{
				case MonitorActivationReason.ActiveServerCrashed:
					string exitWord = Server.TerminationWasRequested ? "exited" : "crashed";
					if (Server.RebootState == Watchdog.RebootState.Shutdown)
					{
						// the time for graceful shutdown is now
						await Chat.SendWatchdogMessage(String.Format(CultureInfo.InvariantCulture, "Server {0}! Exiting due to graceful termination request...", exitWord), cancellationToken).ConfigureAwait(false);
						DisposeAndNullControllers();
						return MonitorAction.Exit;
					}

					await Chat.SendWatchdogMessage(String.Format(CultureInfo.InvariantCulture, "Server {0}! Rebooting...", exitWord), cancellationToken).ConfigureAwait(false);
					return MonitorAction.Restart;
				case MonitorActivationReason.ActiveServerRebooted:
					var rebootState = Server.RebootState;
					Server.ResetRebootState();

					switch (rebootState)
					{
						case Watchdog.RebootState.Normal:
							return HandleNormalReboot();
						case Watchdog.RebootState.Restart:
							return MonitorAction.Restart;
						case Watchdog.RebootState.Shutdown:
							// graceful shutdown time
							await Chat.SendWatchdogMessage("Active server rebooted! Exiting due to graceful termination request...", cancellationToken).ConfigureAwait(false);
							DisposeAndNullControllers();
							return MonitorAction.Exit;
						default:
							throw new InvalidOperationException($"Invalid reboot state: {rebootState}");
					}

				case MonitorActivationReason.ActiveLaunchParametersUpdated:
					await Server.SetRebootState(Watchdog.RebootState.Restart, cancellationToken).ConfigureAwait(false);
					return MonitorAction.Continue;
				case MonitorActivationReason.NewDmbAvailable:
					await HandleNewDmbAvailable(cancellationToken).ConfigureAwait(false);
					return MonitorAction.Continue;
				case MonitorActivationReason.InactiveServerCrashed:
				case MonitorActivationReason.InactiveServerRebooted:
				case MonitorActivationReason.InactiveServerStartupComplete:
					throw new NotSupportedException($"Unsupported activation reason: {reason}");
				default:
					throw new InvalidOperationException($"Invalid activation reason: {reason}");
			}
		}

		/// <inheritdoc />
		protected sealed override WatchdogReattachInformation CreateReattachInformation()
			=> new WatchdogReattachInformation
			{
				AlphaIsActive = true,
				Alpha = Server?.Release()
			};

		/// <inheritdoc />
		protected override void DisposeAndNullControllers()
		{
			Server?.Dispose();
			Server = null;
			Running = false;
		}

		/// <inheritdoc />
		protected sealed override ISessionController GetActiveController() => Server;

		/// <inheritdoc />
		protected sealed override async Task InitControllers(Action callBeforeRecurse, Task chatTask, WatchdogReattachInformation reattachInfo, CancellationToken cancellationToken)
		{
			Debug.Assert(Server == null, "Entered LaunchNoLock with server not being null!");

			// don't need a new dmb if reattaching
			var doesntNeedNewDmb = reattachInfo?.Alpha != null && reattachInfo?.Bravo != null;
			var dmbToUse = doesntNeedNewDmb ? null : DmbFactory.LockNextDmb(1);

			var serverToReattach = reattachInfo?.Alpha ?? reattachInfo?.Bravo;
			var serverToKill = reattachInfo?.Bravo ?? reattachInfo?.Alpha;

			// vice versa
			if (reattachInfo?.AlphaIsActive == false)
			{
				var temp = serverToReattach;
				serverToReattach = serverToKill;
				serverToKill = temp;
			}

			// if this try catches something, both servers are killed
			bool inactiveServerWasKilled = false;
			try
			{
				// start the alpha server task, either by launch a new process or attaching to an existing one
				// The tasks returned are mainly for writing interop files to the directories among other things and should generally never fail
				// The tasks pertaining to server startup times are in the ISessionControllers
				Task<ISessionController> serverLaunchTask, inactiveReattachTask;
				if (!doesntNeedNewDmb)
				{
					dmbToUse = await PrepServerForLaunch(dmbToUse, cancellationToken).ConfigureAwait(false);
					serverLaunchTask = SessionControllerFactory.LaunchNew(
						dmbToUse,
						null,
						ActiveLaunchParameters,
						true,
						true,
						false,
						cancellationToken);
				}
				else
					serverLaunchTask = SessionControllerFactory.Reattach(serverToReattach, cancellationToken);

				bool thereIsAnInactiveServerToKill = serverToKill != null;
				if (thereIsAnInactiveServerToKill)
					inactiveReattachTask = SessionControllerFactory.Reattach(serverToKill, cancellationToken);
				else
					inactiveReattachTask = Task.FromResult<ISessionController>(null);

				// retrieve the session controller
				Server = await serverLaunchTask.ConfigureAwait(false);

				// failed reattaches will return null
				Server?.SetHighPriority();

				var inactiveServerController = await inactiveReattachTask.ConfigureAwait(false);
				inactiveServerController?.Dispose();
				inactiveServerWasKilled = inactiveServerController != null;

				// possiblity of null servers due to failed reattaches
				if (Server == null)
				{
					callBeforeRecurse();
					await NotifyOfFailedReattach(thereIsAnInactiveServerToKill && !inactiveServerWasKilled, cancellationToken).ConfigureAwait(false);
					return;
				}

				await CheckLaunchResult(Server, "Server", cancellationToken).ConfigureAwait(false);

				Server.EnableCustomChatCommands();
			}
			catch
			{
				// kill the controllers
				bool serverWasActive = Server != null;
				DisposeAndNullControllers();

				// server didn't get control of this dmb
				if (dmbToUse != null && !serverWasActive)
					dmbToUse.Dispose();

				if (serverToKill != null && !inactiveServerWasKilled)
					serverToKill.Dmb.Dispose();
				throw;
			}
		}

		/// <inheritdoc />
		protected sealed override async Task MonitorLifetimes(CancellationToken cancellationToken)
		{
			Logger.LogTrace("Entered MonitorLifetimes");

			// this function is responsible for calling HandlerMonitorWakeup when necessary and manitaining the MonitorState
			var iteration = 1;
			for (MonitorAction nextAction = MonitorAction.Continue; nextAction != MonitorAction.Exit; ++iteration)
			{
				// always start out with continue
				nextAction = MonitorAction.Continue;

				// dump some info to the logs
				Logger.LogDebug("Iteration {0} of monitor loop", iteration);
				try
				{
					Logger.LogDebug("Server Compile Job ID: {0}", Server.Dmb.CompileJob.Id);

					// load the activation tasks into local variables
					Task activeServerLifetime = Server.Lifetime;
					var activeServerReboot = Server.OnReboot;
					Task activeLaunchParametersChanged = ActiveParametersUpdated.Task;
					var newDmbAvailable = DmbFactory.OnNewerDmb;

					// cancel waiting if requested
					var cancelTcs = new TaskCompletionSource<object>();
					using (cancellationToken.Register(() => cancelTcs.SetCanceled()))
					{
						var toWaitOn = Task.WhenAny(activeServerLifetime, activeServerReboot, newDmbAvailable, cancelTcs.Task, activeLaunchParametersChanged);

						// wait for something to happen
						await toWaitOn.ConfigureAwait(false);
						cancellationToken.ThrowIfCancellationRequested();
					}

					var chatTask = Task.CompletedTask;
					using (await SemaphoreSlimContext.Lock(Semaphore, cancellationToken).ConfigureAwait(false))
					{
						// always run HandleMonitorWakeup from the context of the semaphore lock
						// multiple things may have happened, handle them one at a time
						for (var moreActivationsToProcess = true; moreActivationsToProcess && (nextAction == MonitorAction.Continue || nextAction == MonitorAction.Skip);)
						{
							MonitorActivationReason activationReason = default; // this will always be assigned before being used

							// process the tasks in this order and call HandlerMonitorWakup for each
							bool CheckActivationReason(ref Task task, MonitorActivationReason testActivationReason)
							{
								var taskCompleted = task?.IsCompleted == true;
								task = null;
								if (nextAction == MonitorAction.Skip)
									nextAction = MonitorAction.Continue;
								else if (taskCompleted)
								{
									activationReason = testActivationReason;
									return true;
								}

								return false;
							}

							if (CheckActivationReason(ref activeServerLifetime, MonitorActivationReason.ActiveServerCrashed)
								|| CheckActivationReason(ref activeServerReboot, MonitorActivationReason.ActiveServerRebooted)
								|| CheckActivationReason(ref newDmbAvailable, MonitorActivationReason.NewDmbAvailable)
								|| CheckActivationReason(ref activeLaunchParametersChanged, MonitorActivationReason.ActiveLaunchParametersUpdated))
							{
								Logger.LogTrace("Monitor activation: {0}", activationReason);
								nextAction = await HandleMonitorWakeup(activationReason, cancellationToken).ConfigureAwait(false);
							}
							else
								moreActivationsToProcess = false;
						}
					}

					// full reboot required
					if (nextAction == MonitorAction.Restart)
					{
						Logger.LogDebug("Next state action is to restart");
						DisposeAndNullControllers();

						for (var retryAttempts = 1; nextAction == MonitorAction.Restart; ++retryAttempts)
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
										break; // continue on main loop
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
					Logger.LogError("Monitor crashed! Iteration: {0}, NextAction: {1}, Exception: {2}", iteration, nextAction, e);
					await Chat.SendWatchdogMessage(String.Format(CultureInfo.InvariantCulture, "Monitor crashed, this should NEVER happen! Please report this, full details in logs! Restarting monitor... Error: {0}", e.Message), cancellationToken).ConfigureAwait(false);
				}
			}

			Logger.LogTrace("Monitor exiting...");
		}

		/// <summary>
		/// Handler for <see cref="MonitorActivationReason.ActiveServerRebooted"/> when the <see cref="RebootState"/> is <see cref="RebootState.Normal"/>.
		/// </summary>
		/// <returns>The <see cref="MonitorAction"/> to take.</returns>
		protected virtual MonitorAction HandleNormalReboot()
		{
			bool dmbUpdatePending = ActiveLaunchParameters != LastLaunchParameters;
			return dmbUpdatePending ? MonitorAction.Restart : MonitorAction.Continue;
		}

		/// <summary>
		/// Handler for <see cref="MonitorActivationReason.NewDmbAvailable"/>.
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task"/> representing the running operation.</returns>
		protected virtual Task HandleNewDmbAvailable(CancellationToken cancellationToken) => Server.SetRebootState(Watchdog.RebootState.Restart, cancellationToken);

		/// <summary>
		/// Prepare the server to launch a new instance with the <see cref="WatchdogBase.ActiveLaunchParameters"/> and a given <paramref name="dmbToUse"/>.
		/// </summary>
		/// <param name="dmbToUse">The <see cref="IDmbProvider"/> to be launched. Will not be disposed by this function.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the modified <see cref="IDmbProvider"/> to be used.</returns>
		protected virtual Task<IDmbProvider> PrepServerForLaunch(IDmbProvider dmbToUse, CancellationToken cancellationToken) => Task.FromResult(dmbToUse);
	}
}
