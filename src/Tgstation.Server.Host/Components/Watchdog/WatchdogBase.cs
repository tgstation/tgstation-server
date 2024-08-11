using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using BetterWin32Errors;

using Microsoft.Extensions.Logging;

using Serilog.Context;

using Tgstation.Server.Api.Models;
using Tgstation.Server.Api.Models.Internal;
using Tgstation.Server.Api.Rights;
using Tgstation.Server.Common.Extensions;
using Tgstation.Server.Host.Components.Chat;
using Tgstation.Server.Host.Components.Deployment;
using Tgstation.Server.Host.Components.Deployment.Remote;
using Tgstation.Server.Host.Components.Events;
using Tgstation.Server.Host.Components.Interop;
using Tgstation.Server.Host.Components.Interop.Topic;
using Tgstation.Server.Host.Components.Session;
using Tgstation.Server.Host.Core;
using Tgstation.Server.Host.Extensions;
using Tgstation.Server.Host.IO;
using Tgstation.Server.Host.Jobs;
using Tgstation.Server.Host.Utils;

namespace Tgstation.Server.Host.Components.Watchdog
{
	/// <summary>
	/// Base class for <see cref="IWatchdog"/>s.
	/// </summary>
#pragma warning disable CA1506 // TODO: Decomplexify
	abstract class WatchdogBase : IWatchdog, ICustomCommandHandler, IRestartHandler
	{
		/// <inheritdoc />
		public long? SessionId => GetActiveController()?.ReattachInformation.Id;

		/// <inheritdoc />
		public WatchdogStatus Status
		{
			get => status;
			protected set
			{
				var oldStatus = status;
				status = value;
				Logger.LogTrace("Status set from {oldStatus} to {status}", oldStatus, status);
			}
		}

		/// <inheritdoc />
		public long? MemoryUsage => GetActiveController()?.MemoryUsage;

		/// <inheritdoc />
		public abstract bool AlphaIsActive { get; }

		/// <inheritdoc />
		public DreamDaemonLaunchParameters ActiveLaunchParameters { get; protected set; }

		/// <inheritdoc />
		public DreamDaemonLaunchParameters? LastLaunchParameters { get; protected set; }

		/// <inheritdoc />
		public Models.CompileJob? ActiveCompileJob => GetActiveController()?.CompileJob;

		/// <inheritdoc />
		public abstract RebootState? RebootState { get; }

		/// <summary>
		/// The <see cref="ISessionPersistor"/> for the <see cref="WatchdogBase"/>.
		/// </summary>
		protected ISessionPersistor SessionPersistor { get; }

		/// <summary>
		/// The <see cref="ILogger"/> for the <see cref="WatchdogBase"/>.
		/// </summary>
		protected ILogger<WatchdogBase> Logger { get; }

		/// <summary>
		/// The <see cref="IChatManager"/> for the <see cref="WatchdogBase"/>.
		/// </summary>
		protected IChatManager Chat { get; }

		/// <summary>
		/// The <see cref="ISessionControllerFactory"/> for the <see cref="WatchdogBase"/>.
		/// </summary>
		protected ISessionControllerFactory SessionControllerFactory { get; }

		/// <summary>
		/// The <see cref="IDmbFactory"/> for the <see cref="WatchdogBase"/>.
		/// </summary>
		protected IDmbFactory DmbFactory { get; }

		/// <summary>
		/// The <see cref="IAsyncDelayer"/> for the <see cref="WatchdogBase"/>.
		/// </summary>
		protected IAsyncDelayer AsyncDelayer { get; }

		/// <summary>
		/// The <see cref="IIOManager"/> for the <see cref="WatchdogBase"/> pointing to the Game directory.
		/// </summary>
		protected IIOManager GameIOManager { get; }

		/// <summary>
		/// The <see cref="Api.Models.Instance"/> for the <see cref="WatchdogBase"/>.
		/// </summary>
		readonly Api.Models.Instance metadata;

		/// <summary>
		/// The <see cref="SemaphoreSlim"/> for the <see cref="WatchdogBase"/>.
		/// </summary>
		readonly SemaphoreSlim synchronizationSemaphore;

		/// <summary>
		/// <see cref="SemaphoreSlim"/> used for <see cref="DisposeAndNullControllers"/>.
		/// </summary>
		readonly SemaphoreSlim controllerDisposeSemaphore;

		/// <summary>
		/// The <see cref="IEventConsumer"/> that is not the <see cref="WatchdogBase"/>.
		/// </summary>
		readonly IEventConsumer eventConsumer;

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
		/// The <see cref="IRemoteDeploymentManagerFactory"/> for the <see cref="WatchdogBase"/>.
		/// </summary>
		readonly IRemoteDeploymentManagerFactory remoteDeploymentManagerFactory;

		/// <summary>
		/// If the <see cref="WatchdogBase"/> should <see cref="LaunchNoLock(bool, bool, bool, ReattachInformation, CancellationToken)"/> in <see cref="StartAsync(CancellationToken)"/>.
		/// </summary>
		readonly bool autoStart;

		/// <summary>
		/// <see cref="TaskCompletionSource"/> that completes when <see cref="ActiveLaunchParameters"/> are changed and we are running.
		/// </summary>
		volatile TaskCompletionSource activeParametersUpdated;

		/// <summary>
		/// The <see cref="CancellationTokenSource"/> for the monitor loop.
		/// </summary>
		CancellationTokenSource? monitorCts;

		/// <summary>
		/// The <see cref="Task"/> running the monitor loop.
		/// </summary>
		Task? monitorTask;

