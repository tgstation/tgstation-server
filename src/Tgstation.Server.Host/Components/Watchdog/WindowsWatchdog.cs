using System;
using System.Collections.Generic;
using System.Diagnostics;
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
	/// A <see cref="IWatchdog"/> that, instead of killing servers for updates, uses the wonders of symlinks to swap out changes without killing DreamDaemon.
	/// </summary>
	class WindowsWatchdog : BasicWatchdog
	{
		/// <summary>
		/// The <see cref="SwappableDmbProvider"/> for <see cref="WatchdogBase.LastLaunchParameters"/>.
		/// </summary>
		protected SwappableDmbProvider ActiveSwappable { get; private set; }

		/// <summary>
		/// The <see cref="IIOManager"/> for the <see cref="WindowsWatchdog"/> pointing to the Game directory.
		/// </summary>
		protected IIOManager GameIOManager { get; }

		/// <summary>
		/// The <see cref="ISymlinkFactory"/> for the <see cref="WindowsWatchdog"/>.
		/// </summary>
		readonly ISymlinkFactory symlinkFactory;

		/// <summary>
		/// <see cref="List{T}"/> of <see cref="Task"/>s that are waiting to clean up old deployments.
		/// </summary>
		readonly List<Task> deploymentCleanupTasks;

		/// <summary>
		/// The active <see cref="SwappableDmbProvider"/> for <see cref="WatchdogBase.ActiveLaunchParameters"/>.
		/// </summary>
		SwappableDmbProvider pendingSwappable;

		/// <summary>
		/// The <see cref="TaskCompletionSource"/> representing the cleanup of an unused <see cref="IDmbProvider"/>.
		/// </summary>
		volatile TaskCompletionSource deploymentCleanupGate;

		/// <summary>
		/// Initializes a new instance of the <see cref="WindowsWatchdog"/> class.
		/// </summary>
		/// <param name="chat">The <see cref="IChatManager"/> for the <see cref="WatchdogBase"/>.</param>
		/// <param name="sessionControllerFactory">The <see cref="ISessionControllerFactory"/> for the <see cref="WatchdogBase"/>.</param>
		/// <param name="dmbFactory">The <see cref="IDmbFactory"/> for the <see cref="WatchdogBase"/>.</param>
		/// <param name="sessionPersistor">The <see cref="ISessionPersistor"/> for the <see cref="WatchdogBase"/>.</param>
		/// <param name="jobManager">The <see cref="IJobManager"/> for the <see cref="WatchdogBase"/>.</param>
		/// <param name="serverControl">The <see cref="IServerControl"/> for the <see cref="WatchdogBase"/>.</param>
		/// <param name="asyncDelayer">The <see cref="IAsyncDelayer"/> for the <see cref="WatchdogBase"/>.</param>
		/// <param name="diagnosticsIOManager">The <see cref="IIOManager"/> for the <see cref="WatchdogBase"/>.</param>
		/// <param name="eventConsumer">The <see cref="IEventConsumer"/> for the <see cref="WatchdogBase"/>.</param>
		/// <param name="remoteDeploymentManagerFactory">The <see cref="IRemoteDeploymentManagerFactory"/> for the <see cref="WatchdogBase"/>.</param>
		/// <param name="gameIOManager">The value of <see cref="GameIOManager"/>.</param>
		/// <param name="symlinkFactory">The value of <see cref="symlinkFactory"/>.</param>
		/// <param name="logger">The <see cref="ILogger"/> for the <see cref="WatchdogBase"/>.</param>
		/// <param name="initialLaunchParameters">The <see cref="DreamDaemonLaunchParameters"/> for the <see cref="WatchdogBase"/>.</param>
		/// <param name="instance">The <see cref="Api.Models.Instance"/> for the <see cref="WatchdogBase"/>.</param>
		/// <param name="autoStart">The autostart value for the <see cref="WatchdogBase"/>.</param>
		public WindowsWatchdog(
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
			ISymlinkFactory symlinkFactory,
			ILogger<WindowsWatchdog> logger,
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
				logger,
				initialLaunchParameters,
				instance,
				autoStart)
		{
			try
			{
				GameIOManager = gameIOManager ?? throw new ArgumentNullException(nameof(gameIOManager));
				this.symlinkFactory = symlinkFactory ?? throw new ArgumentNullException(nameof(symlinkFactory));

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
		protected override async Task DisposeAndNullControllersImpl()
		{
			await base.DisposeAndNullControllersImpl();

			// If we reach this point, we can guarantee PrepServerForLaunch will be called before starting again.
			ActiveSwappable = null;
			pendingSwappable?.Dispose();
			pendingSwappable = null;

			await DrainDeploymentCleanupTasks(true);
		}

		/// <inheritdoc />
		protected override async Task<MonitorAction> HandleNormalReboot(CancellationToken cancellationToken)
		{
			if (pendingSwappable != null)
			{
				var updateTask = BeforeApplyDmb(pendingSwappable.CompileJob, cancellationToken);

				if (!pendingSwappable.Swapped)
					await PerformDmbSwap(pendingSwappable, cancellationToken);

				var currentCompileJobId = Server.ReattachInformation.Dmb.CompileJob.Id;

				await DrainDeploymentCleanupTasks(false);

				IDisposable lingeringDeployment;
				var localDeploymentCleanupGate = new TaskCompletionSource();
				async Task CleanupLingeringDeployment()
				{
					var lingeringDeploymentExpirySeconds = ActiveLaunchParameters.StartupTimeout.Value;
					Logger.LogDebug(
						"Holding old deployment {compileJobId} for up to {expiry} seconds...",
						currentCompileJobId,
						lingeringDeploymentExpirySeconds);

					var timeout = AsyncDelayer.Delay(TimeSpan.FromSeconds(lingeringDeploymentExpirySeconds), cancellationToken);

					var completedTask = await Task.WhenAny(
						localDeploymentCleanupGate.Task,
						timeout);

					var timedOut = completedTask == timeout;
					Logger.Log(
						timedOut
							? LogLevel.Warning
							: LogLevel.Trace,
						"Releasing old deployment {compileJobId}{afterTimeout}",
						timedOut
							? " due to timeout!"
							: "...");

					lingeringDeployment.Dispose();
				}

				var oldDeploymentCleanupGate = Interlocked.Exchange(ref deploymentCleanupGate, localDeploymentCleanupGate);
				oldDeploymentCleanupGate?.TrySetResult();

				Logger.LogTrace("Replacing activeSwappable with pendingSwappable...");

				lock (deploymentCleanupTasks)
				{
					lingeringDeployment = Server.ReplaceDmbProvider(pendingSwappable);
					deploymentCleanupTasks.Add(
						CleanupLingeringDeployment());
				}

				ActiveSwappable = pendingSwappable;
				pendingSwappable = null;

				await SessionPersistor.Save(Server.ReattachInformation, cancellationToken);
				await updateTask;
			}
			else
				Logger.LogTrace("Nothing to do as pendingSwappable is null.");

			return MonitorAction.Continue;
		}

		/// <inheritdoc />
		protected override async Task HandleNewDmbAvailable(CancellationToken cancellationToken)
		{
			IDmbProvider compileJobProvider = DmbFactory.LockNextDmb(1);
			bool canSeamlesslySwap = true;

			if (compileJobProvider.CompileJob.ByondVersion != ActiveCompileJob.ByondVersion)
			{
				// have to do a graceful restart
				Logger.LogDebug(
					"Not swapping to new compile job {0} as it uses a different BYOND version ({1}) than what is currently active {2}. Queueing graceful restart instead...",
					compileJobProvider.CompileJob.Id,
					compileJobProvider.CompileJob.ByondVersion,
					ActiveCompileJob.ByondVersion);
				canSeamlesslySwap = false;
			}

			if (compileJobProvider.CompileJob.DmeName != ActiveCompileJob.DmeName)
			{
				Logger.LogDebug(
					"Not swapping to new compile job {0} as it uses a different .dmb name ({1}) than what is currently active {2}. Queueing graceful restart instead...",
					compileJobProvider.CompileJob.Id,
					compileJobProvider.CompileJob.DmeName,
					ActiveCompileJob.DmeName);
				canSeamlesslySwap = false;
			}

			if (!canSeamlesslySwap)
			{
				compileJobProvider.Dispose();
				await base.HandleNewDmbAvailable(cancellationToken);
				return;
			}

			SwappableDmbProvider windowsProvider = null;
			try
			{
				windowsProvider = new SwappableDmbProvider(compileJobProvider, GameIOManager, symlinkFactory);
				if (ActiveCompileJob.DMApiVersion == null)
				{
					Logger.LogWarning("Active compile job has no DMAPI! Commencing immediate .dmb swap. Note this behavior is known to be buggy in some DM code contexts. See https://github.com/tgstation/tgstation-server/issues/1550");
					await PerformDmbSwap(windowsProvider, cancellationToken);
				}
			}
			catch (Exception ex)
			{
				Logger.LogError(ex, "Exception while swapping");
				IDmbProvider providerToDispose = windowsProvider ?? compileJobProvider;
				providerToDispose.Dispose();
				throw;
			}

			pendingSwappable?.Dispose();
			pendingSwappable = windowsProvider;
		}

		/// <inheritdoc />
		protected sealed override async Task<IDmbProvider> PrepServerForLaunch(IDmbProvider dmbToUse, CancellationToken cancellationToken)
		{
			if (ActiveSwappable != null)
				throw new InvalidOperationException("Expected activeSwappable to be null!");
			if (pendingSwappable != null)
				throw new InvalidOperationException("Expected pendingSwappable to be null!");

			Logger.LogTrace("Prep for server launch");

			ActiveSwappable = new SwappableDmbProvider(dmbToUse, GameIOManager, symlinkFactory);
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
		/// <returns>A <see cref="Task"/> representing the running operation.</returns>
		protected virtual async Task ApplyInitialDmb(CancellationToken cancellationToken)
		{
			Server.ReattachInformation.InitialDmb = await DmbFactory.FromCompileJob(Server.CompileJob, cancellationToken);
		}

		/// <inheritdoc />
		protected override async Task SessionStartupPersist(CancellationToken cancellationToken)
		{
			await ApplyInitialDmb(cancellationToken);
			await base.SessionStartupPersist(cancellationToken);
		}

		/// <inheritdoc />
		protected override async Task<MonitorAction> HandleMonitorWakeup(MonitorActivationReason reason, CancellationToken cancellationToken)
		{
			var result = await base.HandleMonitorWakeup(reason, cancellationToken);
			if (reason == MonitorActivationReason.ActiveServerStartup)
				await DrainDeploymentCleanupTasks(false);

			return result;
		}

		/// <summary>
		/// Create the initial link to the live game directory using <see cref="ActiveSwappable"/>.
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task"/> representing the running operation.</returns>
		protected virtual Task InitialLink(CancellationToken cancellationToken)
		{
			Logger.LogTrace("Symlinking compile job...");
			return ActiveSwappable.MakeActive(cancellationToken);
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

			var suspended = false;
			var server = Server;
			try
			{
				server.Suspend();
				suspended = true;
			}
			catch (Exception ex)
			{
				Logger.LogWarning(ex, "Exception while suspending server!");
			}

			try
			{
				await newProvider.MakeActive(cancellationToken);
			}
			finally
			{
				// Let this throw hard if it fails
				if (suspended)
					server.Resume();
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
