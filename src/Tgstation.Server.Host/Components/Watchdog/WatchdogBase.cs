using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Serilog.Context;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Api.Models;
using Tgstation.Server.Api.Models.Internal;
using Tgstation.Server.Api.Rights;
using Tgstation.Server.Host.Components.Chat;
using Tgstation.Server.Host.Components.Deployment;
using Tgstation.Server.Host.Components.Events;
using Tgstation.Server.Host.Components.Interop.Topic;
using Tgstation.Server.Host.Components.Session;
using Tgstation.Server.Host.Core;
using Tgstation.Server.Host.Database;
using Tgstation.Server.Host.Extensions;
using Tgstation.Server.Host.IO;
using Tgstation.Server.Host.Jobs;

namespace Tgstation.Server.Host.Components.Watchdog
{
	/// <summary>
	/// Base class for <see cref="IWatchdog"/>s.
	/// </summary>
	#pragma warning disable CA1506 // TODO: Decomplexify
	abstract class WatchdogBase : IWatchdog, ICustomCommandHandler, IRestartHandler
	{
		/// <inheritdoc />
		public WatchdogStatus Status
		{
			get => status;
			set
			{
				status = value;
				Logger.LogTrace("Status set to {0}", status);
			}
		}

		/// <inheritdoc />
		public abstract bool AlphaIsActive { get; }

		/// <inheritdoc />
		public abstract Models.CompileJob ActiveCompileJob { get; }

		/// <inheritdoc />
		public DreamDaemonLaunchParameters ActiveLaunchParameters { get; protected set; }

		/// <inheritdoc />
		public DreamDaemonLaunchParameters LastLaunchParameters { get; protected set; }

		/// <inheritdoc />
		public abstract RebootState? RebootState { get; }

		/// <summary>
		/// <see cref="TaskCompletionSource{TResult}"/> that completes when <see cref="ActiveLaunchParameters"/> are changed and we are running
		/// </summary>
		protected TaskCompletionSource<object> ActiveParametersUpdated { get; set; }

		/// <summary>
		/// The <see cref="SemaphoreSlim"/> for the <see cref="WatchdogBase"/>.
		/// </summary>
		protected SemaphoreSlim Semaphore { get; }

		/// <summary>
		/// The <see cref="ILogger"/> for the <see cref="WatchdogBase"/>.
		/// </summary>
		protected ILogger Logger { get; }

		/// <summary>
		/// The <see cref="IChatManager"/> for the <see cref="WatchdogBase"/>
		/// </summary>
		protected IChatManager Chat { get; }

		/// <summary>
		/// The <see cref="ISessionControllerFactory"/> for the <see cref="WatchdogBase"/>
		/// </summary>
		protected ISessionControllerFactory SessionControllerFactory { get; }

		/// <summary>
		/// The <see cref="IDmbFactory"/> for the <see cref="WatchdogBase"/>
		/// </summary>
		protected IDmbFactory DmbFactory { get; }

		/// <summary>
		/// The <see cref="IAsyncDelayer"/> for the <see cref="WatchdogBase"/>.
		/// </summary>
		protected IAsyncDelayer AsyncDelayer { get; }

		/// <summary>
		/// The <see cref="Api.Models.Instance"/> for the <see cref="WatchdogBase"/>.
		/// </summary>
		readonly Api.Models.Instance instance;

		/// <summary>
		/// The <see cref="IReattachInfoHandler"/> for the <see cref="WatchdogBase"/>
		/// </summary>
		readonly IReattachInfoHandler reattachInfoHandler;

		/// <summary>
		/// The <see cref="IDatabaseContextFactory"/> for the <see cref="WatchdogBase"/>
		/// </summary>
		readonly IDatabaseContextFactory databaseContextFactory;

		/// <summary>
		/// The <see cref="IJobManager"/> for the <see cref="WatchdogBase"/>.
		/// </summary>
		readonly IJobManager jobManager;

		/// <summary>
		/// The <see cref="IRestartRegistration"/> for the <see cref="WatchdogBase"/>.
		/// </summary>
		readonly IRestartRegistration restartRegistration;

		/// <summary>
		/// The <see cref="IIOManager"/> pointing to the Diagnostics directory.
		/// </summary>
		readonly IIOManager diagnosticsIOManager;

		/// <summary>
		/// The <see cref="IEventConsumer"/> that is not the <see cref="WatchdogBase"/>
		/// </summary>
		readonly IEventConsumer eventConsumer;