		/// <summary>
		/// Backing field for <see cref="Status"/>.
		/// </summary>
		WatchdogStatus status;

		/// <summary>
		/// The number of hearbeats missed.
		/// </summary>
		int healthChecksMissed;

		/// <summary>
		/// If the servers should be released instead of shutdown.
		/// </summary>
		bool releaseServers;

		/// <summary>
		/// If the <see cref="WatchdogBase"/> has been <see cref="DisposeAsync"/>'d.
		/// </summary>
		bool disposed;

		/// <summary>
		/// Initializes a new instance of the <see cref="WatchdogBase"/> class.
		/// </summary>
		/// <param name="chat">The value of <see cref="Chat"/>.</param>
		/// <param name="sessionControllerFactory">The value of <see cref="SessionControllerFactory"/>.</param>
		/// <param name="dmbFactory">The value of <see cref="DmbFactory"/>.</param>
		/// <param name="sessionPersistor">The value of <see cref="SessionPersistor"/>.</param>
		/// <param name="jobManager">The value of <see cref="jobManager"/>.</param>
		/// <param name="serverControl">The <see cref="IServerControl"/> to populate <see cref="restartRegistration"/> with.</param>
		/// <param name="asyncDelayer">The value of <see cref="AsyncDelayer"/>.</param>
		/// <param name="diagnosticsIOManager">The value of <see cref="diagnosticsIOManager"/>.</param>
		/// <param name="eventConsumer">The value of <see cref="EventConsumer"/>.</param>
		/// <param name="remoteDeploymentManagerFactory">The value of <see cref="remoteDeploymentManagerFactory"/>.</param>
		/// <param name="gameIOManager">The value of <see cref="GameIOManager"/>.</param>
		/// <param name="logger">The value of <see cref="Logger"/>.</param>
		/// <param name="initialLaunchParameters">The initial value of <see cref="ActiveLaunchParameters"/>. May be modified.</param>
		/// <param name="metadata">The value of <see cref="metadata"/>.</param>
		/// <param name="autoStart">The value of <see cref="autoStart"/>.</param>
		protected WatchdogBase(
			IChatManager chat,
			ISessionControllerFactory sessionControllerFactory,
			IDmbFactory dmbFactory,
			ISessionPersistor sessionPersistor,
			IJobManager jobManager,
			IServerControl serverControl,
			IAsyncDelayer asyncDelayer,
			IIOManager diagnosticsIOManager,
			IEventConsumer eventConsumer,
			IRemoteDeploymentManagerFactory remoteDeploymentManagerFactory,
			IIOManager gameIOManager,
			ILogger<WatchdogBase> logger,
			DreamDaemonLaunchParameters initialLaunchParameters,
			Api.Models.Instance metadata,
			bool autoStart)
		{
			Chat = chat ?? throw new ArgumentNullException(nameof(chat));
			SessionControllerFactory = sessionControllerFactory ?? throw new ArgumentNullException(nameof(sessionControllerFactory));
			DmbFactory = dmbFactory ?? throw new ArgumentNullException(nameof(dmbFactory));
			SessionPersistor = sessionPersistor ?? throw new ArgumentNullException(nameof(sessionPersistor));
			this.jobManager = jobManager ?? throw new ArgumentNullException(nameof(jobManager));
			AsyncDelayer = asyncDelayer ?? throw new ArgumentNullException(nameof(asyncDelayer));
			this.diagnosticsIOManager = diagnosticsIOManager ?? throw new ArgumentNullException(nameof(diagnosticsIOManager));
			this.eventConsumer = eventConsumer ?? throw new ArgumentNullException(nameof(eventConsumer));
			this.remoteDeploymentManagerFactory = remoteDeploymentManagerFactory ?? throw new ArgumentNullException(nameof(remoteDeploymentManagerFactory));
			GameIOManager = gameIOManager ?? throw new ArgumentNullException(nameof(gameIOManager));
			Logger = logger ?? throw new ArgumentNullException(nameof(logger));
			ActiveLaunchParameters = initialLaunchParameters ?? throw new ArgumentNullException(nameof(initialLaunchParameters));
			this.metadata = metadata ?? throw new ArgumentNullException(nameof(metadata));
			this.autoStart = autoStart;

			ArgumentNullException.ThrowIfNull(serverControl);

			chat.RegisterCommandHandler(this);

			ActiveLaunchParameters = initialLaunchParameters;
			releaseServers = false;
			activeParametersUpdated = new TaskCompletionSource();

			restartRegistration = serverControl.RegisterForRestart(this);
			try
			{
				synchronizationSemaphore = new SemaphoreSlim(1);
				controllerDisposeSemaphore = new SemaphoreSlim(1);
			}
			catch
			{
				restartRegistration.Dispose();
				synchronizationSemaphore?.Dispose();
				throw;
			}

			Logger.LogTrace("Created watchdog");
		}

		/// <inheritdoc />
		public async ValueTask DisposeAsync()
		{
			Logger.LogTrace("Disposing...");
			synchronizationSemaphore.Dispose();
			restartRegistration.Dispose();

			await DisposeAndNullControllersImpl();
			controllerDisposeSemaphore.Dispose();
			monitorCts?.Dispose();
			disposed = true;
		}

