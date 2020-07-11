using Microsoft.Extensions.Logging;
using System;
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
		/// The active <see cref="SwappableDmbProvider"/> for <see cref="WatchdogBase.ActiveLaunchParameters"/>.
		/// </summary>
		SwappableDmbProvider pendingSwappable;

		/// <summary>
		/// The <see cref="IDmbProvider"/> the <see cref="WindowsWatchdog"/> was started with.
		/// </summary>
		IDmbProvider startupDmbProvider;

		/// <summary>
		/// Initializes a new instance of the <see cref="WindowsWatchdog"/> <see langword="class"/>.
		/// </summary>
		/// <param name="chat">The <see cref="IChatManager"/> for the <see cref="WatchdogBase"/>.</param>
		/// <param name="sessionControllerFactory">The <see cref="ISessionControllerFactory"/> for the <see cref="WatchdogBase"/>.</param>
		/// <param name="dmbFactory">The <see cref="IDmbFactory"/> for the <see cref="WatchdogBase"/>.</param>
		/// <param name="sessionPersistor">The <see cref="ISessionPersistor"/> for the <see cref="WatchdogBase"/>.</param>
		/// <param name="databaseContextFactory">The <see cref="IDatabaseContextFactory"/> for the <see cref="WatchdogBase"/>.</param>
		/// <param name="jobManager">The <see cref="IJobManager"/> for the <see cref="WatchdogBase"/>.</param>
		/// <param name="serverControl">The <see cref="IServerControl"/> for the <see cref="WatchdogBase"/>.</param>
		/// <param name="asyncDelayer">The <see cref="IAsyncDelayer"/> for the <see cref="WatchdogBase"/>.</param>
		/// <param name="diagnosticsIOManager">The <see cref="IIOManager"/> for the <see cref="WatchdogBase"/>.</param>
		/// <param name="eventConsumer">The <see cref="IEventConsumer"/> for the <see cref="WatchdogBase"/>.</param>
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
			IDatabaseContextFactory databaseContextFactory,
			IJobManager jobManager,
			IServerControl serverControl,
			IAsyncDelayer asyncDelayer,
			IIOManager diagnosticsIOManager,
			IEventConsumer eventConsumer,
			IIOManager gameIOManager,
			ISymlinkFactory symlinkFactory,
			ILogger<WindowsWatchdog> logger,
			DreamDaemonLaunchParameters initialLaunchParameters,
			Api.Models.Instance instance, bool autoStart)
			: base(
				chat,
				sessionControllerFactory,
				dmbFactory,
				sessionPersistor,
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
		{
			try
			{
				GameIOManager = gameIOManager ?? throw new ArgumentNullException(nameof(gameIOManager));
				this.symlinkFactory = symlinkFactory ?? throw new ArgumentNullException(nameof(symlinkFactory));
			}
			catch
			{
				var _ = DisposeAsync();
				throw;
			}
		}

		/// <inheritdoc />
		protected override async Task DisposeAndNullControllersImpl()
		{
			await base.DisposeAndNullControllersImpl().ConfigureAwait(false);

			// If we reach this point, we can guarantee PrepServerForLaunch will be called before starting again.
			ActiveSwappable = null;
			pendingSwappable?.Dispose();
			pendingSwappable = null;

			startupDmbProvider?.Dispose();
			startupDmbProvider = null;
		}

		/// <inheritdoc />
		protected override MonitorAction HandleNormalReboot()
		{
			if (pendingSwappable != null)
			{
				Logger.LogTrace("Replacing activeSwappable with pendingSwappable...");
				Server.ReplaceDmbProvider(pendingSwappable);
				ActiveSwappable = pendingSwappable;
				pendingSwappable = null;
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
				await base.HandleNewDmbAvailable(cancellationToken).ConfigureAwait(false);
				return;
			}

			SwappableDmbProvider windowsProvider = null;
			bool suspended = false;
			try
			{
				windowsProvider = new SwappableDmbProvider(compileJobProvider, GameIOManager, symlinkFactory);

				Logger.LogDebug("Swapping to compile job {0}...", windowsProvider.CompileJob.Id);
				try
				{
					Server.Suspend();
					suspended = true;
				}
				catch (Exception ex)
				{
					Logger.LogWarning("Exception while suspending server: {0}", ex);
				}

				await windowsProvider.MakeActive(cancellationToken).ConfigureAwait(false);
			}
			catch (Exception ex)
			{
				Logger.LogError("Exception while swapping: {0}", ex);
				IDmbProvider providerToDispose = windowsProvider ?? compileJobProvider;
				providerToDispose.Dispose();
				throw;
			}

			// Let this throw hard if it fails
			if (suspended)
				Server.Resume();

			pendingSwappable?.Dispose();
			pendingSwappable = windowsProvider;
		}

		/// <inheritdoc />
		protected sealed override async Task<IDmbProvider> PrepServerForLaunch(IDmbProvider dmbToUse, CancellationToken cancellationToken)
		{
			if(ActiveSwappable != null)
				throw new InvalidOperationException("Expected activeSwappable to be null!");
			if(startupDmbProvider != null)
				throw new InvalidOperationException("Expected startupDmbProvider to be null!");

			Logger.LogTrace("Prep for server launch. pendingSwappable is {0}available", pendingSwappable == null ? "not " : String.Empty);

			// Add another lock to the startup DMB because it'll be used throughout the lifetime of the watchdog
			startupDmbProvider = await DmbFactory.FromCompileJob(dmbToUse.CompileJob, cancellationToken).ConfigureAwait(false);

			ActiveSwappable = pendingSwappable ?? new SwappableDmbProvider(dmbToUse, GameIOManager, symlinkFactory);
			pendingSwappable = null;

			try
			{
				await InitialLink(cancellationToken).ConfigureAwait(false);
			}
			catch
			{
				// We won't worry about disposing activeSwappable here as we can't dispose dmbToUse here.
				ActiveSwappable = null;
				throw;
			}

			return ActiveSwappable;
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
	}
}
