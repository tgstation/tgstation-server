using Microsoft.Extensions.Logging;
using System;
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
		protected override async Task<MonitorAction> HandleMonitorWakeup(MonitorActivationReason reason, CancellationToken cancellationToken)
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
						return MonitorAction.Exit;
					}

					await Chat.SendWatchdogMessage(
						String.Format(
							CultureInfo.InvariantCulture,
							"Server {0}! Rebooting...",
							exitWord),
						false,
						cancellationToken)
						.ConfigureAwait(false);
					return MonitorAction.Restart;
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
							return HandleNormalReboot();
						case Session.RebootState.Restart:
							return MonitorAction.Restart;
						case Session.RebootState.Shutdown:
							// graceful shutdown time
							await Chat.SendWatchdogMessage(
								"Active server rebooted! Shutting down due to graceful termination request...",
								false,
								cancellationToken)
								.ConfigureAwait(false);
							return MonitorAction.Exit;
						default:
							throw new InvalidOperationException($"Invalid reboot state: {rebootState}");
					}

				case MonitorActivationReason.ActiveLaunchParametersUpdated:
					await Server.SetRebootState(Session.RebootState.Restart, cancellationToken).ConfigureAwait(false);
					gracefulRebootRequired = true;
					break;
				case MonitorActivationReason.NewDmbAvailable:
					await HandleNewDmbAvailable(cancellationToken).ConfigureAwait(false);
					break;
				case MonitorActivationReason.Heartbeat:
				default:
					throw new InvalidOperationException($"Invalid activation reason: {reason}");
			}

			return MonitorAction.Continue;
		}

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
		protected override async Task InitControllers(
			Task chatTask,
			ReattachInformation reattachInfo,
			CancellationToken cancellationToken)
		{
			// don't need a new dmb if reattaching
			var reattachInProgress = reattachInfo != null;
			var dmbToUse = reattachInProgress ? null : DmbFactory.LockNextDmb(1);

			// if this try catches something, both servers are killed
			try
			{
				// start the alpha server task, either by launch a new process or attaching to an existing one
				// The tasks returned are mainly for writing interop files to the directories among other things and should generally never fail
				// The tasks pertaining to server startup times are in the ISessionControllers
				Task<ISessionController> serverLaunchTask;
				if (!reattachInProgress)
				{
					dmbToUse = await PrepServerForLaunch(dmbToUse, cancellationToken).ConfigureAwait(false);
					serverLaunchTask = SessionControllerFactory.LaunchNew(
						dmbToUse,
						null,
						ActiveLaunchParameters,
						false,
						cancellationToken);
				}
				else
					serverLaunchTask = SessionControllerFactory.Reattach(reattachInfo, cancellationToken);

				// retrieve the session controller
				Server = await serverLaunchTask.ConfigureAwait(false);

				// failed reattaches will return null
				Server?.SetHighPriority();

				// possiblity of null servers due to failed reattaches
				if (Server == null)
				{
					await ReattachFailure(
						chatTask,
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
			if (Server.Dmb.CompileJob.DMApiVersion == null)
				return Chat.SendWatchdogMessage(
					"A new deployment has been made but cannot be applied automatically as the currently running server has no DMAPI. Please manually reboot the server to apply the update.",
					true,
					cancellationToken);
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