		/// <summary>
		/// <see langword="lock"/> <see cref="object"/> used for <see cref="DisposeAndNullControllers"/>.
		/// </summary>
		readonly object controllerDisposeLock;

		/// <summary>
		/// If the <see cref="WatchdogBase"/> should <see cref="LaunchNoLock(bool, bool, ReattachInformation, CancellationToken)"/> in <see cref="StartAsync(CancellationToken)"/>
		/// </summary>
		readonly bool autoStart;

		/// <summary>
		/// Used when detaching servers.
		/// </summary>
		ReattachInformation releasedReattachInformation;

		/// <summary>
		/// The <see cref="CancellationTokenSource"/> for the monitor loop
		/// </summary>
		CancellationTokenSource monitorCts;

		/// <summary>
		/// The <see cref="Task"/> running the monitor loop
		/// </summary>
		Task monitorTask;

		/// <summary>
		/// Backing field for <see cref="Status"/>.
		/// </summary>
		WatchdogStatus status;

		/// <summary>
		/// The number of hearbeats missed.
		/// </summary>
		int heartbeatsMissed;

		/// <summary>
		/// If the servers should be released instead of shutdown
		/// </summary>
		bool releaseServers;

		/// <summary>
		/// If the <see cref="WatchdogBase"/> has been <see cref="Dispose"/>d.
		/// </summary>
		bool disposed;

