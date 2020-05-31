using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
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
using Tgstation.Server.Host.Components.Interop.Topic;
using Tgstation.Server.Host.Components.Session;
using Tgstation.Server.Host.Core;
using Tgstation.Server.Host.Database;
using Tgstation.Server.Host.Extensions;
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
		public bool Running
		{
			get => running;
			set
			{
				running = value;
				Logger.LogTrace("Running set to {0}", running);
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
		/// <see cref="TaskCompletionSource{TResult}"/> that completes when <see cref="ActiveLaunchParameters"/> are changed and we are <see cref="Running"/>.
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
		/// <see langword="lock"/> <see cref="object"/> used for <see cref="DisposeAndNullControllers"/>.
		/// </summary>
		readonly object controllerDisposeLock;

		/// <summary>
		/// If the <see cref="WatchdogBase"/> should <see cref="LaunchImplNoLock(bool, bool, DualReattachInformation, CancellationToken)"/> in <see cref="StartAsync(CancellationToken)"/>
		/// </summary>
		readonly bool autoStart;

		/// <summary>
		/// Used when detaching servers.
		/// </summary>
		DualReattachInformation releasedReattachInformation;

		/// <summary>
		/// The <see cref="CancellationTokenSource"/> for the monitor loop
		/// </summary>
		CancellationTokenSource monitorCts;

		/// <summary>
		/// The <see cref="Task"/> running the monitor loop
		/// </summary>
		Task monitorTask;

		/// <summary>
		/// The number of hearbeats missed.
		/// </summary>
		int heartbeatsMissed;

		/// <summary>
		/// If the servers should be released instead of shutdown
		/// </summary>
		bool releaseServers;

		/// <summary>
		/// Backing field for <see cref="Running"/>.
		/// </summary>
		bool running;

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
			if (!Running)
				return;
			if (!graceful)
			{
				var eventTask = HandleEvent(releaseServers ? EventType.WatchdogDetach : EventType.WatchdogShutdown, null, cancellationToken);

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
		/// <param name="activeServer">The active <see cref="ISessionController"/>.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the next <see cref="MonitorAction"/> to take.</returns>
		async Task<MonitorAction> HandleHeartbeat(ISessionController activeServer, CancellationToken cancellationToken)
		{
			Logger.LogTrace("Sending heartbeat to active server...");
			var response = await activeServer.SendCommand(new TopicParameters(), cancellationToken).ConfigureAwait(false);

			var shouldShutdown = activeServer.RebootState == Session.RebootState.Shutdown;
			if (response == null)
			{
				switch (++heartbeatsMissed)
				{
					case 1:
						Logger.LogDebug("DEFCON 4: Watchdog missed first heartbeat!");
						break;
					case 2:
						var message2 = "DEFCON 3: Watchdog has missed 2 heartbeats!";
						Logger.LogInformation(message2);
						await Chat.SendWatchdogMessage(message2, true, cancellationToken).ConfigureAwait(false);
						break;
					case 3:
						var actionToTake = shouldShutdown
							? "shutdown"
							: "be restarted";
						var message3 = $"DEFCON 2: Watchdog has missed 3 heartbeats! If DreamDaemon does not respond to the next one, the watchdog will {actionToTake}!";
						Logger.LogWarning(message3);
						await Chat.SendWatchdogMessage(message3, false, cancellationToken).ConfigureAwait(false);
						break;
					case 4:
						var actionTaken = shouldShutdown
							? "Shutting down due to graceful termination request"
							: "Restarting";
						var message4 = $"DEFCON 1: Four heartbeats have been missed! {actionTaken}...";
						Logger.LogWarning(message4);
						DisposeAndNullControllers();
						await Chat.SendWatchdogMessage(message4, false, cancellationToken).ConfigureAwait(false);
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
		/// <param name="reattachInfo"><see cref="DualReattachInformation"/> to use, if any</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		protected async Task LaunchImplNoLock(bool startMonitor, bool announce, DualReattachInformation reattachInfo, CancellationToken cancellationToken)
		{
			Logger.LogTrace("Begin LaunchImplNoLock");

			if (Running)
				throw new JobException(ErrorCode.WatchdogRunning);

			if (!DmbFactory.DmbAvailable)
				throw new JobException(ErrorCode.WatchdogCompileJobCorrupted);

			// this is necessary, the monitor could be in it's sleep loop trying to restart, if so cancel THAT monitor and start our own with blackjack and hookers
			Task chatTask;
			if (startMonitor && await StopMonitor().ConfigureAwait(false))
				chatTask = Chat.SendWatchdogMessage("Automatic retry sequence cancelled by manual launch. Restarting...", false, cancellationToken);
			else if (announce)
				chatTask = Chat.SendWatchdogMessage(reattachInfo == null ? "Launching..." : "Reattaching...", false, cancellationToken); // simple announce
			else
				chatTask = Task.CompletedTask; // no announce

			// since neither server is running, this is safe to do
			LastLaunchParameters = ActiveLaunchParameters;
			heartbeatsMissed = 0;

			// for when we call ourself and want to not catch thrown exceptions
			var ignoreNestedException = false;
			try
			{
				await InitControllers(() => ignoreNestedException = true, chatTask, reattachInfo, cancellationToken).ConfigureAwait(false);
				await chatTask.ConfigureAwait(false);

				Logger.LogInformation("Launched servers successfully");
				Running = true;

				if (startMonitor)
				{
					StartMonitor();
				}
			}
			catch (Exception e)
			{
				// don't try to send chat tasks or warning logs if were suppressing exceptions or cancelled
				if (!ignoreNestedException && !cancellationToken.IsCancellationRequested)
				{
					var originalChatTask = chatTask;
					async Task ChainChatTaskWithErrorMessage()
					{
						await originalChatTask.ConfigureAwait(false);
						await Chat.SendWatchdogMessage("Startup failed!", false, cancellationToken).ConfigureAwait(false);
					}

					chatTask = ChainChatTaskWithErrorMessage();
					Logger.LogWarning("Failed to start watchdog: {0}", e.ToString());
				}

				throw;
			}
			finally
			{
				// finish the chat task that's in flight
				try
				{
					await chatTask.ConfigureAwait(false);
				}
				catch (OperationCanceledException) { }
			}
		}

		/// <summary>
		/// Call <see cref="MonitorLifetimes(CancellationToken)"/> and setup <see cref="monitorCts"/> and <see cref="monitorTask"/>.
		/// </summary>
		protected void StartMonitor()
		{
			monitorCts = new CancellationTokenSource();
			monitorTask = MonitorLifetimes(monitorCts.Token);
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
		/// Send a chat message and log about a failed reattach operation and attempts another call to <see cref="LaunchImplNoLock(bool, bool, DualReattachInformation, CancellationToken)"/>.
		/// </summary>
		/// <param name="inactiveReattachSuccess">If the inactive server was reattached successfully.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation/</param>
		/// <returns>A <see cref="Task"/> representing the running operation.</returns>
		protected async Task NotifyOfFailedReattach(bool inactiveReattachSuccess, CancellationToken cancellationToken)
		{
			// we lost the server, just restart entirely
			DisposeAndNullControllers();
			const string FailReattachMessage = "Unable to properly reattach to server! Restarting...";
			Logger.LogWarning(FailReattachMessage);
			Logger.LogDebug(inactiveReattachSuccess ? "Also could not reattach to inactive server!" : "Inactive server was reattached successfully!");
			Task chatTask = Chat.SendWatchdogMessage(FailReattachMessage, false, cancellationToken);
			await LaunchImplNoLock(true, false, null, cancellationToken).ConfigureAwait(false);
			await chatTask.ConfigureAwait(false);
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
		/// Call <see cref="IDisposable.Dispose"/> and null the fields for all <see cref="ISessionController"/>s and set <see cref="Running"/> to <see langword="false"/>.
		/// </summary>
		protected abstract void DisposeAndNullControllersImpl();

		/// <summary>
		/// Wrapper for <see cref="DisposeAndNullControllersImpl"/> under a locked context.
		/// </summary>
		protected void DisposeAndNullControllers()
		{
			lock (controllerDisposeLock)
				DisposeAndNullControllersImpl();
		}

		/// <summary>
		/// Get the active <see cref="ISessionController"/>.
		/// </summary>
		/// <returns>The active <see cref="ISessionController"/>.</returns>
		protected abstract ISessionController GetActiveController();

		/// <summary>
		/// Create the <see cref="DualReattachInformation"/> for the <see cref="ISessionController"/>s.
		/// </summary>
		/// <returns>A new <see cref="DualReattachInformation"/>.</returns>
		protected abstract DualReattachInformation CreateReattachInformation();

		/// <summary>
		/// Gets the tasks for the following <see cref="MonitorActivationReason"/>s: <see cref="MonitorActivationReason.ActiveServerCrashed"/>, <see cref="MonitorActivationReason.ActiveServerRebooted"/>, <see cref="MonitorActivationReason.InactiveServerCrashed"/>, <see cref="MonitorActivationReason.InactiveServerRebooted"/>, <see cref="MonitorActivationReason.InactiveServerStartupComplete"/>.
		/// </summary>
		/// <param name="monitorState">The current <see cref="MonitorState"/>.</param>
		/// <returns>A <see cref="IReadOnlyDictionary{TKey, TValue}"/> of the <see cref="Task"/>s keyed by their <see cref="MonitorActivationReason"/>.</returns>
		/// <remarks>This function should not assume the servers are not <see langword="null"/>.</remarks>
		protected abstract IReadOnlyDictionary<MonitorActivationReason, Task> GetMonitoredServerTasks(MonitorState monitorState);

		/// <summary>
		/// Handles the actions to take when the monitor has to "wake up"
		/// </summary>
		/// <param name="activationReason">The <see cref="MonitorActivationReason"/> that caused the invocation. Will never be <see cref="MonitorActivationReason.Heartbeat"/>.</param>
		/// <param name="monitorState">The current <see cref="MonitorState"/>.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task"/> representing the running operation.</returns>
		protected abstract Task HandleMonitorWakeup(
			MonitorActivationReason activationReason,
			MonitorState monitorState,
			CancellationToken cancellationToken);

		private async Task<MonitorState> MonitorRestart(CancellationToken cancellationToken)
		{
			Logger.LogTrace("Monitor restart!");
			DisposeAndNullControllers();

			var chatTask = Task.CompletedTask;
			for (var retryAttempts = 1; ; ++retryAttempts)
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
							return new MonitorState();
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
		}

		/// <summary>
		/// The loop that watches the watchdog.
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task"/> representing the running operation.</returns>
		private async Task MonitorLifetimes(CancellationToken cancellationToken)
		{
			Logger.LogTrace("Entered MonitorLifetimes");
			using var _ = cancellationToken.Register(() => Logger.LogTrace("Monitor cancellationToken triggered"));

			// this function is responsible for calling HandlerMonitorWakeup when necessary and manitaining the MonitorState
			var iteration = 1;
			try
			{
				for (var monitorState = new MonitorState(); monitorState.NextAction != MonitorAction.Exit; ++iteration)
					try
					{
						Logger.LogDebug("Iteration {0} of monitor loop", iteration);

						// load the activation tasks into local variables
						var serverTasks = GetMonitoredServerTasks(monitorState);
						if (serverTasks.Count != 5)
							throw new InvalidOperationException("Expected 5 monitored server tasks!");

						var activeServerLifetime = serverTasks[MonitorActivationReason.ActiveServerCrashed];
						var activeServerReboot = serverTasks[MonitorActivationReason.ActiveServerRebooted];
						var inactiveServerLifetime = serverTasks[MonitorActivationReason.InactiveServerCrashed];
						var inactiveServerReboot = serverTasks[MonitorActivationReason.InactiveServerRebooted];
						var inactiveStartupComplete = serverTasks[MonitorActivationReason.InactiveServerStartupComplete];

						Task activeLaunchParametersChanged = ActiveParametersUpdated.Task;
						var newDmbAvailable = DmbFactory.OnNewerDmb;

						var heartbeatSeconds = ActiveLaunchParameters.HeartbeatSeconds.Value;
						var heartbeat = heartbeatSeconds == 0
							? Extensions.TaskExtensions.InfiniteTask()
							: Task.Delay(TimeSpan.FromSeconds(heartbeatSeconds));

						// cancel waiting if requested
						var cancelTcs = new TaskCompletionSource<object>();
						using (cancellationToken.Register(() => cancelTcs.SetCanceled()))
						{
							var toWaitOn = Task.WhenAny(
								activeServerLifetime,
								activeServerReboot,
								inactiveServerLifetime,
								inactiveServerReboot,
								inactiveStartupComplete,
								heartbeat,
								newDmbAvailable,
								cancelTcs.Task,
								activeLaunchParametersChanged);

							// wait for something to happen
							await toWaitOn.ConfigureAwait(false);
						}

						cancellationToken.ThrowIfCancellationRequested();
						Logger.LogTrace("Monitor activated");

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

								var anyActivation = CheckActivationReason(ref activeServerLifetime, MonitorActivationReason.ActiveServerCrashed)
									|| CheckActivationReason(ref activeServerReboot, MonitorActivationReason.ActiveServerRebooted)
									|| CheckActivationReason(ref newDmbAvailable, MonitorActivationReason.NewDmbAvailable)
									|| CheckActivationReason(ref inactiveServerLifetime, MonitorActivationReason.InactiveServerCrashed)
									|| CheckActivationReason(ref inactiveServerReboot, MonitorActivationReason.InactiveServerRebooted)
									|| CheckActivationReason(ref inactiveStartupComplete, MonitorActivationReason.InactiveServerStartupComplete)
									|| CheckActivationReason(ref activeLaunchParametersChanged, MonitorActivationReason.ActiveLaunchParametersUpdated)
									|| CheckActivationReason(ref heartbeat, MonitorActivationReason.Heartbeat);

								if (!anyActivation)
									moreActivationsToProcess = false;
								else
								{
									Logger.LogTrace("Reason: {0}", activationReason);
									if (activationReason == MonitorActivationReason.Heartbeat)
										monitorState.NextAction = await HandleHeartbeat(
											monitorState.ActiveServer,
											cancellationToken)
											.ConfigureAwait(false);
									else
										await HandleMonitorWakeup(
											activationReason,
											monitorState,
											cancellationToken)
											.ConfigureAwait(false);
								}
							}
						}

						Logger.LogTrace("Next monitor action is to {0}", monitorState.NextAction);
						if (monitorState.NextAction == MonitorAction.Restart)
							monitorState = await MonitorRestart(cancellationToken).ConfigureAwait(false);
					}
					catch (OperationCanceledException)
					{
						throw;
					}
					catch (Exception e)
					{
						// really, this should NEVER happen
						Logger.LogError(
							"Monitor crashed! Iteration: {0}, Monitor State: {1}, Exception: {2}",
							iteration,
							JsonConvert.SerializeObject(monitorState),
							e);

						var nextActionMessage = monitorState.NextAction != MonitorAction.Exit
							? "Restarting"
							: "Shutting down";
						var chatTask = Chat.SendWatchdogMessage(
							$"Monitor crashed, this should NEVER happen! Please report this, full details in logs! {nextActionMessage}. Error: {e.Message}",
							false,
							cancellationToken);

						if (disposed)
							monitorState.NextAction = MonitorAction.Exit;
						else if (monitorState.NextAction != MonitorAction.Exit)
							monitorState = await MonitorRestart(cancellationToken).ConfigureAwait(false);

						await chatTask.ConfigureAwait(false);
					}
			}
			catch (OperationCanceledException)
			{
				Logger.LogDebug("Monitor cancelled");

				if (releaseServers)
				{
					Logger.LogTrace("Detaching servers...");
					releasedReattachInformation = CreateReattachInformation();
				}
			}

			DisposeAndNullControllers();

			Logger.LogTrace("Monitor exiting...");
		}

		/// <summary>
		/// Starts all <see cref="ISessionController"/>s.
		/// </summary>
		/// <param name="callBeforeRecurse">An <see cref="Action"/> that must be run before making a recursive call to <see cref="LaunchImplNoLock(bool, bool, DualReattachInformation, CancellationToken)"/>.</param>
		/// <param name="chatTask">A, possibly active, <see cref="Task"/> for an outgoing chat message.</param>
		/// <param name="reattachInfo"><see cref="DualReattachInformation"/> to use, if any</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		protected abstract Task InitControllers(Action callBeforeRecurse, Task chatTask, DualReattachInformation reattachInfo, CancellationToken cancellationToken);

		/// <inheritdoc />
		public async Task ChangeSettings(DreamDaemonLaunchParameters launchParameters, CancellationToken cancellationToken)
		{
			using (await SemaphoreSlimContext.Lock(Semaphore, cancellationToken).ConfigureAwait(false))
			{
				if (launchParameters.Match(ActiveLaunchParameters))
					return;
				ActiveLaunchParameters = launchParameters;
				if (Running)
				{
					ActiveParametersUpdated.TrySetResult(null); // queue an update
					ActiveParametersUpdated = new TaskCompletionSource<object>();
				}
			}
		}

		/// <inheritdoc />
		public async Task HandleEvent(EventType eventType, IEnumerable<string> parameters, CancellationToken cancellationToken)
		{
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
				if (!Running)
					return "ERROR: Server offline!";

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
			using (await SemaphoreSlimContext.Lock(Semaphore, cancellationToken).ConfigureAwait(false))
				await LaunchImplNoLock(true, true, null, cancellationToken).ConfigureAwait(false);
		}

		/// <inheritdoc />
		public virtual async Task ResetRebootState(CancellationToken cancellationToken)
		{
			using (await SemaphoreSlimContext.Lock(Semaphore, cancellationToken).ConfigureAwait(false))
			{
				if (!Running)
					return;
				var toClear = GetActiveController();
				if (toClear != null)
					await toClear.SetRebootState(Session.RebootState.Normal, cancellationToken).ConfigureAwait(false);
			}
		}

		/// <inheritdoc />
		public async Task Restart(bool graceful, CancellationToken cancellationToken)
		{
			Logger.LogTrace("Begin Restart. Graceful: {0}", graceful);
			using (await SemaphoreSlimContext.Lock(Semaphore, cancellationToken).ConfigureAwait(false))
			{
				if (!graceful || !Running)
				{
					Task chatTask;
					if (Running)
					{
						chatTask = Chat.SendWatchdogMessage("Manual restart triggered...", false, cancellationToken);
						await TerminateNoLock(false, false, cancellationToken).ConfigureAwait(false);
					}
					else
						chatTask = Task.CompletedTask;
					await LaunchImplNoLock(true, !Running, null, cancellationToken).ConfigureAwait(false);
					await chatTask.ConfigureAwait(false);
				}

				var toReboot = GetActiveController();
				if (toReboot != null)
				{
					if (!await toReboot.SetRebootState(Session.RebootState.Restart, cancellationToken).ConfigureAwait(false))
						Logger.LogWarning("Unable to send reboot state change event!");
				}
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
					await LaunchImplNoLock(true, true, reattachInfo, ct).ConfigureAwait(false);
			}, cancellationToken).ConfigureAwait(false);
		}

		/// <inheritdoc />
		public async Task StopAsync(CancellationToken cancellationToken)
		{
			if (releaseServers)
			{
				await StopMonitor().ConfigureAwait(false);
				if (releasedReattachInformation != null)
					await reattachInfoHandler.Save(releasedReattachInformation, cancellationToken).ConfigureAwait(false);
			}

			await TerminateNoLock(false, !releaseServers, cancellationToken).ConfigureAwait(false);
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
			if (Running)
				await Chat.SendWatchdogMessage("Detaching...", false, cancellationToken).ConfigureAwait(false);
		}
	}
}
