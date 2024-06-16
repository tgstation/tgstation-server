using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using Tgstation.Server.Api.Models.Internal;
using Tgstation.Server.Host.Components.Chat;
using Tgstation.Server.Host.Components.Deployment;
using Tgstation.Server.Host.Components.Deployment.Remote;
using Tgstation.Server.Host.Components.Events;
using Tgstation.Server.Host.Components.Session;
using Tgstation.Server.Host.Core;
using Tgstation.Server.Host.IO;
using Tgstation.Server.Host.Jobs;
using Tgstation.Server.Host.Utils;

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
		public sealed override RebootState? RebootState => Server?.RebootState;

		/// <summary>
		/// The single <see cref="ISessionController"/>.
		/// </summary>
		protected ISessionController? Server { get; private set; }

		/// <summary>
		/// If the server is set to gracefully reboot due to a pending dmb or settings change.
		/// </summary>
		bool gracefulRebootRequired;

		/// <summary>
		/// Initializes a new instance of the <see cref="BasicWatchdog"/> class.
		/// </summary>
		/// <param name="chat">The <see cref="IChatManager"/> for the <see cref="WatchdogBase"/>.</param>
		/// <param name="sessionControllerFactory">The <see cref="ISessionControllerFactory"/> for the <see cref="WatchdogBase"/>.</param>
		/// <param name="dmbFactory">The <see cref="IDmbFactory"/> for the <see cref="WatchdogBase"/>.</param>
		/// <param name="sessionPersistor">The <see cref="ISessionPersistor"/> for the <see cref="WatchdogBase"/>.</param>
		/// <param name="jobManager">The <see cref="IJobManager"/> for the <see cref="WatchdogBase"/>.</param>
		/// <param name="serverControl">The <see cref="IServerControl"/> for the <see cref="WatchdogBase"/>.</param>
		/// <param name="asyncDelayer">The <see cref="IAsyncDelayer"/> for the <see cref="WatchdogBase"/>.</param>
		/// <param name="diagnosticsIOManager">The 'Diagnostics' <see cref="IIOManager"/> for the <see cref="WatchdogBase"/>.</param>
		/// <param name="eventConsumer">The <see cref="IEventConsumer"/> for the <see cref="WatchdogBase"/>.</param>
		/// <param name="remoteDeploymentManagerFactory">The <see cref="IRemoteDeploymentManagerFactory"/> for the <see cref="WatchdogBase"/>.</param>
		/// <param name="gameIOManager">The 'Game' <see cref="IIOManager"/> for the <see cref="WatchdogBase"/>.</param>
		/// <param name="logger">The <see cref="ILogger"/> for the <see cref="WatchdogBase"/>.</param>
		/// <param name="initialLaunchParameters">The <see cref="DreamDaemonLaunchParameters"/> for the <see cref="WatchdogBase"/>.</param>
		/// <param name="instance">The <see cref="Api.Models.Instance"/> for the <see cref="WatchdogBase"/>.</param>
		/// <param name="autoStart">The autostart value for the <see cref="WatchdogBase"/>.</param>
		public BasicWatchdog(
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
			ILogger<BasicWatchdog> logger,
			DreamDaemonLaunchParameters initialLaunchParameters,
			Api.Models.Instance instance,
			bool autoStart)
			: base(
				 chat,
				 sessionControllerFactory,
				 dmbFactory,
				 sessionPersistor,
				 jobManager,
				 serverControl,
				 asyncDelayer,
				 diagnosticsIOManager,
				 eventConsumer,
				 remoteDeploymentManagerFactory,
				 gameIOManager,
				 logger,
				 initialLaunchParameters,
				 instance,
				 autoStart)
		{
		}

		/// <inheritdoc />
		public override ValueTask ResetRebootState(CancellationToken cancellationToken)
		{
			if (!gracefulRebootRequired)
				return base.ResetRebootState(cancellationToken);

			return Restart(true, cancellationToken);
		}

		/// <inheritdoc />
		public sealed override ValueTask InstanceRenamed(string newInstanceName, CancellationToken cancellationToken)
			=> Server?.InstanceRenamed(newInstanceName, cancellationToken) ?? ValueTask.CompletedTask;

		/// <inheritdoc />
		protected override async ValueTask<MonitorAction> HandleMonitorWakeup(MonitorActivationReason reason, CancellationToken cancellationToken)
		{
			var controller = Server!;
			switch (reason)
			{
				case MonitorActivationReason.ActiveServerCrashed:
					var eventType = controller.TerminationWasIntentional
						? EventType.WorldEndProcess
						: EventType.WatchdogCrash;
					await HandleEventImpl(eventType, Enumerable.Empty<string>(), false, cancellationToken);

					var exitWord = controller.TerminationWasIntentional ? "exited" : "crashed";
					if (controller.RebootState == Session.RebootState.Shutdown)
					{
						// the time for graceful shutdown is now
						Chat.QueueWatchdogMessage(
							String.Format(
								CultureInfo.InvariantCulture,
								"Server {0}! Shutting down due to graceful termination request...",
								exitWord));
						return MonitorAction.Exit;
					}

					Chat.QueueWatchdogMessage(
						String.Format(
							CultureInfo.InvariantCulture,
							"Server {0}! Rebooting...",
							exitWord));
					return MonitorAction.Restart;
				case MonitorActivationReason.ActiveServerRebooted:
					var rebootState = controller.RebootState;
					if (gracefulRebootRequired && rebootState == Session.RebootState.Normal)
					{
						Logger.LogError("Watchdog reached normal reboot state with gracefulRebootRequired set!");
						rebootState = Session.RebootState.Restart;
					}

					gracefulRebootRequired = false;
					controller.ResetRebootState();

					var eventTask = HandleEventImpl(EventType.WorldReboot, Enumerable.Empty<string>(), false, cancellationToken);
					try
					{
						switch (rebootState)
						{
							case Session.RebootState.Normal:
								return await HandleNormalReboot(cancellationToken);
							case Session.RebootState.Restart:
								return MonitorAction.Restart;
							case Session.RebootState.Shutdown:
								// graceful shutdown time
								Chat.QueueWatchdogMessage(
									"Active server rebooted! Shutting down due to graceful termination request...");
								return MonitorAction.Exit;
							default:
								throw new InvalidOperationException($"Invalid reboot state: {rebootState}");
						}
					}
					finally
					{
						await eventTask;
					}

				case MonitorActivationReason.ActiveLaunchParametersUpdated:
					await controller.SetRebootState(Session.RebootState.Restart, cancellationToken);
					gracefulRebootRequired = true;
					break;
				case MonitorActivationReason.NewDmbAvailable:
					await HandleNewDmbAvailable(cancellationToken);
					break;
				case MonitorActivationReason.ActiveServerPrimed:
					await HandleEventImpl(EventType.WorldPrime, Enumerable.Empty<string>(), false, cancellationToken);
					break;
				case MonitorActivationReason.ActiveServerStartup:
					Status = Api.Models.WatchdogStatus.Online;
					break;
				case MonitorActivationReason.HealthCheck:
				default:
					throw new InvalidOperationException($"Invalid activation reason: {reason}");
			}

			return MonitorAction.Continue;
		}

		/// <inheritdoc />
		protected override async ValueTask DisposeAndNullControllersImpl()
		{
			var disposeTask = Server?.DisposeAsync();
			gracefulRebootRequired = false;
			if (!disposeTask.HasValue)
				return;

			await disposeTask.Value;
			Server = null;
		}

		/// <inheritdoc />
		protected sealed override ISessionController? GetActiveController() => Server;

		/// <inheritdoc />
		protected override async ValueTask InitController(
			ValueTask eventTask,
			ReattachInformation? reattachInfo,
			CancellationToken cancellationToken)
		{
			// don't need a new dmb if reattaching
			var reattachInProgress = reattachInfo != null;
			var dmbToUse = reattachInProgress ? null : DmbFactory.LockNextDmb("Watchdog initialization");

			// if this try catches something, both servers are killed
			try
			{
				// start the alpha server task, either by launch a new process or attaching to an existing one
				// The tasks returned are mainly for writing interop files to the directories among other things and should generally never fail
				// The tasks pertaining to server startup times are in the ISessionControllers
				if (!reattachInProgress)
				{
					Logger.LogTrace("Initializing controller with CompileJob {compileJobId}...", dmbToUse!.CompileJob.Id);
					await BeforeApplyDmb(dmbToUse.CompileJob, cancellationToken);
					dmbToUse = await PrepServerForLaunch(dmbToUse, cancellationToken);

					await eventTask;
					Server = await SessionControllerFactory.LaunchNew(
						dmbToUse,
						null,
						ActiveLaunchParameters,
						false,
						cancellationToken);
				}
				else
				{
					await eventTask;
					Server = await SessionControllerFactory.Reattach(reattachInfo!, cancellationToken);
				}

				// possiblity of null servers due to failed reattaches
				if (Server == null)
				{
					await ReattachFailure(
						cancellationToken);
					return;
				}

				if (!reattachInProgress)
					await SessionStartupPersist(cancellationToken);

				if (!SessionId.HasValue)
					Logger.LogError("Server should have a session ID allocated by now but it doesn't!");
				else
					Logger.LogInformation("Watchdog starting session ID {id}", SessionId.Value);

				await CheckLaunchResult(Server, "Server", cancellationToken);

				// persist again, because the DMAPI can say we need a different topic port (Original OD behavior)
				// kinda hacky imo, but at least we can safely forget about this
				if (!reattachInProgress)
					await SessionPersistor.Update(Server.ReattachInformation, cancellationToken);
			}
			catch (Exception ex)
			{
				Logger.LogTrace(ex, "Controller initialization failure!");

				// kill the controllers
				bool serverWasActive = Server != null;

				// DCT: Operation must always run
				await DisposeAndNullControllers(CancellationToken.None);

				// server didn't get control of this dmb
				if (dmbToUse != null && !serverWasActive)
					await dmbToUse.DisposeAsync();

				throw;
			}
		}

		/// <summary>
		/// Called to save the current <see cref="Server"/> into the <see cref="WatchdogBase.SessionPersistor"/> when initially launched.
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask"/> representing the running operation.</returns>
		protected virtual ValueTask SessionStartupPersist(CancellationToken cancellationToken)
			=> SessionPersistor.Save(Server!.ReattachInformation, cancellationToken);

		/// <summary>
		/// Handler for <see cref="MonitorActivationReason.ActiveServerRebooted"/> when the <see cref="RebootState"/> is <see cref="RebootState.Normal"/>.
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in the <see cref="MonitorAction"/> to take.</returns>
		protected virtual ValueTask<MonitorAction> HandleNormalReboot(CancellationToken cancellationToken)
		{
			if (Server!.CompileJob.DMApiVersion != null)
				Status = Api.Models.WatchdogStatus.Restoring;

			return ValueTask.FromResult(MonitorAction.Continue);
		}

		/// <summary>
		/// Handler for <see cref="MonitorActivationReason.NewDmbAvailable"/>.
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask"/> representing the running operation.</returns>
		protected virtual async ValueTask HandleNewDmbAvailable(CancellationToken cancellationToken)
		{
			gracefulRebootRequired = true;
			if (Server!.CompileJob.DMApiVersion == null)
			{
				Chat.QueueWatchdogMessage(
					"A new deployment has been made but cannot be applied automatically as the currently running server has no DMAPI. Please manually reboot the server to apply the update.");
				return;
			}

			await Server.SetRebootState(Session.RebootState.Restart, cancellationToken);
		}

		/// <summary>
		/// Prepare the server to launch a new instance with the <see cref="WatchdogBase.ActiveLaunchParameters"/> and a given <paramref name="dmbToUse"/>.
		/// </summary>
		/// <param name="dmbToUse">The <see cref="IDmbProvider"/> to be launched. Will not be disposed by this function.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in the modified <see cref="IDmbProvider"/> to be used.</returns>
		protected virtual ValueTask<IDmbProvider> PrepServerForLaunch(IDmbProvider dmbToUse, CancellationToken cancellationToken) => ValueTask.FromResult(dmbToUse);
	}
}