		/// <inheritdoc />
		public async ValueTask<bool> ChangeSettings(DreamDaemonLaunchParameters launchParameters, CancellationToken cancellationToken)
		{
			using (await SemaphoreSlimContext.Lock(synchronizationSemaphore, cancellationToken))
			{
				var currentLaunchParameters = ActiveLaunchParameters;
				ActiveLaunchParameters = launchParameters;
				var currentEngine = GetActiveController()?.EngineVersion.Engine;
				if (!currentEngine.HasValue)
					return false;

				bool match = launchParameters.CanApplyWithoutReboot(currentLaunchParameters, currentEngine.Value);
				if (match || Status == WatchdogStatus.Offline || Status == WatchdogStatus.DelayedRestart)
					return false;

				var oldTcs = Interlocked.Exchange(ref activeParametersUpdated, new TaskCompletionSource());
				oldTcs.SetResult();
			}

			return true;
		}

		/// <inheritdoc />
		public async ValueTask<MessageContent> HandleChatCommand(string commandName, string arguments, ChatUser sender, CancellationToken cancellationToken)
		{
			using (await SemaphoreSlimContext.Lock(synchronizationSemaphore, cancellationToken))
			{
				var commandObject = new ChatCommand(sender, commandName, arguments);
				var command = new TopicParameters(commandObject);
				var activeServer = GetActiveController();
				if (Status != WatchdogStatus.Online || activeServer == null)
					return new MessageContent
					{
						Text = "TGS: Server offline!",
					};

				var commandResult = await activeServer.SendCommand(command, cancellationToken);

				if (commandResult == null)
					return new MessageContent
					{
						Text = "TGS: Bad topic exchange!",
					};

				if (commandResult == null)
					return new MessageContent
					{
						Text = "TGS: Bad topic response!",
					};

				var commandResponse = new MessageContent
				{
					Text = commandResult.CommandResponse?.Text ?? commandResult.CommandResponseMessage,
					Embed = commandResult.CommandResponse?.Embed,
				};

				if (commandResponse.Text == null && commandResponse.Embed == null)
				{
					commandResponse.Text = "TGS: Command processed but no DMAPI response returned!";
				}

				HandleChatResponses(commandResult);

				return commandResponse;
			}
		}

		/// <inheritdoc />
		public async ValueTask Launch(CancellationToken cancellationToken)
		{
			if (Status != WatchdogStatus.Offline)
				throw new JobException(ErrorCode.WatchdogRunning);
			using (await SemaphoreSlimContext.Lock(synchronizationSemaphore, cancellationToken))
				await LaunchNoLock(true, true, true, null, cancellationToken);
		}

		/// <inheritdoc />
		public virtual async ValueTask ResetRebootState(CancellationToken cancellationToken)
		{
			using (await SemaphoreSlimContext.Lock(synchronizationSemaphore, cancellationToken))
			{
				if (Status == WatchdogStatus.Offline)
					return;
				var toClear = GetActiveController();
				if (toClear != null)
					await toClear.SetRebootState(Session.RebootState.Normal, cancellationToken);
			}
		}

		/// <inheritdoc />
		public async ValueTask Restart(bool graceful, CancellationToken cancellationToken)
		{
			if (Status == WatchdogStatus.Offline)
				throw new JobException(ErrorCode.WatchdogNotRunning);

			Logger.LogTrace("Begin Restart. Graceful: {gracefulFlag}", graceful);
			using (await SemaphoreSlimContext.Lock(synchronizationSemaphore, cancellationToken))
			{
				if (!graceful)
				{
					Chat.QueueWatchdogMessage("Manual restart triggered...");
					await TerminateNoLock(false, false, cancellationToken);
					await LaunchNoLock(true, false, true, null, cancellationToken);
					return;
				}

				var toReboot = GetActiveController();
				if (toReboot != null
					&& !await toReboot.SetRebootState(Session.RebootState.Restart, cancellationToken))
					Logger.LogWarning("Unable to send reboot state change event!");
			}
		}

		/// <inheritdoc />
		public async Task StartAsync(CancellationToken cancellationToken)
		{
			var reattachInfo = await SessionPersistor.Load(cancellationToken);
			var reattaching = reattachInfo != null;
			if (!autoStart && !reattaching)
				return;

			var job = Models.Job.Create(
				reattaching
					? JobCode.StartupWatchdogReattach
					: JobCode.StartupWatchdogLaunch,
				null,
				metadata,
				DreamDaemonRights.Shutdown);
			await jobManager.RegisterOperation(
				job,
				async (core, databaseContextFactory, paramJob, progressFunction, ct) =>
				{
					if (core?.Watchdog != this)
						throw new InvalidOperationException(Instance.DifferentCoreExceptionMessage);

					using (await SemaphoreSlimContext.Lock(synchronizationSemaphore, ct))
						await LaunchNoLock(true, true, true, reattachInfo, ct);

					await Chat.UpdateTrackingContexts(ct);
				},
				cancellationToken);
		}

		/// <inheritdoc />
		public async Task StopAsync(CancellationToken cancellationToken) =>
			await TerminateNoLock(false, !releaseServers, cancellationToken);

		/// <inheritdoc />
		public async ValueTask Terminate(bool graceful, CancellationToken cancellationToken)
		{
			using (await SemaphoreSlimContext.Lock(synchronizationSemaphore, cancellationToken))
				await TerminateNoLock(graceful, !releaseServers, cancellationToken);
		}

		/// <inheritdoc />
		public async ValueTask HandleRestart(Version? updateVersion, bool handlerMayDelayShutdownWithExtremelyLongRunningTasks, CancellationToken cancellationToken)
		{
			if (handlerMayDelayShutdownWithExtremelyLongRunningTasks)
			{
				await Terminate(true, cancellationToken);

				if (Status != WatchdogStatus.Offline)
				{
					Logger.LogDebug("Waiting for server to gracefully shut down.");
					await monitorTask!.WaitAsync(cancellationToken);
				}
				else
					Logger.LogTrace("Graceful shutdown requested but server is already offline.");

				return;
			}

			releaseServers = true;
			if (Status == WatchdogStatus.Online)
				Chat.QueueWatchdogMessage("Detaching...");
			else
				Logger.LogTrace("Not sending detach chat message as status is: {status}", Status);
		}

