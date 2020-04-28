using Byond.TopicSender;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Api.Models.Internal;
using Tgstation.Server.Host.Components.Chat;
using Tgstation.Server.Host.Components.Deployment;
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
		/// <inheritdoc />
		protected override string DeploymentTimeWhileRunning => "when DreamDaemon reboots";

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
		/// Initializes a new instance of the <see cref="WindowsWatchdog"/> <see langword="class"/>.
		/// </summary>
		/// <param name="chat">The <see cref="IChatManager"/> for the <see cref="WatchdogBase"/>.</param>
		/// <param name="sessionControllerFactory">The <see cref="ISessionControllerFactory"/> for the <see cref="WatchdogBase"/>.</param>
		/// <param name="dmbFactory">The <see cref="IDmbFactory"/> for the <see cref="WatchdogBase"/>.</param>
		/// <param name="reattachInfoHandler">The <see cref="IReattachInfoHandler"/> for the <see cref="WatchdogBase"/>.</param>
		/// <param name="databaseContextFactory">The <see cref="IDatabaseContextFactory"/> for the <see cref="WatchdogBase"/>.</param>
		/// <param name="byondTopicSender">The <see cref="IByondTopicSender"/> for the <see cref="WatchdogBase"/>.</param>
		/// <param name="eventConsumer">The <see cref="IEventConsumer"/> for the <see cref="WatchdogBase"/>.</param>
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
			IByondTopicSender byondTopicSender,
			IEventConsumer eventConsumer,
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
				byondTopicSender,
				eventConsumer,
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
		protected override void DisposeAndNullControllers()
		{
			base.DisposeAndNullControllers();

			// If we reach this point, we can guarantee PrepServerForLaunch will be called before starting again.
			activeSwappable = null;
			pendingSwappable?.Dispose();
			pendingSwappable = null;
		}

		/// <inheritdoc />
		protected override MonitorAction HandleNormalReboot()
		{
			Debug.Assert(activeSwappable != null, "Expected activeSwappable to not be null!");
			if (pendingSwappable != null)
			{
				Logger.LogTrace("Replacing activeSwappable with pendingSwappable");
				Server.ReplaceDmbProvider(pendingSwappable);
				activeSwappable = pendingSwappable;
				pendingSwappable = null;
			}

			return MonitorAction.Continue;
		}

		/// <inheritdoc />
		protected override async Task HandleNewDmbAvailable(CancellationToken cancellationToken)
		{
			IDmbProvider compileJobProvider = DmbFactory.LockNextDmb(1);
			WindowsSwappableDmbProvider windowsProvider = null;
			try
			{
				windowsProvider = new WindowsSwappableDmbProvider(compileJobProvider, ioManager, symlinkFactory);

				Logger.LogDebug("Swapping to compile job {0}...", windowsProvider.CompileJob.Id);
				Server.Suspend();
				await windowsProvider.MakeActive(cancellationToken).ConfigureAwait(false);
				Server.Resume();
			}
			catch
			{
				IDmbProvider providerToDispose = windowsProvider ?? compileJobProvider;
				providerToDispose.Dispose();
				throw;
			}

			pendingSwappable?.Dispose();
			pendingSwappable = windowsProvider;
		}

		/// <inheritdoc />
		protected override async Task<IDmbProvider> PrepServerForLaunch(IDmbProvider dmbToUse, CancellationToken cancellationToken)
		{
			Debug.Assert(activeSwappable == null, "Expected swappableDmbProvider to be null!");

			Logger.LogTrace("Prep for server launch. pendingSwappable is {0}avaiable", pendingSwappable == null ? "not " : String.Empty);

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