		/// <summary>
		/// Initializes a new instance of the <see cref="WatchdogBase"/> <see langword="class"/>.
		/// </summary>
		/// <param name="chat">The value of <see cref="Chat"/></param>
		/// <param name="sessionControllerFactory">The value of <see cref="SessionControllerFactory"/></param>
		/// <param name="dmbFactory">The value of <see cref="DmbFactory"/></param>
		/// <param name="reattachInfoHandler">The value of <see cref="reattachInfoHandler"/></param>
		/// <param name="databaseContextFactory">The value of <see cref="databaseContextFactory"/></param>
		/// <param name="jobManager">The value of <see cref="jobManager"/></param>
		/// <param name="serverControl">The <see cref="IServerControl"/> to populate <see cref="restartRegistration"/> with</param>
		/// <param name="asyncDelayer">The value of <see cref="AsyncDelayer"/>.</param>
		/// <param name="diagnosticsIOManager">The value of <see cref="diagnosticsIOManager"/>.</param>
		/// <param name="eventConsumer">The value of <see cref="eventConsumer"/>.</param>
		/// <param name="logger">The value of <see cref="Logger"/></param>
		/// <param name="initialLaunchParameters">The initial value of <see cref="ActiveLaunchParameters"/>. May be modified</param>
		/// <param name="instance">The value of <see cref="instance"/></param>
		/// <param name="autoStart">The value of <see cref="autoStart"/></param>
		protected WatchdogBase(
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
			ILogger logger,
			DreamDaemonLaunchParameters initialLaunchParameters,
			Api.Models.Instance instance,
			bool autoStart)
		{
			Chat = chat ?? throw new ArgumentNullException(nameof(chat));
			SessionControllerFactory = sessionControllerFactory ?? throw new ArgumentNullException(nameof(sessionControllerFactory));
			DmbFactory = dmbFactory ?? throw new ArgumentNullException(nameof(dmbFactory));
			this.reattachInfoHandler = reattachInfoHandler ?? throw new ArgumentNullException(nameof(reattachInfoHandler));
			this.databaseContextFactory = databaseContextFactory ?? throw new ArgumentNullException(nameof(databaseContextFactory));
			this.jobManager = jobManager ?? throw new ArgumentNullException(nameof(jobManager));
			AsyncDelayer = asyncDelayer ?? throw new ArgumentNullException(nameof(asyncDelayer));
			this.diagnosticsIOManager = diagnosticsIOManager ?? throw new ArgumentNullException(nameof(diagnosticsIOManager));
			this.eventConsumer = eventConsumer ?? throw new ArgumentNullException(nameof(eventConsumer));
			Logger = logger ?? throw new ArgumentNullException(nameof(logger));
			ActiveLaunchParameters = initialLaunchParameters ?? throw new ArgumentNullException(nameof(initialLaunchParameters));
			this.instance = instance ?? throw new ArgumentNullException(nameof(instance));
			this.autoStart = autoStart;

			if (serverControl == null)
				throw new ArgumentNullException(nameof(serverControl));

			chat.RegisterCommandHandler(this);

			ActiveLaunchParameters = initialLaunchParameters;
			releaseServers = false;
			ActiveParametersUpdated = new TaskCompletionSource<object>();
			controllerDisposeLock = new object();

			restartRegistration = serverControl.RegisterForRestart(this);
			try
			{
				Semaphore = new SemaphoreSlim(1);
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
			Logger.LogTrace("Disposing...");
			Semaphore.Dispose();
			restartRegistration.Dispose();
			DisposeAndNullControllers();
			monitorCts?.Dispose();
			disposed = true;
		}

		/// <summary>
		/// Implementation of <see cref="Terminate(bool, CancellationToken)"/>. Does not lock <see cref="Semaphore"/>
		/// </summary>
		/// <param name="graceful">If <see langword="true"/> the termination will be delayed until a reboot is detected in the active server's DMAPI and this function will return immediately</param>
		/// <param name="announce">If <see langword="true"/> the termination will be announced using <see cref="Chat"/></param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task"/> representing the running operation.</returns>
		async Task TerminateNoLock(bool graceful, bool announce, CancellationToken cancellationToken)
		{
			if (Status == WatchdogStatus.Offline)
				return;
			if (!graceful)
			{
				var eventTask = eventConsumer.HandleEvent(releaseServers ? EventType.WatchdogDetach : EventType.WatchdogShutdown, null, cancellationToken);

				var chatTask = announce ? Chat.SendWatchdogMessage("Shutting down...", false, cancellationToken) : Task.CompletedTask;

				await eventTask.ConfigureAwait(false);

				await StopMonitor().ConfigureAwait(false);

				DisposeAndNullControllers();

				LastLaunchParameters = null;

				await chatTask.ConfigureAwait(false);
				return;
			}

			// merely set the reboot state
			var toKill = GetActiveController();
			if (toKill != null)
			{
				await toKill.SetRebootState(Session.RebootState.Shutdown, cancellationToken).ConfigureAwait(false);
				Logger.LogTrace("Graceful termination requested");
			}
			else
				Logger.LogTrace("Could not gracefully terminate as there is no active controller!");
		}

		/// <summary>
		/// Handles a watchdog heartbeat.
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the next <see cref="MonitorAction"/> to take.</returns>
		async Task<MonitorAction> HandleHeartbeat(CancellationToken cancellationToken)
		{
			Logger.LogTrace("Sending heartbeat to active server...");
			var activeServer = GetActiveController();
			var response = await activeServer.SendCommand(new TopicParameters(), cancellationToken).ConfigureAwait(false);

			var shouldShutdown = activeServer.RebootState == Session.RebootState.Shutdown;
			if (response == null)
			{
				switch (++heartbeatsMissed)
				{
					case 1:
						Logger.LogDebug("DEFCON 4: DreamDaemon missed first heartbeat!");
						break;
					case 2:
						var message2 = "DEFCON 3: DreamDaemon has missed 2 heartbeats!";
						Logger.LogInformation(message2);
						await Chat.SendWatchdogMessage(message2, true, cancellationToken).ConfigureAwait(false);
						break;
					case 3:
						var actionToTake = shouldShutdown
							? "shutdown"
							: "be restarted";
						var message3 = $"DEFCON 2: DreamDaemon has missed 3 heartbeats! If it does not respond to the next one, the watchdog will {actionToTake}!";
						Logger.LogWarning(message3);
						await Chat.SendWatchdogMessage(message3, false, cancellationToken).ConfigureAwait(false);
						break;
					case 4:
						var actionTaken = shouldShutdown
							? "Shutting down due to graceful termination request"
							: "Restarting";
						var message4 = $"DEFCON 1: Four heartbeats have been missed! {actionTaken}...";
						Logger.LogWarning(message4);
						await Chat.SendWatchdogMessage(message4, false, cancellationToken).ConfigureAwait(false);
						DisposeAndNullControllers();
						return shouldShutdown ? MonitorAction.Exit : MonitorAction.Restart;
					default:
						Logger.LogError("Invalid heartbeats missed count: {0}", heartbeatsMissed);
						break;
				}
			}
			else
				heartbeatsMissed = 0;

			return MonitorAction.Continue;
		}

		/// <summary>
		/// Launches the watchdog.
		/// </summary>
		/// <param name="startMonitor">If <see cref="MonitorLifetimes(CancellationToken)"/> should be started by this function</param>
		/// <param name="announce">If the launch should be announced to chat by this function</param>
		/// <param name="reattachInfo"><see cref="ReattachInformation"/> to use, if any</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		protected async Task LaunchNoLock(bool startMonitor, bool announce, ReattachInformation reattachInfo, CancellationToken cancellationToken)
		{
			Logger.LogTrace("Begin LaunchImplNoLock");

			if (reattachInfo == null && !DmbFactory.DmbAvailable)
				throw new JobException(ErrorCode.WatchdogCompileJobCorrupted);

			// this is necessary, the monitor could be in it's sleep loop trying to restart, if so cancel THAT monitor and start our own with blackjack and hookers
			Task announceTask;
			if (announce)
			{
				announceTask = Chat.SendWatchdogMessage(reattachInfo == null ? "Launching..." : "Reattaching...", false, cancellationToken); // simple announce
				if (reattachInfo == null)
					announceTask = Task.WhenAll(
						eventConsumer.HandleEvent(EventType.WatchdogLaunch, Enumerable.Empty<string>(), cancellationToken),
						announceTask);
			}
			else
				announceTask = Task.CompletedTask; // no announce

			// since neither server is running, this is safe to do
			LastLaunchParameters = ActiveLaunchParameters;
			heartbeatsMissed = 0;

			try
			{
				await InitControllers(announceTask, reattachInfo, cancellationToken).ConfigureAwait(false);
			}
			catch (OperationCanceledException)
			{
				Logger.LogTrace("Controller initialization canceled!");
				throw;
			}
			catch (Exception e)
			{
				// don't try to send chat tasks or warning logs if were suppressing exceptions or cancelled
				var originalChatTask = announceTask;
				async Task ChainChatTaskWithErrorMessage()
				{
					await originalChatTask.ConfigureAwait(false);
					await Chat.SendWatchdogMessage("Startup failed!", false, cancellationToken).ConfigureAwait(false);
				}

				announceTask = ChainChatTaskWithErrorMessage();
				Logger.LogWarning("Failed to start watchdog: {0}", e.ToString());
				throw;
			}
			finally
			{
				// finish the chat task that's in flight
				try
				{
					await announceTask.ConfigureAwait(false);
				}
				catch (OperationCanceledException)
				{
					Logger.LogTrace("Announcement task canceled!");
				}
			}

			Logger.LogInformation("Controller(s) initialized successfully");

			if (startMonitor)
			{
				monitorCts = new CancellationTokenSource();
				monitorTask = MonitorLifetimes(monitorCts.Token);
			}
		}

		/// <summary>
		/// Stops <see cref="MonitorLifetimes(CancellationToken)"/>. Doesn't kill the servers
		/// </summary>
		/// <returns><see langword="true"/> if the monitor was running, <see langword="false"/> otherwise</returns>
		protected async Task<bool> StopMonitor()
		{
			Logger.LogTrace("StopMonitor");
			if (monitorTask == null)
				return false;
			var wasRunning = !monitorTask.IsCompleted;
			monitorCts.Cancel();
			await monitorTask.ConfigureAwait(false);
			Logger.LogTrace("Stopped Monitor");
			monitorCts.Dispose();
			monitorTask = null;
			monitorCts = null;
			return wasRunning;
		}

		/// <summary>
		/// Check the <see cref="LaunchResult"/> of a given <paramref name="controller"/> for errors and throw a <see cref="JobException"/> if any are detected.
		/// </summary>
		/// <param name="controller">The <see cref="ISessionController"/> to checkou.</param>
		/// <param name="serverName">The name of the server being checked.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task"/> representing the running operation.</returns>
		protected async Task CheckLaunchResult(ISessionController controller, string serverName, CancellationToken cancellationToken)
		{
			var launchResult = await controller.LaunchResult.WithToken(cancellationToken).ConfigureAwait(false);

			// Dead sessions won't trigger this
			if (launchResult.ExitCode.HasValue) // you killed us ray...
				throw new JobException(
					ErrorCode.WatchdogStartupFailed,
					new JobException($"{serverName} failed to start: {launchResult}"));
			if (!launchResult.StartupTime.HasValue)
				throw new JobException(
					ErrorCode.WatchdogStartupTimeout,
					new JobException($"{serverName} timed out on startup: {ActiveLaunchParameters.StartupTimeout.Value}s"));
		}

		/// <summary>
		/// Call from <see cref="InitControllers(Task, ReattachInformation, CancellationToken)"/> when a reattach operation fails to attempt a fresh start.
		/// </summary>
		/// <param name="chatTask">A, possibly active, <see cref="Task"/> for an outgoing chat message.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task"/> representing the running operation.</returns>
		protected async Task ReattachFailure(Task chatTask, CancellationToken cancellationToken)
		{
			// we lost the server, just restart entirely
			DisposeAndNullControllers();
			const string FailReattachMessage = "Unable to properly reattach to server! Restarting watchdog...";
			Logger.LogWarning(FailReattachMessage);

			async Task ChainChatTask()
			{
				await chatTask.ConfigureAwait(false);
				await Chat.SendWatchdogMessage(FailReattachMessage, false, cancellationToken).ConfigureAwait(false);
			}

			await InitControllers(ChainChatTask(), null, cancellationToken).ConfigureAwait(false);
		}

		/// <summary>
		/// Call <see cref="IDisposable.Dispose"/> and null the fields for all <see cref="ISessionController"/>s.
		/// </summary>
		protected abstract void DisposeAndNullControllersImpl();

		/// <summary>
		/// Wrapper for <see cref="DisposeAndNullControllersImpl"/> under a locked context.
		/// </summary>
		protected void DisposeAndNullControllers()
		{
			Logger.LogTrace("DisposeAndNullControllers");
			lock (controllerDisposeLock)
				DisposeAndNullControllersImpl();
		}

		/// <summary>
		/// Get the active <see cref="ISessionController"/>.
		/// </summary>
		/// <returns>The active <see cref="ISessionController"/>.</returns>
		protected abstract ISessionController GetActiveController();

		/// <summary>
		/// Handles the actions to take when the monitor has to "wake up"
		/// </summary>
		/// <param name="activationReason">The <see cref="MonitorActivationReason"/> that caused the invocation. Will never be <see cref="MonitorActivationReason.Heartbeat"/>.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="MonitorAction"/> to take.</returns>
		protected abstract Task<MonitorAction> HandleMonitorWakeup(
			MonitorActivationReason activationReason,
			CancellationToken cancellationToken);

		/// <summary>
		/// Attempt to restart the monitor from scratch.
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task"/> representing the running operation.</returns>
		private async Task MonitorRestart(CancellationToken cancellationToken)
		{
			Logger.LogTrace("Monitor restart!");
			DisposeAndNullControllers();

			var chatTask = Task.CompletedTask;
			for (var retryAttempts = 1; ; ++retryAttempts)
			{
				Status = WatchdogStatus.Restoring;
				Exception launchException;
				using (await SemaphoreSlimContext.Lock(Semaphore, cancellationToken).ConfigureAwait(false))
					try
					{
						// use LaunchImplNoLock without announcements or restarting the monitor
						await LaunchNoLock(false, false, null, cancellationToken).ConfigureAwait(false);
						Logger.LogDebug("Relaunch successful, resuming monitor...");
						return;
					}
					catch (OperationCanceledException)
					{
						throw;
					}
					catch (Exception e)
					{
						launchException = e;
					}
					finally
					{
						await chatTask.ConfigureAwait(false);
					}

				Logger.LogWarning("Failed to automatically restart the watchdog! Attempt: {0}, Exception: {1}", retryAttempts, launchException);

				var retryDelay = Math.Min(
					Convert.ToInt32(
						Math.Pow(2, retryAttempts)),
					TimeSpan.FromHours(1).Seconds); // max of one hour, increasing by a power of 2 each time

				chatTask = Chat.SendWatchdogMessage(
					$"Failed to restart (Attempt: {retryAttempts}), retrying in {retryDelay}",
					false,
					cancellationToken);

				await Task.WhenAll(
					AsyncDelayer.Delay(
						TimeSpan.FromSeconds(retryDelay),
						cancellationToken),
					chatTask)
					.ConfigureAwait(false);
			}
		}

		/// <summary>
		/// The main loop of the watchdog. Ayschronously waits for events to occur and then responds to them.
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task"/> representing the running operation.</returns>
		private async Task MonitorLifetimes(CancellationToken cancellationToken)
		{
			Logger.LogTrace("Entered MonitorLifetimes");
			Status = WatchdogStatus.Online;
			using var _ = cancellationToken.Register(() => Logger.LogTrace("Monitor cancellationToken triggered"));

			// this function is responsible for calling HandlerMonitorWakeup when necessary and manitaining the MonitorState
			try
			{
				MonitorAction nextAction = MonitorAction.Continue;
				for (ulong iteration = 1; nextAction != MonitorAction.Exit; ++iteration)
					using (LogContext.PushProperty("Monitor", iteration))
						try
						{
							Logger.LogTrace("Iteration {0} of monitor loop", iteration);
							nextAction = MonitorAction.Continue;

							var controller = GetActiveController();
							Task activeServerLifetime = controller.Lifetime;
							var activeServerReboot = controller.OnReboot;

							Task activeLaunchParametersChanged = ActiveParametersUpdated.Task;
							var newDmbAvailable = DmbFactory.OnNewerDmb;

							var heartbeatSeconds = ActiveLaunchParameters.HeartbeatSeconds.Value;
							var heartbeat = heartbeatSeconds == 0
								? Extensions.TaskExtensions.InfiniteTask()
								: Task.Delay(TimeSpan.FromSeconds(heartbeatSeconds));

							// cancel waiting if requested
							var cancelTcs = new TaskCompletionSource<object>();
							var toWaitOn = Task.WhenAny(
								activeServerLifetime,
								activeServerReboot,
								heartbeat,
								newDmbAvailable,
								cancelTcs.Task,
								activeLaunchParametersChanged);

							// wait for something to happen
							using (cancellationToken.Register(() => cancelTcs.SetCanceled()))
								await toWaitOn.ConfigureAwait(false);

							cancellationToken.ThrowIfCancellationRequested();
							Logger.LogTrace("Monitor activated");

							// always run HandleMonitorWakeup from the context of the semaphore lock
							using (await SemaphoreSlimContext.Lock(Semaphore, cancellationToken).ConfigureAwait(false))
							{
								// multiple things may have happened, handle them one at a time
								for (var moreActivationsToProcess = true; moreActivationsToProcess && (nextAction == MonitorAction.Continue || nextAction == MonitorAction.Skip);)
								{
									MonitorActivationReason activationReason = default; // this will always be assigned before being used

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

									// process the tasks in this order and call HandlerMonitorWakup for each depending on the new monitorState
									var anyActivation = CheckActivationReason(ref activeServerLifetime, MonitorActivationReason.ActiveServerCrashed)
										|| CheckActivationReason(ref activeServerReboot, MonitorActivationReason.ActiveServerRebooted)
										|| CheckActivationReason(ref newDmbAvailable, MonitorActivationReason.NewDmbAvailable)
										|| CheckActivationReason(ref activeLaunchParametersChanged, MonitorActivationReason.ActiveLaunchParametersUpdated)
										|| CheckActivationReason(ref heartbeat, MonitorActivationReason.Heartbeat);

									if (!anyActivation)
										moreActivationsToProcess = false;
									else
									{
										Logger.LogTrace("Reason: {0}", activationReason);
										if (activationReason == MonitorActivationReason.Heartbeat)
											nextAction = await HandleHeartbeat(
												cancellationToken)
												.ConfigureAwait(false);
										else
											nextAction = await HandleMonitorWakeup(
												activationReason,
												cancellationToken)
												.ConfigureAwait(false);
									}
								}
							}

							Logger.LogTrace("Next monitor action is to {0}", nextAction);

							// Restart if requested
							if (nextAction == MonitorAction.Restart)
							{
								await MonitorRestart(cancellationToken).ConfigureAwait(false);
								nextAction = MonitorAction.Continue;
							}
						}
						catch (OperationCanceledException)
						{
							// let this bubble, other exceptions caught below
							throw;
						}
						catch (Exception e)
						{
							// really, this should NEVER happen
							Logger.LogError(
								"Monitor crashed! Iteration: {0}, Exception: {1}",
								iteration,
								e);

							var nextActionMessage = nextAction != MonitorAction.Exit
								? "Restarting"
								: "Shutting down";
							var chatTask = Chat.SendWatchdogMessage(
								$"Monitor crashed, this should NEVER happen! Please report this, full details in logs! {nextActionMessage}. Error: {e.Message}",
								false,
								cancellationToken);

							if (disposed)
								nextAction = MonitorAction.Exit;
							else if (nextAction != MonitorAction.Exit)
							{
								await MonitorRestart(cancellationToken).ConfigureAwait(false);
								nextAction = MonitorAction.Continue;
							}

							await chatTask.ConfigureAwait(false);
						}
			}
			catch (OperationCanceledException)
			{
				// stop signal
				Logger.LogDebug("Monitor cancelled");

				if (releaseServers)
				{
					Logger.LogTrace("Detaching servers...");
					releasedReattachInformation = GetActiveController().Release();
				}
			}

			DisposeAndNullControllers();
			Status = WatchdogStatus.Offline;

			Logger.LogTrace("Monitor exiting...");
		}

		/// <summary>
		/// Starts all <see cref="ISessionController"/>s.
		/// </summary>
		/// <param name="chatTask">A, possibly active, <see cref="Task"/> for an outgoing chat message.</param>
		/// <param name="reattachInfo"><see cref="ReattachInformation"/> to use, if any</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		protected abstract Task InitControllers(Task chatTask, ReattachInformation reattachInfo, CancellationToken cancellationToken);

		/// <inheritdoc />
		public async Task ChangeSettings(DreamDaemonLaunchParameters launchParameters, CancellationToken cancellationToken)
		{
			using (await SemaphoreSlimContext.Lock(Semaphore, cancellationToken).ConfigureAwait(false))
			{
				bool match = launchParameters.CanApplyWithoutReboot(ActiveLaunchParameters);
				ActiveLaunchParameters = launchParameters;
				if (match || Status == WatchdogStatus.Offline)
					return;

				ActiveParametersUpdated.TrySetResult(null); // queue an update
				ActiveParametersUpdated = new TaskCompletionSource<object>();
			}
		}

		/// <inheritdoc />
		async Task IEventConsumer.HandleEvent(EventType eventType, IEnumerable<string> parameters, CancellationToken cancellationToken)
		{
			// Method explicitly implemented to prevent accidental calls when this.eventConsumer should be used.
			var activeServer = GetActiveController();

			// Server may have ended
			if (activeServer == null)
				return;

			var notification = new EventNotification(eventType, parameters);
			var result = await activeServer.SendCommand(
				new TopicParameters(notification),
				cancellationToken)
				.ConfigureAwait(false);

			if (result?.InteropResponse?.ChatResponses != null)
				await Task.WhenAll(
					result.InteropResponse.ChatResponses.Select(
						x => Chat.SendMessage(
							x.Text,
							x.ChannelIds
								.Select(channelIdString =>
								{
									if (UInt64.TryParse(channelIdString, out var channelId))
										return (ulong?)channelId;

									return null;
								})
								.Where(nullableChannelId => nullableChannelId.HasValue)
								.Select(nullableChannelId => nullableChannelId.Value),
							cancellationToken)))
					.ConfigureAwait(false);
		}

		/// <inheritdoc />
		public async Task<string> HandleChatCommand(string commandName, string arguments, ChatUser sender, CancellationToken cancellationToken)
		{
			using (await SemaphoreSlimContext.Lock(Semaphore, cancellationToken).ConfigureAwait(false))
			{
				if (Status == WatchdogStatus.Offline)
					return "TGS: Server offline!";

				var commandObject = new ChatCommand(sender, commandName, arguments);

				var command = new TopicParameters(commandObject);

				var activeServer = GetActiveController();
				var commandResult = await activeServer.SendCommand(command, cancellationToken).ConfigureAwait(false);

				if (commandResult == null)
					return "TGS: Bad topic exchange!";

				if (commandResult.InteropResponse == null)
					return "TGS: Bad topic response!";

				return commandResult.InteropResponse.CommandResponseMessage ??
					"TGS: Command processed but no DMAPI response returned!";
			}
		}

		/// <inheritdoc />
		public async Task Launch(CancellationToken cancellationToken)
		{
			if (Status != WatchdogStatus.Offline)
				throw new JobException(ErrorCode.WatchdogRunning);
			using (await SemaphoreSlimContext.Lock(Semaphore, cancellationToken).ConfigureAwait(false))
				await LaunchNoLock(true, true, null, cancellationToken).ConfigureAwait(false);
		}

		/// <inheritdoc />
		public virtual async Task ResetRebootState(CancellationToken cancellationToken)
		{
			using (await SemaphoreSlimContext.Lock(Semaphore, cancellationToken).ConfigureAwait(false))
			{
				if (Status == WatchdogStatus.Offline)
					return;
				var toClear = GetActiveController();
				if (toClear != null)
					await toClear.SetRebootState(Session.RebootState.Normal, cancellationToken).ConfigureAwait(false);
			}
		}

		/// <inheritdoc />
		public async Task Restart(bool graceful, CancellationToken cancellationToken)
		{
			if (Status == WatchdogStatus.Offline)
				throw new JobException(ErrorCode.WatchdogNotRunning);

			Logger.LogTrace("Begin Restart. Graceful: {0}", graceful);
			using (await SemaphoreSlimContext.Lock(Semaphore, cancellationToken).ConfigureAwait(false))
			{
				if (!graceful)
				{
					var chatTask = Chat.SendWatchdogMessage("Manual restart triggered...", false, cancellationToken);
					await TerminateNoLock(false, false, cancellationToken).ConfigureAwait(false);
					await LaunchNoLock(true, false, null, cancellationToken).ConfigureAwait(false);
					await chatTask.ConfigureAwait(false);
					return;
				}

				var toReboot = GetActiveController();
				if (toReboot != null
					&& !await toReboot.SetRebootState(Session.RebootState.Restart, cancellationToken).ConfigureAwait(false))
					Logger.LogWarning("Unable to send reboot state change event!");
			}
		}

		/// <inheritdoc />
		public async Task StartAsync(CancellationToken cancellationToken)
		{
			var reattachInfo = await reattachInfoHandler.Load(cancellationToken).ConfigureAwait(false);
			if (!autoStart && reattachInfo == null)
				return;

			long? adminUserId = null;

			await databaseContextFactory.UseContext(
				async db => adminUserId = await db
					.Users
					.AsQueryable()
					.Where(x => x.CanonicalName == Models.User.CanonicalizeName(Api.Models.User.AdminName))
					.Select(x => x.Id)
					.FirstAsync(cancellationToken)
					.ConfigureAwait(false))
				.ConfigureAwait(false);
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
				Description = $"Instance startup watchdog {(reattachInfo != null ? "reattach" : "launch")}",
				CancelRight = (ulong)DreamDaemonRights.Shutdown,
				CancelRightsType = RightsType.DreamDaemon
			};
			await jobManager.RegisterOperation(job, async (j, databaseContextFactory, progressFunction, ct) =>
			{
				using (await SemaphoreSlimContext.Lock(Semaphore, ct).ConfigureAwait(false))
					await LaunchNoLock(true, true, reattachInfo, ct).ConfigureAwait(false);
			}, cancellationToken).ConfigureAwait(false);
		}

