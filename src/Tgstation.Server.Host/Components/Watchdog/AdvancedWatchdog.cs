using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using Prometheus;

using Tgstation.Server.Api.Models;
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
	/// A <see cref="IWatchdog"/> that, instead of killing servers for updates, uses the wonders of filesystem links to swap out changes without killing the server process.
	/// </summary>
	abstract class AdvancedWatchdog : BasicWatchdog
	{
		/// <summary>
		/// The <see cref="SwappableDmbProvider"/> for <see cref="WatchdogBase.LastLaunchParameters"/>.
		/// </summary>
		protected SwappableDmbProvider? ActiveSwappable { get; private set; }

		/// <summary>
		/// The <see cref="IFilesystemLinkFactory"/> for the <see cref="AdvancedWatchdog"/>.
		/// </summary>
		protected IFilesystemLinkFactory LinkFactory { get; }

		/// <summary>
		/// <see cref="List{T}"/> of <see cref="Task"/>s that are waiting to clean up old deployments.
		/// </summary>
		readonly List<Task> deploymentCleanupTasks;

		/// <summary>
		/// The active <see cref="SwappableDmbProvider"/> for <see cref="WatchdogBase.ActiveLaunchParameters"/>.
		/// </summary>
		SwappableDmbProvider? pendingSwappable;

		/// <summary>
		/// The <see cref="TaskCompletionSource"/> representing the cleanup of an unused <see cref="IDmbProvider"/>.
		/// </summary>
		volatile TaskCompletionSource? deploymentCleanupGate;

		/// <summary>
		/// Initializes a new instance of the <see cref="AdvancedWatchdog"/> class.
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
		/// <param name="metricFactory">The <see cref="IMetricFactory"/> for the <see cref="WatchdogBase"/>.</param>
		/// <param name="gameIOManager">The 'Game' <see cref="IIOManager"/> for the <see cref="WatchdogBase"/>.</param>
		/// <param name="linkFactory">The value of <see cref="LinkFactory"/>.</param>
		/// <param name="logger">The <see cref="ILogger"/> for the <see cref="WatchdogBase"/>.</param>
		/// <param name="initialLaunchParameters">The <see cref="DreamDaemonLaunchParameters"/> for the <see cref="WatchdogBase"/>.</param>
		/// <param name="instance">The <see cref="Api.Models.Instance"/> for the <see cref="WatchdogBase"/>.</param>
		/// <param name="autoStart">The autostart value for the <see cref="WatchdogBase"/>.</param>
		public AdvancedWatchdog(
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
			IMetricFactory metricFactory,
			IIOManager gameIOManager,
			IFilesystemLinkFactory linkFactory,
			ILogger<AdvancedWatchdog> logger,
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
				metricFactory,
				gameIOManager,
				logger,
				initialLaunchParameters,
				instance,
				autoStart)
		{
			try
			{
				LinkFactory = linkFactory ?? throw new ArgumentNullException(nameof(linkFactory));

				deploymentCleanupTasks = new List<Task>();
			}
			catch
			{
				// Async dispose is for if we have controllers running, not the case here
				var disposeTask = DisposeAsync();
				Debug.Assert(disposeTask.IsCompleted, "This should always be true during construction!");
				disposeTask.GetAwaiter().GetResult();

				throw;
			}
		}

		/// <inheritdoc />
		protected sealed override async ValueTask DisposeAndNullControllersImpl()
		{
			await base.DisposeAndNullControllersImpl();

			// If we reach this point, we can guarantee PrepServerForLaunch will be called before starting again.
			ActiveSwappable = null;

			if (pendingSwappable != null)
			{
				await pendingSwappable.DisposeAsync();
				pendingSwappable = null;
			}

			await DrainDeploymentCleanupTasks(true);
		}

		/// <inheritdoc />
		protected sealed override async ValueTask<MonitorAction> HandleNormalReboot(CancellationToken cancellationToken)
		{
			if (pendingSwappable != null)
			{
				var needToSwap = !pendingSwappable.Swapped;
				var controller = Server!;
				if (needToSwap)
				{
					// IMPORTANT: THE SESSIONCONTROLLER SHOULD STILL BE PROCESSING THE BRIDGE REQUEST SO WE KNOW DD IS SLEEPING
					// OTHERWISE, IT COULD RETURN TO /world/Reboot() TOO EARLY AND LOAD THE WRONG .DMB
					if (!controller.ProcessingRebootBridgeRequest)
					{
						// integration test logging will catch this
						Logger.LogError(
							"The reboot bridge request completed before the watchdog could suspend the server! This can lead to buggy DreamDaemon behaviour and should be reported! To ensure stability, we will need to hard reboot the server");
						return MonitorAction.Restart;
					}

					// DCT: Not necessary
					if (!pendingSwappable.FinishActivationPreparation(CancellationToken.None).IsCompleted)
					{
						// rare pokemon
						Logger.LogInformation("Deployed .dme is not ready to swap, delaying until next reboot!");
						Chat.QueueWatchdogMessage("The pending deployment was not ready to be activated this reboot. It will be applied at the next one.");
						return MonitorAction.Continue;
					}
				}

				var updateTask = BeforeApplyDmb(pendingSwappable.CompileJob, cancellationToken);
				if (needToSwap)
					await PerformDmbSwap(pendingSwappable, cancellationToken);

				var currentCompileJobId = controller.ReattachInformation.Dmb.CompileJob.Id;

				await DrainDeploymentCleanupTasks(false);

				IAsyncDisposable lingeringDeployment;
				var localDeploymentCleanupGate = new TaskCompletionSource();
				async Task CleanupLingeringDeployment()
				{
					var lingeringDeploymentExpirySeconds = ActiveLaunchParameters.StartupTimeout!.Value;
					Logger.LogDebug(
						"Holding old deployment {compileJobId} for up to {expiry} seconds...",
						currentCompileJobId,
						lingeringDeploymentExpirySeconds);

					// DCT: A cancel firing here can result in us leaving a dmbprovider undisposed, localDeploymentCleanupGate will always fire in that case
					var timeout = AsyncDelayer.Delay(TimeSpan.FromSeconds(lingeringDeploymentExpirySeconds), CancellationToken.None).AsTask();

					var completedTask = await Task.WhenAny(
						localDeploymentCleanupGate.Task,
						timeout);

					var timedOut = completedTask == timeout;
					Logger.Log(
						timedOut
							? LogLevel.Warning
							: LogLevel.Trace,
						"Releasing old deployment {compileJobId}{afterTimeout}",
						currentCompileJobId,
						timedOut
							? " due to timeout!"
							: "...");

					await lingeringDeployment.DisposeAsync();
				}

				var oldDeploymentCleanupGate = Interlocked.Exchange(ref deploymentCleanupGate, localDeploymentCleanupGate);
				oldDeploymentCleanupGate?.TrySetResult();

				Logger.LogTrace("Replacing activeSwappable with pendingSwappable...");

				lock (deploymentCleanupTasks)
				{
					lingeringDeployment = controller.ReplaceDmbProvider(pendingSwappable);
					deploymentCleanupTasks.Add(
						CleanupLingeringDeployment());
				}

				ActiveSwappable = pendingSwappable;
				pendingSwappable = null;

				await SessionPersistor.Update(controller.ReattachInformation, cancellationToken);
				await updateTask;
			}
			else
				Logger.LogTrace("Nothing to do as pendingSwappable is null.");

			return await base.HandleNormalReboot(cancellationToken);
		}

		/// <inheritdoc />
		protected sealed override async ValueTask HandleNewDmbAvailable(CancellationToken cancellationToken)
		{
			IDmbProvider compileJobProvider = DmbFactory.LockNextDmb("AdvancedWatchdog next compile job preload");
			bool canSeamlesslySwap = CanUseSwappableDmbProvider(compileJobProvider);
			if (canSeamlesslySwap)
				if (compileJobProvider.CompileJob.EngineVersion != ActiveCompileJob!.EngineVersion)
				{
					// have to do a graceful restart
					Logger.LogDebug(
						"Not swapping to new compile job {compileJobId} as it uses a different engine version ({newEngineVersion}) than what is currently active {oldEngineVersion}.",
						compileJobProvider.CompileJob.Id,
						compileJobProvider.CompileJob.EngineVersion,
						ActiveCompileJob.EngineVersion);
					canSeamlesslySwap = false;
				}
				else if (compileJobProvider.CompileJob.DmeName != ActiveCompileJob.DmeName)
				{
					Logger.LogDebug(
						"Not swapping to new compile job {compileJobId} as it uses a different .dmb name ({newDmbName}) than what is currently active {oldDmbName}.",
						compileJobProvider.CompileJob.Id,
						compileJobProvider.CompileJob.DmeName,
						ActiveCompileJob.DmeName);
					canSeamlesslySwap = false;
				}

			if (!canSeamlesslySwap)
			{
				Logger.LogDebug("Queueing graceful restart instead...");
				await compileJobProvider.DisposeAsync();
				await base.HandleNewDmbAvailable(cancellationToken);
				return;
			}

			SwappableDmbProvider? swappableProvider = null;
			try
			{
				swappableProvider = CreateSwappableDmbProvider(compileJobProvider);
				if (ActiveCompileJob!.DMApiVersion == null)
				{
					Logger.LogWarning("Active compile job has no DMAPI! Commencing immediate .dmb swap. Note this behavior is known to be buggy in some DM code contexts. See https://github.com/tgstation/tgstation-server/issues/1550");
					await PerformDmbSwap(swappableProvider, cancellationToken);
				}
			}
			catch (Exception ex)
			{
				Logger.LogError(ex, "Exception while swapping");
				IDmbProvider providerToDispose = swappableProvider ?? compileJobProvider;
				await providerToDispose.DisposeAsync();
				throw;
			}

			await (pendingSwappable?.DisposeAsync() ?? ValueTask.CompletedTask);
			pendingSwappable = swappableProvider;
		}

		/// <inheritdoc />
		protected sealed override async ValueTask<IDmbProvider> PrepServerForLaunch(IDmbProvider dmbToUse, CancellationToken cancellationToken)
		{
			if (ActiveSwappable != null)
				throw new InvalidOperationException("Expected activeSwappable to be null!");
			if (pendingSwappable != null)
				throw new InvalidOperationException("Expected pendingSwappable to be null!");

			Logger.LogTrace("Prep for server launch");
			if (!CanUseSwappableDmbProvider(dmbToUse))
				return dmbToUse;

			ActiveSwappable = CreateSwappableDmbProvider(dmbToUse);
			try
			{
				await InitialLink(cancellationToken);
			}
			catch (Exception ex)
			{
				// We won't worry about disposing activeSwappable here as we can't dispose dmbToUse here.
				Logger.LogTrace(ex, "Initial link error, nulling ActiveSwappable");
				ActiveSwappable = null;
				throw;
			}

			return ActiveSwappable;
		}

		/// <summary>
		/// Set the <see cref="ReattachInformation.InitialDmb"/> for the <see cref="BasicWatchdog.Server"/>.
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask"/> representing the running operation.</returns>
		protected abstract ValueTask ApplyInitialDmb(CancellationToken cancellationToken);

		/// <summary>
		/// Create a <see cref="SwappableDmbProvider"/> for a given <paramref name="dmbProvider"/>.
		/// </summary>
		/// <param name="dmbProvider">The <see cref="IDmbProvider"/> to create a <see cref="SwappableDmbProvider"/> for.</param>
		/// <returns>A new <see cref="SwappableDmbProvider"/>.</returns>
		protected abstract SwappableDmbProvider CreateSwappableDmbProvider(IDmbProvider dmbProvider);

		/// <inheritdoc />
		protected override async ValueTask SessionStartupPersist(CancellationToken cancellationToken)
		{
			await ApplyInitialDmb(cancellationToken);
			await base.SessionStartupPersist(cancellationToken);
		}

		/// <inheritdoc />
		protected override async ValueTask<MonitorAction> HandleMonitorWakeup(MonitorActivationReason reason, CancellationToken cancellationToken)
		{
			var result = await base.HandleMonitorWakeup(reason, cancellationToken);
			if (reason == MonitorActivationReason.ActiveServerStartup)
				await DrainDeploymentCleanupTasks(false);

			return result;
		}

		/// <summary>
		/// If the <see cref="SwappableDmbProvider"/> feature of the <see cref="AdvancedWatchdog"/> can be used with a given <paramref name="dmbProvider"/>.
		/// </summary>
		/// <param name="dmbProvider">The <see cref="IDmbProvider"/> that is to be activated.</param>
		/// <returns><see langword="true"/> if swapping is possible, <see langword="false"/> otherwise.</returns>
		bool CanUseSwappableDmbProvider(IDmbProvider dmbProvider)
		{
			if (dmbProvider.EngineVersion.Engine != EngineType.Byond)
			{
				Logger.LogDebug("Not using SwappableDmbProvider for engine type {engineType}", dmbProvider.EngineVersion.Engine);
				return false;
			}

			return true;
		}

		/// <summary>
		/// Create the initial link to the live game directory using <see cref="ActiveSwappable"/>.
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask"/> representing the running operation.</returns>
		async ValueTask InitialLink(CancellationToken cancellationToken)
		{
			await ActiveSwappable!.FinishActivationPreparation(cancellationToken);
			Logger.LogTrace("Linking compile job...");
			await ActiveSwappable.MakeActive(cancellationToken);
		}

		/// <summary>
		/// Suspends the <see cref="BasicWatchdog.Server"/> and calls <see cref="SwappableDmbProvider.MakeActive(CancellationToken)"/> on a <paramref name="newProvider"/>.
		/// </summary>
		/// <param name="newProvider">The <see cref="SwappableDmbProvider"/> to activate.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask"/> representing the running operation.</returns>
		async ValueTask PerformDmbSwap(SwappableDmbProvider newProvider, CancellationToken cancellationToken)
		{
			Logger.LogDebug("Swapping to compile job {id}...", newProvider.CompileJob.Id);

			await newProvider.FinishActivationPreparation(cancellationToken);

			var suspended = false;
			var server = Server!;
			try
			{
				server.SuspendProcess();
				suspended = true;
			}
			catch (Exception ex)
			{
				Logger.LogWarning(ex, "Exception while suspending server!");
			}

			try
			{
				Logger.LogTrace("Making new provider {id} active...", newProvider.CompileJob.Id);
				await newProvider.MakeActive(cancellationToken);
			}
			finally
			{
				// Let this throw hard if it fails
				if (suspended)
					server.ResumeProcess();
			}
		}

		/// <summary>
		/// Asynchronously drain <see cref="deploymentCleanupTasks"/>.
		/// </summary>
		/// <param name="blocking">If <see langword="true"/>, all <see cref="Task"/>s will be <see langword="await"/>ed. Otherwise, only <see cref="Task"/>s with <see cref="Task.IsCompleted"/> set will be <see langword="await"/>ed.</param>
		/// <returns>A <see cref="Task"/> representing the running operation.</returns>
		Task DrainDeploymentCleanupTasks(bool blocking)
		{
			Logger.LogTrace("DrainDeploymentCleanupTasks...");
			var localDeploymentCleanupGate = Interlocked.Exchange(ref deploymentCleanupGate, null);
			localDeploymentCleanupGate?.TrySetResult();

			List<Task> localDeploymentCleanupTasks;
			lock (deploymentCleanupTasks)
			{
				var totalActiveTasks = deploymentCleanupTasks.Count;
				localDeploymentCleanupTasks = new List<Task>(totalActiveTasks);
				for (var i = totalActiveTasks - 1; i >= 0; --i)
				{
					var currentTask = deploymentCleanupTasks[i];
					if (!blocking && !currentTask.IsCompleted)
						continue;

					localDeploymentCleanupTasks.Add(currentTask);
					deploymentCleanupTasks.RemoveAt(i);
				}
			}

			return Task.WhenAll(localDeploymentCleanupTasks);
		}
	}
}
