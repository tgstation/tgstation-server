using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
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
	/// A <see cref="IWatchdog"/> that manages one server.
	/// </summary>
	class BasicWatchdog : WatchdogBase
	{
		/// <inheritdoc />
		public sealed override bool AlphaIsActive => true;

		/// <inheritdoc />
		public sealed override Models.CompileJob ActiveCompileJob => Server?.Dmb.CompileJob;

		/// <inheritdoc />
		public sealed override RebootState? RebootState => Server?.RebootState;

		/// <summary>
		/// The single <see cref="ISessionController"/>.
		/// </summary>
		protected ISessionController Server { get; private set; }

		/// <summary>
		/// If the server is set to gracefully reboot due to a pending dmb or settings change.
		/// </summary>
		bool gracefulRebootRequired;

		/// <summary>
		/// Initializes a new instance of the <see cref="BasicWatchdog"/> <see langword="class"/>.
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
		public BasicWatchdog(
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
				 jobManager,
				 serverControl,
				 asyncDelayer,
				 diagnosticsIOManager,
				 eventConsumer,
				 logger,
				 initialLaunchParameters,
				 instance,
				 autoStart)
		{ }

		/// <inheritdoc />
		protected override IReadOnlyDictionary<MonitorActivationReason, Task> GetMonitoredServerTasks(MonitorState monitorState)
		{
			if (Server == null)
				return null;

			monitorState.ActiveServer = Server;

			return new Dictionary<MonitorActivationReason, Task>
			{
				{ MonitorActivationReason.ActiveServerCrashed, Server.Lifetime },
				{ MonitorActivationReason.ActiveServerRebooted, Server.OnReboot },
				{ MonitorActivationReason.InactiveServerCrashed, Extensions.TaskExtensions.InfiniteTask() },
				{ MonitorActivationReason.InactiveServerRebooted, Extensions.TaskExtensions.InfiniteTask() },
				{ MonitorActivationReason.InactiveServerStartupComplete, Extensions.TaskExtensions.InfiniteTask() }
			};
		}

		/// <inheritdoc />
		protected override async Task HandleMonitorWakeup(MonitorActivationReason reason, MonitorState monitorState, CancellationToken cancellationToken)
		{
			switch (reason)
			{
				case MonitorActivationReason.ActiveServerCrashed:
					string exitWord = Server.TerminationWasRequested ? "exited" : "crashed";
					if (Server.RebootState == Session.RebootState.Shutdown)
					{
						// the time for graceful shutdown is now
						await Chat.SendWatchdogMessage(
							String.Format(
								CultureInfo.InvariantCulture,
								"Server {0}! Shutting down due to graceful termination request...",
								exitWord),
							false,
							cancellationToken)
							.ConfigureAwait(false);
						monitorState.NextAction = MonitorAction.Exit;
					}
					else
					{
						await Chat.SendWatchdogMessage(
							String.Format(
								CultureInfo.InvariantCulture,
								"Server {0}! Rebooting...",
								exitWord),
							false,
							cancellationToken)
							.ConfigureAwait(false);
						monitorState.NextAction = MonitorAction.Restart;
					}

					break;
				case MonitorActivationReason.ActiveServerRebooted:
					var rebootState = Server.RebootState;
					if (gracefulRebootRequired && rebootState == Session.RebootState.Normal)
					{
						Logger.LogError("Watchdog reached normal reboot state with gracefulRebootRequired set!");
						rebootState = Session.RebootState.Restart;
					}

					gracefulRebootRequired = false;
					Server.ResetRebootState();

					switch (rebootState)
					{
						case Session.RebootState.Normal:
							monitorState.NextAction = HandleNormalReboot();
							break;
						case Session.RebootState.Restart:
							monitorState.NextAction = MonitorAction.Restart;
							break;
						case Session.RebootState.Shutdown:
							// graceful shutdown time
							await Chat.SendWatchdogMessage(
								"Active server rebooted! Shutting down due to graceful termination request...",
								false,
								cancellationToken)
								.ConfigureAwait(false);
							monitorState.NextAction = MonitorAction.Exit;
							break;
						default:
							throw new InvalidOperationException($"Invalid reboot state: {rebootState}");
					}

					break;
				case MonitorActivationReason.ActiveLaunchParametersUpdated:
					await Server.SetRebootState(Session.RebootState.Restart, cancellationToken).ConfigureAwait(false);
					gracefulRebootRequired = true;
					break;
				case MonitorActivationReason.NewDmbAvailable:
					await HandleNewDmbAvailable(cancellationToken).ConfigureAwait(false);
					break;
				case MonitorActivationReason.InactiveServerCrashed:
				case MonitorActivationReason.InactiveServerRebooted:
				case MonitorActivationReason.InactiveServerStartupComplete:
					throw new NotSupportedException($"Unsupported activation reason: {reason}");
				case MonitorActivationReason.Heartbeat:
				default:
					throw new InvalidOperationException($"Invalid activation reason: {reason}");
			}
		}

		/// <inheritdoc />
		protected sealed override DualReattachInformation CreateReattachInformation()
			=> new DualReattachInformation
			{
				AlphaIsActive = true,
				Alpha = Server?.Release()
			};

		/// <inheritdoc />
		protected override void DisposeAndNullControllersImpl()
		{
			Server?.Dispose();
			Server = null;
			gracefulRebootRequired = false;
		}

		/// <inheritdoc />
		protected sealed override ISessionController GetActiveController() => Server;

		/// <inheritdoc />
		protected sealed override async Task InitControllers(
			Task chatTask,
			DualReattachInformation reattachInfo,
			CancellationToken cancellationToken)
		{
			var serverToReattach = reattachInfo?.Alpha ?? reattachInfo?.Bravo;
			var serverToKill = reattachInfo?.Bravo ?? reattachInfo?.Alpha;

			// vice versa
			if (serverToKill == serverToReattach)
				serverToKill = null;

			if (reattachInfo?.AlphaIsActive == false)
			{
				var temp = serverToReattach;
				serverToReattach = serverToKill;
				serverToKill = temp;
			}

			// don't need a new dmb if reattaching
			var doesntNeedNewDmb = serverToReattach != null;
			var dmbToUse = doesntNeedNewDmb ? null : DmbFactory.LockNextDmb(1);

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
					serverLaunchTask = SessionControllerFactory.Reattach(serverToReattach, reattachInfo.TopicRequestTimeout, cancellationToken);

				bool thereIsAnInactiveServerToKill = serverToKill != null;
				if (thereIsAnInactiveServerToKill)
					inactiveReattachTask = SessionControllerFactory.Reattach(serverToKill, reattachInfo.TopicRequestTimeout, cancellationToken);
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
					await ReattachFailure(
						chatTask,
						thereIsAnInactiveServerToKill && !inactiveServerWasKilled,
						cancellationToken)
						.ConfigureAwait(false);
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
		protected virtual Task HandleNewDmbAvailable(CancellationToken cancellationToken)
		{
			gracefulRebootRequired = true;
			return Server.SetRebootState(Session.RebootState.Restart, cancellationToken);
		}

		/// <summary>
		/// Prepare the server to launch a new instance with the <see cref="WatchdogBase.ActiveLaunchParameters"/> and a given <paramref name="dmbToUse"/>.
		/// </summary>
		/// <param name="dmbToUse">The <see cref="IDmbProvider"/> to be launched. Will not be disposed by this function.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the modified <see cref="IDmbProvider"/> to be used.</returns>
		protected virtual Task<IDmbProvider> PrepServerForLaunch(IDmbProvider dmbToUse, CancellationToken cancellationToken) => Task.FromResult(dmbToUse);

		/// <inheritdoc />
		public override Task ResetRebootState(CancellationToken cancellationToken)
		{
			if (!gracefulRebootRequired)
				return base.ResetRebootState(cancellationToken);

			return Restart(true, cancellationToken);
		}

		/// <inheritdoc />
		public sealed override Task InstanceRenamed(string newInstanceName, CancellationToken cancellationToken)
			=> Server?.InstanceRenamed(newInstanceName, cancellationToken) ?? Task.CompletedTask;
	}
}