		/// <inheritdoc />
		public async Task StopAsync(CancellationToken cancellationToken)
		{
			await TerminateNoLock(false, !releaseServers, cancellationToken).ConfigureAwait(false);
			if (releasedReattachInformation != null)
			{
				await reattachInfoHandler.Save(releasedReattachInformation, cancellationToken).ConfigureAwait(false);
				releasedReattachInformation = null;
				releaseServers = false;
			}
		}

		/// <inheritdoc />
		public async Task Terminate(bool graceful, CancellationToken cancellationToken)
		{
			using (await SemaphoreSlimContext.Lock(Semaphore, cancellationToken).ConfigureAwait(false))
				await TerminateNoLock(graceful, !releaseServers, cancellationToken).ConfigureAwait(false);
		}

		/// <inheritdoc />
		public async Task HandleRestart(Version updateVersion, CancellationToken cancellationToken)
		{
			releaseServers = true;
			if (Status == WatchdogStatus.Online)
				await Chat.SendWatchdogMessage("Detaching...", false, cancellationToken).ConfigureAwait(false);
		}

		/// <inheritdoc />
		public abstract Task InstanceRenamed(string newInstanceName, CancellationToken cancellationToken);

		/// <inheritdoc />
		public async Task CreateDump(CancellationToken cancellationToken)
		{
			var session = GetActiveController();

			const string DumpDirectory = "ProcessDumps";
			await diagnosticsIOManager.CreateDirectory(DumpDirectory, cancellationToken).ConfigureAwait(false);

			var dumpFileName = diagnosticsIOManager.ResolvePath(
				diagnosticsIOManager.ConcatPath(
					DumpDirectory,
					$"DreamDaemon-{DateTimeOffset.Now.ToFileStamp()}.dmp"));

			Logger.LogInformation("Dumping session to {0}...", dumpFileName);
			await session.CreateDump(dumpFileName, cancellationToken).ConfigureAwait(false);
		}
	}
}
