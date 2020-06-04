using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Api.Models.Internal;
using Tgstation.Server.Host.Components.Chat;
using Tgstation.Server.Host.Components.Deployment;
using Tgstation.Server.Host.Components.Session;
using Tgstation.Server.Host.Core;
using Tgstation.Server.Host.Database;
using Tgstation.Server.Host.IO;
using Tgstation.Server.Host.Jobs;

namespace Tgstation.Server.Host.Components.Watchdog
{
	/// <summary>
	/// A version of the <see cref="BasicWatchdog"/> that, instead of killing servers for updates, uses the wonders of symlinks to swap out changes without killing DreamDaemon.
	/// </summary>
	sealed class WindowsWatchdog : BasicWatchdog
	{
		/// <summary>
		/// The <see cref="IIOManager"/> for the <see cref="WindowsWatchdog"/>.
		/// </summary>
		readonly IIOManager ioManager;

		/// <summary>
		/// The <see cref="ISymlinkFactory"/> for the <see cref="WindowsWatchdog"/>.
		/// </summary>
		readonly ISymlinkFactory symlinkFactory;

		/// <summary>
		/// The <see cref="WindowsSwappableDmbProvider"/> for <see cref="WatchdogBase.LastLaunchParameters"/>.
		/// </summary>
		WindowsSwappableDmbProvider activeSwappable;

		/// <summary>
		/// The active <see cref="WindowsSwappableDmbProvider"/> for <see cref="WatchdogBase.ActiveLaunchParameters"/>.
		/// </summary>
		WindowsSwappableDmbProvider pendingSwappable;

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
		/// <param name="reattachInfoHandler">The <see cref="IReattachInfoHandler"/> for the <see cref="WatchdogBase"/>.</param>
		/// <param name="databaseContextFactory">The <see cref="IDatabaseContextFactory"/> for the <see cref="WatchdogBase"/>.</param>
		/// <param name="jobManager">The <see cref="IJobManager"/> for the <see cref="WatchdogBase"/>.</param>
		/// <param name="serverControl">The <see cref="IServerControl"/> for the <see cref="WatchdogBase"/>.</param>
		/// <param name="asyncDelayer">The <see cref="IAsyncDelayer"/> for the <see cref="WatchdogBase"/>.</param>
		/// <param name="ioManager">The value of <see cref="ioManager"/>.</param>
		/// <param name="symlinkFactory">The value of <see cref="symlinkFactory"/>.</param>
		/// <param name="logger">The <see cref="ILogger"/> for the <see cref="WatchdogBase"/>.</param>
		/// <param name="initialLaunchParameters">The <see cref="DreamDaemonLaunchParameters"/> for the <see cref="WatchdogBase"/>.</param>
		/// <param name="instance">The <see cref="Api.Models.Instance"/> for the <see cref="WatchdogBase"/>.</param>
		/// <param name="autoStart">The autostart value for the <see cref="WatchdogBase"/>.</param>
		public WindowsWatchdog(
			IChatManager chat,
			ISessionControllerFactory sessionControllerFactory,
			IDmbFactory dmbFactory,
			IReattachInfoHandler reattachInfoHandler,
			IDatabaseContextFactory databaseContextFactory,
			IJobManager jobManager,
			IServerControl serverControl,
			IAsyncDelayer asyncDelayer,
			IIOManager ioManager,
			ISymlinkFactory symlinkFactory,
			ILogger<WindowsWatchdog> logger,
			DreamDaemonLaunchParameters initialLaunchParameters,
			Api.Models.Instance instance, bool autoStart)
			: base(
				chat,
				sessionControllerFactory,
				dmbFactory,
				reattachInfoHandler,
				databaseContextFactory,
				jobManager,
				serverControl,
				asyncDelayer,
				logger,
				initialLaunchParameters,
				instance,
				autoStart)
		{
			try
			{
				this.ioManager = ioManager ?? throw new ArgumentNullException(nameof(ioManager));
				this.symlinkFactory = symlinkFactory ?? throw new ArgumentNullException(nameof(symlinkFactory));
			}
			catch
			{
				Dispose();
				throw;
			}
		}

		/// <inheritdoc />
		protected override void DisposeAndNullControllersImpl()
		{
			base.DisposeAndNullControllersImpl();

			// If we reach this point, we can guarantee PrepServerForLaunch will be called before starting again.
			activeSwappable = null;
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
				activeSwappable = pendingSwappable;
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
			if (compileJobProvider.CompileJob.ByondVersion != ActiveCompileJob.ByondVersion)
			{
				// have to do a graceful restart
				Logger.LogDebug(
					"Not swapping to new compile job {0} as it uses a different BYOND version ({1}) than what is currently active {2}. Queueing graceful restart instead...",
					compileJobProvider.CompileJob.Id,
					compileJobProvider.CompileJob.ByondVersion,
					ActiveCompileJob.ByondVersion);
				compileJobProvider.Dispose();
				await base.HandleNewDmbAvailable(cancellationToken).ConfigureAwait(false);
				return;
			}

			WindowsSwappableDmbProvider windowsProvider = null;
			bool suspended = false;
			try
			{
				windowsProvider = new WindowsSwappableDmbProvider(compileJobProvider, ioManager, symlinkFactory);

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
			catch(Exception ex)
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
		protected override async Task<IDmbProvider> PrepServerForLaunch(IDmbProvider dmbToUse, CancellationToken cancellationToken)
		{
			if(activeSwappable != null)
				throw new InvalidOperationException("Expected activeSwappable to be null!");
			if(startupDmbProvider != null)
				throw new InvalidOperationException("Expected startupDmbProvider to be null!");

			Logger.LogTrace("Prep for server launch. pendingSwappable is {0}available", pendingSwappable == null ? "not " : String.Empty);

			// Add another lock to the startup DMB because it'll be used throughout the lifetime of the watchdog
			startupDmbProvider = await DmbFactory.FromCompileJob(dmbToUse.CompileJob, cancellationToken).ConfigureAwait(false);

			activeSwappable = pendingSwappable ?? new WindowsSwappableDmbProvider(dmbToUse, ioManager, symlinkFactory);
			pendingSwappable = null;

			try
			{
				await activeSwappable.MakeActive(cancellationToken).ConfigureAwait(false);
			}
			catch
			{
				// We won't worry about disposing activeSwappable here as we can't dispose dmbToUse here.
				activeSwappable = null;
				throw;
			}

			return activeSwappable;
		}
	}
}