		/// <inheritdoc />
		public abstract ValueTask InstanceRenamed(string newInstanceName, CancellationToken cancellationToken);

		/// <inheritdoc />
		public async ValueTask CreateDump(CancellationToken cancellationToken)
		{
			using (await SemaphoreSlimContext.Lock(synchronizationSemaphore, cancellationToken))
				await CreateDumpNoLock(cancellationToken);
		}

		/// <inheritdoc />
		public async ValueTask<bool> Broadcast(string message, CancellationToken cancellationToken)
		{
			ArgumentNullException.ThrowIfNull(message);

			var activeServer = GetActiveController();
			if (activeServer == null)
			{
				Logger.LogInformation("Attempted broadcast failed, no active server!");
				return false;
			}

			if (!activeServer.DMApiAvailable)
			{
				Logger.LogInformation("Attempted broadcast failed, no DMAPI!");
				return false;
			}

			var minimumRequiredVersion = new Version(5, 7, 0);
			if (activeServer.DMApiVersion < minimumRequiredVersion)
			{
				Logger.LogInformation(
					"Attempted broadcast failed, insufficient interop version: {interopVersion}. Requires {minimumRequiredVersion}!",
					activeServer.DMApiVersion,
					minimumRequiredVersion);
				return false;
			}

			Logger.LogInformation("Broadcasting: {message}", message);

			var response = await activeServer.SendCommand(
				TopicParameters.CreateBroadcastParameters(message),
				cancellationToken);

			return response != null && response.ErrorMessage == null;
		}

		/// <inheritdoc />
		async ValueTask IEventConsumer.HandleEvent(EventType eventType, IEnumerable<string?> parameters, bool deploymentPipeline, CancellationToken cancellationToken)
		{
			ArgumentNullException.ThrowIfNull(parameters);

			// Method explicitly implemented to prevent accidental calls when this.eventConsumer should be used.
			var activeServer = GetActiveController();

			// Server may have ended
			if (activeServer == null)
				return;

			var notification = new EventNotification(eventType, parameters);
			var result = await activeServer.SendCommand(
				new TopicParameters(notification),
				cancellationToken);

			HandleChatResponses(result);
		}

		/// <inheritdoc />
		ValueTask? IEventConsumer.HandleCustomEvent(string eventName, IEnumerable<string?> parameters, CancellationToken cancellationToken)
			=> throw new NotSupportedException("Watchdogs do not support custom events!");

		/// <summary>
		/// Starts all <see cref="ISessionController"/>s.
		/// </summary>
		/// <param name="eventTask">A, possibly active, <see cref="Task"/> for an event that's running.</param>
		/// <param name="reattachInfo"><see cref="ReattachInformation"/> to use, if any.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task"/> representing the running operation.</returns>
		protected abstract ValueTask InitController(ValueTask eventTask, ReattachInformation? reattachInfo, CancellationToken cancellationToken);

