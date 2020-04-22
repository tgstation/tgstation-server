using Byond.TopicSender;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Api.Models.Internal;
using Tgstation.Server.Api.Rights;
using Tgstation.Server.Host.Components.Chat;
using Tgstation.Server.Host.Components.Deployment;
using Tgstation.Server.Host.Components.Interop.Topic;
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
		public bool Running { get; protected set; }

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
		/// The <see cref="IChat"/> for the <see cref="WatchdogBase"/>
		/// </summary>
		protected IChat Chat { get; }

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
		/// The <see cref="IDatabaseContextFactory"/> for the <see cref="ExperimentalWatchdog"/>
		/// </summary>
		readonly IDatabaseContextFactory databaseContextFactory;

		/// <summary>
		/// The <see cref="IByondTopicSender"/> for the <see cref="ExperimentalWatchdog"/>
		/// </summary>
		readonly IByondTopicSender byondTopicSender;

		/// <summary>
		/// The <see cref="IEventConsumer"/> for the <see cref="ExperimentalWatchdog"/>
		/// </summary>
		readonly IEventConsumer eventConsumer;

		/// <summary>
		/// The <see cref="IJobManager"/> for the <see cref="ExperimentalWatchdog"/>
		/// </summary>
		readonly IJobManager jobManager;

		/// <summary>
		/// The <see cref="IRestartRegistration"/> for the <see cref="ExperimentalWatchdog"/>
		/// </summary>
		readonly IRestartRegistration restartRegistration;

		/// <summary>
		/// If the <see cref="WatchdogBase"/> should <see cref="LaunchImplNoLock(bool, bool, WatchdogReattachInformation, CancellationToken)"/> in <see cref="StartAsync(CancellationToken)"/>
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
		/// If the servers should be released instead of shutdown
		/// </summary>
		bool releaseServers;

		/// <summary>
		/// Initializes a new instance of the <see cref="WatchdogBase"/> <see langword="class"/>.
		/// </summary>
		/// <param name="chat">The value of <see cref="Chat"/></param>
		/// <param name="sessionControllerFactory">The value of <see cref="SessionControllerFactory"/></param>
		/// <param name="dmbFactory">The value of <see cref="DmbFactory"/></param>
		/// <param name="reattachInfoHandler">The value of <see cref="reattachInfoHandler"/></param>
		/// <param name="databaseContextFactory">The value of <see cref="databaseContextFactory"/></param>
		/// <param name="byondTopicSender">The value of <see cref="byondTopicSender"/></param>
		/// <param name="eventConsumer">The value of <see cref="eventConsumer"/></param>
		/// <param name="jobManager">The value of <see cref="jobManager"/></param>
		/// <param name="serverControl">The <see cref="IServerControl"/> to populate <see cref="restartRegistration"/> with</param>
		/// <param name="asyncDelayer">The value of <see cref="AsyncDelayer"/>.</param>
		/// <param name="logger">The value of <see cref="Logger"/></param>
		/// <param name="initialLaunchParameters">The initial value of <see cref="ActiveLaunchParameters"/>. May be modified</param>
		/// <param name="instance">The value of <see cref="instance"/></param>
		/// <param name="autoStart">The value of <see cref="autoStart"/></param>
		protected WatchdogBase(
			IChat chat,
			ISessionControllerFactory sessionControllerFactory,
			IDmbFactory dmbFactory,
			IReattachInfoHandler reattachInfoHandler,
			IDatabaseContextFactory databaseContextFactory,
			IByondTopicSender byondTopicSender,
			IEventConsumer eventConsumer,
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
			AsyncDelayer = asyncDelayer ?? throw new ArgumentNullException(nameof(asyncDelayer));
			this.reattachInfoHandler = reattachInfoHandler ?? throw new ArgumentNullException(nameof(reattachInfoHandler));
			this.databaseContextFactory = databaseContextFactory ?? throw new ArgumentNullException(nameof(databaseContextFactory));
			this.byondTopicSender = byondTopicSender ?? throw new ArgumentNullException(nameof(byondTopicSender));
			this.eventConsumer = eventConsumer ?? throw new ArgumentNullException(nameof(eventConsumer));
			this.jobManager = jobManager ?? throw new ArgumentNullException(nameof(jobManager));
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
			Semaphore.Dispose();
			restartRegistration.Dispose();
			DisposeAndNullControllers();

			Debug.Assert(monitorCts == null, "Expected monitorCts to be null!");
			monitorCts?.Dispose();
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
				var chatTask = announce ? Chat.SendWatchdogMessage("Terminating...", cancellationToken) : Task.CompletedTask;
				await StopMonitor().ConfigureAwait(false);
				DisposeAndNullControllers();
				LastLaunchParameters = null;
				await chatTask.ConfigureAwait(false);
				return;
			}

			// merely set the reboot state
			var toKill = GetActiveController();
			if (toKill != null)
				await toKill.SetRebootState(Watchdog.RebootState.Shutdown, cancellationToken).ConfigureAwait(false);
		}

		/// <summary>
		/// Launches the watchdog.
		/// </summary>
		/// <param name="startMonitor">If <see cref="MonitorLifetimes(CancellationToken)"/> should be started by this function</param>
		/// <param name="announce">If the launch should be announced to chat by this function</param>
		/// <param name="reattachInfo"><see cref="WatchdogReattachInformation"/> to use, if any</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		protected async Task LaunchImplNoLock(bool startMonitor, bool announce, WatchdogReattachInformation reattachInfo, CancellationToken cancellationToken)
		{
			Logger.LogTrace("Begin LaunchNoLock");

			if (Running)
				throw new JobException("Watchdog already running!");

			if (!DmbFactory.DmbAvailable)
				throw new JobException("Corrupted compilation, please redeploy!");

			// this is necessary, the monitor could be in it's sleep loop trying to restart, if so cancel THAT monitor and start our own with blackjack and hookers
			Task chatTask;
			if (startMonitor && await StopMonitor().ConfigureAwait(false))
				chatTask = Chat.SendWatchdogMessage("Automatic retry sequence cancelled by manual launch. Restarting...", cancellationToken);
			else if (announce)
				chatTask = Chat.SendWatchdogMessage(reattachInfo == null ? "Starting..." : "Reattaching...", cancellationToken); // simple announce
			else
				chatTask = Task.CompletedTask; // no announce

			// since neither server is running, this is safe to do
			LastLaunchParameters = ActiveLaunchParameters;

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
						await Chat.SendWatchdogMessage("Startup failed!", cancellationToken).ConfigureAwait(false);
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
			monitorCts.Cancel();
			await monitorTask.ConfigureAwait(false);
			monitorCts.Dispose();
			monitorTask = null;
			monitorCts = null;
			return true;
		}

		/// <summary>
		/// Send a chat message and log about a failed reattach operation and attempts another call to <see cref="LaunchImplNoLock(bool, bool, WatchdogReattachInformation, CancellationToken)"/>.
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
			Task chatTask = Chat.SendWatchdogMessage(FailReattachMessage, cancellationToken);
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
				throw new JobException(String.Format(CultureInfo.InvariantCulture, "{0} failed to start: {1}", serverName, launchResult));
			if (!launchResult.StartupTime.HasValue)
				throw new JobException(String.Format(CultureInfo.InvariantCulture, "{0} timed out on startup: {1}s", serverName, ActiveLaunchParameters.StartupTimeout.Value));
		}

		/// <summary>
		/// Call <see cref="IDisposable.Dispose"/> and null the fields for all <see cref="ISessionController"/>s and set <see cref="Running"/> to <see langword="false"/>.
		/// </summary>
		protected abstract void DisposeAndNullControllers();

		/// <summary>
		/// Get the active <see cref="ISessionController"/>.
		/// </summary>
		/// <returns>The active <see cref="ISessionController"/>.</returns>
		protected abstract ISessionController GetActiveController();

		/// <summary>
		/// Create the <see cref="WatchdogReattachInformation"/> for the <see cref="ISessionController"/>s.
		/// </summary>
		/// <returns>A new <see cref="WatchdogReattachInformation"/>.</returns>
		protected abstract WatchdogReattachInformation CreateReattachInformation();

		/// <summary>
		/// The loop that watches the watchdog
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		protected abstract Task MonitorLifetimes(CancellationToken cancellationToken);

		/// <summary>
		/// Starts all <see cref="ISessionController"/>s.
		/// </summary>
		/// <param name="callBeforeRecurse">An <see cref="Action"/> that must be run before making a recursive call to <see cref="LaunchImplNoLock(bool, bool, WatchdogReattachInformation, CancellationToken)"/>.</param>
		/// <param name="chatTask">A, possibly active, <see cref="Task"/> for an outgoing chat message.</param>
		/// <param name="reattachInfo"><see cref="WatchdogReattachInformation"/> to use, if any</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		protected abstract Task InitControllers(Action callBeforeRecurse, Task chatTask, WatchdogReattachInformation reattachInfo, CancellationToken cancellationToken);

		/// <inheritdoc />
		public async Task ChangeSettings(DreamDaemonLaunchParameters launchParameters, CancellationToken cancellationToken)
		{
			using (await SemaphoreSlimContext.Lock(Semaphore, cancellationToken).ConfigureAwait(false))
			{
				if (launchParameters.Match(ActiveLaunchParameters))
					return;
				ActiveLaunchParameters = launchParameters;
				if (Running)
					ActiveParametersUpdated.TrySetResult(null); // queue an update
			}
		}

		/// <inheritdoc />
		public async Task<bool> HandleEvent(EventType eventType, IEnumerable<string> parameters, CancellationToken cancellationToken)
		{
			if (!Running)
				return true;

			TopicResponse result;
			using (await SemaphoreSlimContext.Lock(Semaphore, cancellationToken).ConfigureAwait(false))
			{
				if (!Running)
					return true;

				var notification = new EventNotification(eventType, parameters);

				var activeServer = GetActiveController();
				result = await activeServer.SendCommand(
					new TopicParameters(notification),
					cancellationToken)
					.ConfigureAwait(false);
			}

			if (result?.ChatResponses == null)
				return true;

			await Task.WhenAll(result.ChatResponses.Select(x => Chat.SendMessage(x.Message, x.ChannelIds, cancellationToken))).ConfigureAwait(false);

			return true;
		}

		/// <inheritdoc />
		public async Task<string> HandleChatCommand(string commandName, string arguments, Chat.User sender, CancellationToken cancellationToken)
		{
			using (await SemaphoreSlimContext.Lock(Semaphore, cancellationToken).ConfigureAwait(false))
			{
				if (!Running)
					return "ERROR: Server offline!";

				var commandObject = new ChatCommand(sender, commandName, arguments);

				var command = new TopicParameters(commandObject);

				var activeServer = GetActiveController();
				var commandResult = await activeServer.SendCommand(command, cancellationToken).ConfigureAwait(false);

				return commandResult?.CommandResponse ??
					(commandResult == null
						? "ERROR: Bad topic exchange!"
						: "ERROR: Bad DMAPI response!");
			}
		}

		/// <inheritdoc />
		public async Task Launch(CancellationToken cancellationToken)
		{
			using (await SemaphoreSlimContext.Lock(Semaphore, cancellationToken).ConfigureAwait(false))
				await LaunchImplNoLock(true, true, null, cancellationToken).ConfigureAwait(false);
		}

		/// <inheritdoc />
		public async Task ResetRebootState(CancellationToken cancellationToken)
		{
			using (await SemaphoreSlimContext.Lock(Semaphore, cancellationToken).ConfigureAwait(false))
			{
				if (!Running)
					return;
				var toClear = GetActiveController();
				if (toClear != null)
					toClear.ResetRebootState();
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
						chatTask = Chat.SendWatchdogMessage("Manual restart triggered...", cancellationToken);
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
					if (!await toReboot.SetRebootState(Watchdog.RebootState.Restart, cancellationToken).ConfigureAwait(false))
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

			await databaseContextFactory.UseContext(async db => adminUserId = await db.Users
			.Where(x => x.CanonicalName == Models.User.CanonicalizeName(Api.Models.User.AdminName))
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
				using (await SemaphoreSlimContext.Lock(Semaphore, ct).ConfigureAwait(false))
					await LaunchImplNoLock(true, true, reattachInfo, ct).ConfigureAwait(false);
			}, cancellationToken).ConfigureAwait(false);
		}

		/// <inheritdoc />
		public async Task StopAsync(CancellationToken cancellationToken)
		{
			try
			{
				if (releaseServers && Running)
				{
					await StopMonitor().ConfigureAwait(false);

					var reattachInformation = CreateReattachInformation();
					await reattachInfoHandler.Save(reattachInformation, cancellationToken).ConfigureAwait(false);
				}

				await Terminate(false, cancellationToken).ConfigureAwait(false);
			}
			catch
			{
				releaseServers = false;
				throw;
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
			if (Running)
				await Chat.SendWatchdogMessage("Detaching...", cancellationToken).ConfigureAwait(false);
		}
	}
}