		/// <summary>
		/// Launches the watchdog.
		/// </summary>
		/// <param name="startMonitor">If <see cref="MonitorLifetimes(CancellationToken)"/> should be started by this function.</param>
		/// <param name="announce">If the launch should be announced to chat by this function.</param>
		/// <param name="announceFailure">If launch failure should be announced to chat by this function.</param>
		/// <param name="reattachInfo"><see cref="ReattachInformation"/> to use, if any.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask"/> representing the running operation.</returns>
		protected async ValueTask LaunchNoLock(
			bool startMonitor,
			bool announce,
			bool announceFailure,
			ReattachInformation? reattachInfo,
			CancellationToken cancellationToken)
		{
			Logger.LogTrace("Begin LaunchImplNoLock");
			if (startMonitor && Status != WatchdogStatus.Offline)
				throw new JobException(ErrorCode.WatchdogRunning);

			if (reattachInfo == null && !DmbFactory.DmbAvailable)
				throw new JobException(ErrorCode.WatchdogCompileJobCorrupted);

			// this is necessary, the monitor could be in it's sleep loop trying to restart, if so cancel THAT monitor and start our own with blackjack and hookers
			var eventTask = ValueTask.CompletedTask;
			if (announce)
			{
				Chat.QueueWatchdogMessage(
					reattachInfo == null
						? "Launching..."
						: "Reattaching..."); // simple announce
				if (reattachInfo == null)
					eventTask = HandleEventImpl(EventType.WatchdogLaunch, Enumerable.Empty<string>(), false, cancellationToken);
			}

			// since neither server is running, this is safe to do
			LastLaunchParameters = ActiveLaunchParameters;
			healthChecksMissed = 0;

			try
			{
				await InitController(eventTask, reattachInfo, cancellationToken);
			}
			catch (OperationCanceledException ex)
			{
				Logger.LogTrace(ex, "Controller initialization cancelled!");
				throw;
			}
			catch (Exception e)
			{
				Logger.LogWarning(e, "Failed to start watchdog!");
				var originalChatTask = eventTask;
				async ValueTask ChainEventTaskWithErrorMessage()
				{
					await originalChatTask;
					if (announceFailure)
						Chat.QueueWatchdogMessage("Startup failed!");
				}

				eventTask = ChainEventTaskWithErrorMessage();
				throw;
			}
			finally
			{
				// finish the chat task that's in flight
				try
				{
					await eventTask;
				}
				catch (OperationCanceledException ex)
				{
					Logger.LogTrace(ex, "Announcement task canceled!");
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
		/// Stops <see cref="MonitorLifetimes(CancellationToken)"/>. Doesn't kill the servers.
		/// </summary>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in <see langword="true"/> if the monitor was running, <see langword="false"/> otherwise.</returns>
		protected async ValueTask<bool> StopMonitor()
		{
			Logger.LogTrace("StopMonitor");
			if (monitorTask == null)
				return false;
			var wasRunning = !monitorTask.IsCompleted;
			monitorCts!.Cancel();
			await monitorTask;
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
		/// <returns>A <see cref="ValueTask"/> representing the running operation.</returns>
		protected async ValueTask CheckLaunchResult(ISessionController controller, string serverName, CancellationToken cancellationToken)
		{
			var launchResult = await controller.LaunchResult.WaitAsync(cancellationToken);

			// Dead sessions won't trigger this
			if (launchResult.ExitCode.HasValue) // you killed us ray...
				throw new JobException(
					ErrorCode.WatchdogStartupFailed,
					new JobException($"{serverName} failed to start: {launchResult}"));
			if (!launchResult.StartupTime.HasValue)
				throw new JobException(
					ErrorCode.WatchdogStartupTimeout,
					new JobException($"{serverName} timed out on startup: {ActiveLaunchParameters.StartupTimeout!.Value}s"));
		}

		/// <summary>
		/// Call from <see cref="InitController(ValueTask, ReattachInformation, CancellationToken)"/> when a reattach operation fails to attempt a fresh start.
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask"/> representing the running operation.</returns>
		protected async ValueTask ReattachFailure(CancellationToken cancellationToken)
		{
			// we lost the server, just restart entirely
			// DCT: Operation must always run
			await DisposeAndNullControllers(CancellationToken.None);
			const string FailReattachMessage = "Unable to properly reattach to server! Restarting watchdog...";
			Logger.LogWarning(FailReattachMessage);

			Chat.QueueWatchdogMessage(FailReattachMessage);
			await InitController(ValueTask.CompletedTask, null, cancellationToken);
		}

		/// <summary>
		/// Call <see cref="IDisposable.Dispose"/> and null the fields for all <see cref="ISessionController"/>s.
		/// </summary>
		/// <returns>A <see cref="ValueTask"/> representing the running operation.</returns>
		protected abstract ValueTask DisposeAndNullControllersImpl();

		/// <summary>
		/// Wrapper for <see cref="DisposeAndNullControllersImpl"/> under a locked context.
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask"/> representing the running operation.</returns>
		protected async ValueTask DisposeAndNullControllers(CancellationToken cancellationToken)
		{
			Logger.LogTrace("DisposeAndNullControllers");
			using (await SemaphoreSlimContext.Lock(controllerDisposeSemaphore, cancellationToken))
			{
				await DisposeAndNullControllersImpl();
				if (!releaseServers)
					await SessionPersistor.Clear(cancellationToken);
			}
		}

		/// <summary>
		/// Get the active <see cref="ISessionController"/>.
		/// </summary>
		/// <returns>The active <see cref="ISessionController"/>, if any.</returns>
		protected abstract ISessionController? GetActiveController();

		/// <summary>
		/// Handles the actions to take when the monitor has to "wake up".
		/// </summary>
		/// <param name="activationReason">The <see cref="MonitorActivationReason"/> that caused the invocation. Will never be <see cref="MonitorActivationReason.HealthCheck"/>.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in the <see cref="MonitorAction"/> to take.</returns>
		protected abstract ValueTask<MonitorAction> HandleMonitorWakeup(
			MonitorActivationReason activationReason,
			CancellationToken cancellationToken);

		/// <summary>
		/// To be called before a given <paramref name="newCompileJob"/> goes live.
		/// </summary>
		/// <param name="newCompileJob">The new <see cref="Models.CompileJob"/> being applied.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask"/> representing the running operation.</returns>
		protected async ValueTask BeforeApplyDmb(Models.CompileJob newCompileJob, CancellationToken cancellationToken)
		{
			if (newCompileJob.Id == ActiveCompileJob?.Id)
			{
				Logger.LogTrace("Same compile job, not sending deployment event");
				return;
			}

			var remoteDeploymentManager = remoteDeploymentManagerFactory.CreateRemoteDeploymentManager(
				metadata,
				newCompileJob);

			var eventTask = eventConsumer.HandleEvent(
				EventType.DeploymentActivation,
				new List<string?>
				{
					GameIOManager.ResolvePath(newCompileJob.DirectoryName!.Value.ToString()),
				},
				false,
				cancellationToken);

			try
			{
				await remoteDeploymentManager.ApplyDeployment(newCompileJob, cancellationToken);
			}
			catch (Exception ex)
			{
				Logger.LogWarning(ex, "Failed to apply remote deployment!");
			}

			await eventTask;
		}

		/// <summary>
		/// Handle a given <paramref name="eventType"/> without re-throwing errors.
		/// </summary>
		/// <param name="eventType">The <see cref="EventType"/>.</param>
		/// <param name="parameters">An <see cref="IEnumerable{T}"/> of <see cref="string"/> parameters for <paramref name="eventType"/>.</param>
		/// <param name="relayToSession">If the event should be sent to DreamDaemon.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask"/> representing the running operation.</returns>
		protected async ValueTask HandleEventImpl(EventType eventType, IEnumerable<string> parameters, bool relayToSession, CancellationToken cancellationToken)
		{
			try
			{
				var sessionEventTask = relayToSession ? ((IEventConsumer)this).HandleEvent(eventType, parameters, false, cancellationToken) : ValueTask.CompletedTask;
				var eventConsumerTask = eventConsumer.HandleEvent(eventType, parameters, false, cancellationToken);
				await ValueTaskExtensions.WhenAll(
					eventConsumerTask,
					sessionEventTask);
			}
			catch (JobException ex)
			{
				Logger.LogError(ex, "Suppressing exception triggered by event!");
			}
		}

		/// <summary>
		/// Attempt to restart the monitor from scratch.
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask"/> representing the running operation.</returns>
		async ValueTask MonitorRestart(CancellationToken cancellationToken)
		{
			Logger.LogTrace("Monitor restart!");

			await DisposeAndNullControllers(cancellationToken);

			for (var retryAttempts = 1; ; ++retryAttempts)
			{
				Status = WatchdogStatus.Restoring;
				Exception launchException;
				using (await SemaphoreSlimContext.Lock(synchronizationSemaphore, cancellationToken))
					try
					{
						// use LaunchImplNoLock without announcements or restarting the monitor
						await LaunchNoLock(false, false, false, null, cancellationToken);
						Status = WatchdogStatus.Online;
						Logger.LogDebug("Relaunch successful, resuming monitor...");
						return;
					}
					catch (Exception e) when (e is not OperationCanceledException)
					{
						launchException = e;
					}

				Logger.LogWarning(launchException, "Failed to automatically restart the watchdog! Attempt: {attemptNumber}", retryAttempts);
				Status = WatchdogStatus.DelayedRestart;

				var retryDelay = Math.Min(
					Convert.ToInt32(
						Math.Pow(2, retryAttempts)),
					TimeSpan.FromHours(1).TotalSeconds); // max of one hour, increasing by a power of 2 each time

				Chat.QueueWatchdogMessage(
					$"Failed to restart (Attempt: {retryAttempts}), retrying in {retryDelay}s...");

				await AsyncDelayer.Delay(
					TimeSpan.FromSeconds(retryDelay),
					cancellationToken);
			}
		}

		/// <summary>
		/// Check for a new <see cref="IDmbProvider"/>.
		/// </summary>
		/// <param name="currentCompileJob">The session's current <see cref="CompileJob"/>.</param>
		/// <returns>A <see cref="Task"/> that completes if and when a newer <see cref="CompileJob"/> is available.</returns>
		async Task InitialCheckDmbUpdated(CompileJob currentCompileJob)
		{
			var factoryTask = DmbFactory.OnNewerDmb;

			var latestCompileJob = await DmbFactory.LatestCompileJob();
			if (latestCompileJob != null && latestCompileJob.Id != currentCompileJob.Id)
			{
				Logger.LogDebug("Found new CompileJob without waiting");
				return;
			}

			await factoryTask;
		}

		/// <summary>
		/// The main loop of the watchdog. Ayschronously waits for events to occur and then responds to them.
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask"/> representing the running operation.</returns>
#pragma warning disable CA1502
		async Task MonitorLifetimes(CancellationToken cancellationToken)
		{
			Logger.LogTrace("Entered MonitorLifetimes");
			Status = WatchdogStatus.Online;
			using var cancellationTokenLoggingRegistration = cancellationToken.Register(() => Logger.LogTrace("Monitor cancellationToken triggered"));

			// this function is responsible for calling HandlerMonitorWakeup when necessary and manitaining the MonitorState
			try
			{
				MonitorAction nextAction = MonitorAction.Continue;
				Task? activeServerLifetime = null,
					activeServerReboot = null,
					activeServerStartup = null,
					serverPrimed = null,
					activeLaunchParametersChanged = null,
					newDmbAvailable = null,
					healthCheck = null;
				ISessionController? lastController = null;
				var ranInitialDmbCheck = false;
				for (ulong iteration = 1; nextAction != MonitorAction.Exit; ++iteration)
					using (LogContext.PushProperty(SerilogContextHelper.WatchdogMonitorIterationContextProperty, iteration))
					{
						var nextMonitorWakeupTcs = new TaskCompletionSource();
						try
						{
							Logger.LogTrace("Iteration {iteration} of monitor loop", iteration);
							nextAction = MonitorAction.Continue;

							var controller = GetActiveController();

							void UpdateMonitoredTasks()
							{
								var sameController = lastController == controller;
								void TryUpdateTask(ref Task? oldTask, Func<Task> newTaskFactory)
								{
									if (sameController && oldTask?.IsCompleted == true)
										return;

									oldTask = newTaskFactory();
								}

								controller!.RebootGate = nextMonitorWakeupTcs.Task;

								TryUpdateTask(ref activeServerLifetime, () => controller.Lifetime);
								TryUpdateTask(ref activeServerReboot, () => controller.OnReboot);
								TryUpdateTask(ref serverPrimed, () => controller.OnPrime);
								TryUpdateTask(ref activeServerStartup, () => controller.OnStartup);

								if (!sameController)
									lastController = controller;

								TryUpdateTask(ref activeLaunchParametersChanged, () => activeParametersUpdated.Task);
								TryUpdateTask(
									ref newDmbAvailable,
									() =>
									{
										var result = ranInitialDmbCheck
											? DmbFactory.OnNewerDmb
											: InitialCheckDmbUpdated(controller.CompileJob);
										ranInitialDmbCheck = true;
										return result;
									});
							}

							if (controller != null)
							{
								UpdateMonitoredTasks();

								var healthCheckSeconds = ActiveLaunchParameters.HealthCheckSeconds!.Value;
								healthCheck = healthCheckSeconds == 0
									|| !controller.DMApiAvailable
									? Extensions.TaskExtensions.InfiniteTask
									: Task.Delay(
										TimeSpan.FromSeconds(healthCheckSeconds),
										cancellationToken);

								// cancel waiting if requested
								var toWaitOn = Task.WhenAny(
									activeServerLifetime!,
									activeServerReboot!,
									activeServerStartup!,
									healthCheck,
									newDmbAvailable!,
									activeLaunchParametersChanged!,
									serverPrimed!);

								// wait for something to happen
								await toWaitOn.WaitAsync(cancellationToken);
							}
							else
							{
								Logger.LogError("Controller was null on monitor wakeup! Attempting restart...");
								nextAction = MonitorAction.Restart; // excuse me wtf?
							}

							cancellationToken.ThrowIfCancellationRequested();
							Logger.LogTrace("Monitor activated");

							// always run HandleMonitorWakeup from the context of the semaphore lock
							using (await SemaphoreSlimContext.Lock(synchronizationSemaphore, cancellationToken))
							{
								// Set this sooner so chat sends don't hold us up
								if (activeServerLifetime!.IsCompleted)
									Status = WatchdogStatus.Restoring;

								// multiple things may have happened, handle them one at a time
								for (var moreActivationsToProcess = true; moreActivationsToProcess && (nextAction == MonitorAction.Continue || nextAction == MonitorAction.Skip);)
								{
									MonitorActivationReason activationReason = default; // this will always be assigned before being used

									bool CheckActivationReason(ref Task? task, MonitorActivationReason testActivationReason)
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
										|| CheckActivationReason(ref healthCheck, MonitorActivationReason.HealthCheck)
										|| CheckActivationReason(ref serverPrimed, MonitorActivationReason.ActiveServerPrimed)
										|| CheckActivationReason(ref activeServerStartup, MonitorActivationReason.ActiveServerStartup);

									UpdateMonitoredTasks();

									if (!anyActivation)
										moreActivationsToProcess = false;
									else
									{
										Logger.LogTrace("Reason: {activationReason}", activationReason);
										if (activationReason == MonitorActivationReason.HealthCheck)
											nextAction = await HandleHealthCheck(
												cancellationToken);
										else
											nextAction = await HandleMonitorWakeup(
												activationReason,
												cancellationToken);
									}
								}
							}

							Logger.LogTrace("Next monitor action is to {nextAction}", nextAction);

							// Restart if requested
							if (nextAction == MonitorAction.Restart)
							{
								await MonitorRestart(cancellationToken);
								nextAction = MonitorAction.Continue;
							}
						}
						catch (Exception e) when (e is not OperationCanceledException)
						{
							// really, this should NEVER happen
							Logger.LogError(
								e,
								"Monitor crashed! Iteration: {iteration}",
								iteration);

							var nextActionMessage = nextAction != MonitorAction.Exit
								? "Recovering"
								: "Shutting down";
							Chat.QueueWatchdogMessage(
								$"Monitor crashed, this should NEVER happen! Please report this, full details in logs! {nextActionMessage}. Error: {e.Message}");

							if (disposed)
								nextAction = MonitorAction.Exit;
							else if (nextAction != MonitorAction.Exit)
							{
								if (GetActiveController()?.Lifetime.IsCompleted != true)
									await MonitorRestart(cancellationToken);
								else
									Logger.LogDebug("Server seems to be okay, not restarting");
								nextAction = MonitorAction.Continue;
							}
						}
						finally
						{
							nextMonitorWakeupTcs.SetResult();
						}
					}
			}
			catch (OperationCanceledException)
			{
				// stop signal
				Logger.LogDebug("Monitor cancelled");

				if (releaseServers)
				{
					Logger.LogTrace("Detaching server...");
					var controller = GetActiveController();
					if (controller != null)
						await controller.Release();
					else
						Logger.LogError("Controller was null on monitor shutdown!");
				}
			}

			// DCT: Operation must always run
			await DisposeAndNullControllers(CancellationToken.None);
			Status = WatchdogStatus.Offline;

			Logger.LogTrace("Monitor exiting...");
		}
#pragma warning restore CA1502

		/// <summary>
		/// Implementation of <see cref="Terminate(bool, CancellationToken)"/>. Does not lock <see cref="synchronizationSemaphore"/>.
		/// </summary>
		/// <param name="graceful">If <see langword="true"/> the termination will be delayed until a reboot is detected in the active server's DMAPI and this function will return immediately.</param>
		/// <param name="announce">If <see langword="true"/> the termination will be announced using <see cref="Chat"/>.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask"/> representing the running operation.</returns>
		async ValueTask TerminateNoLock(bool graceful, bool announce, CancellationToken cancellationToken)
		{
			if (Status == WatchdogStatus.Offline)
				return;
			if (!graceful)
			{
				var eventTask = HandleEventImpl(
					releaseServers
						? EventType.WatchdogDetach
						: EventType.WatchdogShutdown,
					Enumerable.Empty<string>(),
					releaseServers,
					cancellationToken);

				if (announce)
					Chat.QueueWatchdogMessage("Shutting down...");

				await eventTask;

				await StopMonitor();

				LastLaunchParameters = null;
				return;
			}

			// merely set the reboot state
			var toKill = GetActiveController();
			if (toKill != null)
			{
				await toKill.SetRebootState(Session.RebootState.Shutdown, cancellationToken);
				Logger.LogTrace("Graceful termination requested");
			}
			else
				Logger.LogTrace("Could not gracefully terminate as there is no active controller!");
		}

		/// <summary>
		/// Handles a watchdog health check.
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in the next <see cref="MonitorAction"/> to take.</returns>
		async ValueTask<MonitorAction> HandleHealthCheck(CancellationToken cancellationToken)
		{
			Logger.LogTrace("Sending health check to active server...");
			var activeServer = GetActiveController();
			if (activeServer == null)
				return MonitorAction.Restart; // uhhhh???

			var response = await activeServer.SendCommand(new TopicParameters(), cancellationToken);

			var shouldShutdown = activeServer.RebootState == Session.RebootState.Shutdown;
			if (response == null)
			{
				switch (++healthChecksMissed)
				{
					case 1:
						Logger.LogDebug("DEFCON 4: Game server missed first health check!");
						break;
					case 2:
						const string message2 = "DEFCON 3: Game server has missed 2 health checks!";
						Logger.LogInformation(message2);
						Chat.QueueWatchdogMessage(message2);
						break;
					case 3:
						var actionToTake = shouldShutdown
							? "shutdown"
							: "be restarted";
						const string logTemplate1 = "DEFCON 2: Game server has missed 3 health checks! If it does not respond to the next one, the watchdog will {actionToTake}!";
						Logger.LogWarning(logTemplate1, actionToTake);
						Chat.QueueWatchdogMessage(
							logTemplate1.Replace(
								"{actionToTake}",
								actionToTake,
								StringComparison.Ordinal));
						break;
					case 4:
						var actionTaken = shouldShutdown
							? "Shutting down due to graceful termination request"
							: "Restarting";
						const string logTemplate2 = "DEFCON 1: Four health checks have been missed! {actionTaken}...";
						Logger.LogWarning(logTemplate2, actionTaken);
						Chat.QueueWatchdogMessage(
							logTemplate2.Replace(
								"{actionTaken}",
								actionTaken,
								StringComparison.Ordinal));

						if (ActiveLaunchParameters.DumpOnHealthCheckRestart!.Value)
						{
							Logger.LogDebug("DumpOnHealthCheckRestart enabled.");
							try
							{
								await CreateDumpNoLock(cancellationToken);
							}
							catch (JobException ex)
							{
								Logger.LogWarning(ex, "Creating dump failed!");
							}
							catch (Win32Exception ex)
							{
								Logger.LogWarning(ex, "Creating dump failed!");
							}
						}
						else
							Logger.LogTrace("DumpOnHealthCheckRestart disabled.");

						await DisposeAndNullControllers(cancellationToken);
						return shouldShutdown ? MonitorAction.Exit : MonitorAction.Restart;
					default:
						Logger.LogError("Invalid health checks missed count: {healthChecksMissed}", healthChecksMissed);
						break;
				}
			}
			else
				healthChecksMissed = 0;

			return MonitorAction.Continue;
		}

		/// <summary>
		/// Handle any <see cref="TopicResponse.ChatResponses"/> in a given topic <paramref name="result"/>.
		/// </summary>
		/// <param name="result">The <see cref="TopicResponse"/>.</param>
		void HandleChatResponses(TopicResponse? result)
		{
			if (result?.ChatResponses != null)
			{
				var warnedMissingChannelIds = false;
				foreach (var response in result.ChatResponses
					.Where(response =>
					{
						if (response.ChannelIds == null)
						{
							if (!warnedMissingChannelIds)
							{
								Logger.LogWarning("DMAPI response contains null channelIds!");
								warnedMissingChannelIds = true;
							}

							return false;
						}

						return true;
					}))
					Chat.QueueMessage(
						response,
						response.ChannelIds!
							.Select(channelIdString =>
							{
								if (UInt64.TryParse(channelIdString, out var channelId))
									return (ulong?)channelId;
								else
									Logger.LogWarning("Could not parse chat response channel ID: {channelID}", channelIdString);

								return null;
							})
							.Where(nullableChannelId => nullableChannelId.HasValue)
							.Select(nullableChannelId => nullableChannelId!.Value));
			}
		}

		/// <summary>
		/// Attempt to create a process dump for the game server. Requires a lock on <see cref="synchronizationSemaphore"/>.
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask"/> representing the running operation.</returns>
		async ValueTask CreateDumpNoLock(CancellationToken cancellationToken)
		{
			const string DumpDirectory = "ProcessDumps";

			var session = GetActiveController();
			if (session?.Lifetime.IsCompleted != false)
				throw new JobException(ErrorCode.GameServerOffline);

			var dumpFileExtension = session.DumpFileExtension;

			var dumpFileNameTemplate = diagnosticsIOManager.ResolvePath(
				diagnosticsIOManager.ConcatPath(
					DumpDirectory,
					$"DreamDaemon-{DateTimeOffset.UtcNow.ToFileStamp()}"));

			var dumpFileName = $"{dumpFileNameTemplate}{dumpFileExtension}";
			var iteration = 0;
			while (await diagnosticsIOManager.FileExists(dumpFileName, cancellationToken))
				dumpFileName = $"{dumpFileNameTemplate} ({++iteration}){dumpFileExtension}";

			if (iteration == 0)
				await diagnosticsIOManager.CreateDirectory(DumpDirectory, cancellationToken);

			if (session.Lifetime.IsCompleted)
				throw new JobException(ErrorCode.GameServerOffline);

			Logger.LogInformation("Dumping session to {dumpFileName}...", dumpFileName);
			await session.CreateDump(dumpFileName, ActiveLaunchParameters.Minidumps!.Value, cancellationToken);
		}
	}
}
