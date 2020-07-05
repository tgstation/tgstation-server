using Microsoft.Extensions.Logging;
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
	/// A variant of the <see cref="WindowsWatchdog"/> that works on POSIX systems.
	/// </summary>
	sealed class PosixWatchdog : WindowsWatchdog
	{
		/// <summary>
		/// If the swappable game directory is currently a rename of the compile job.
		/// </summary>
		bool directoryHardLinked;

		/// <summary>
		/// Initializes a new instance of the <see cref="PosixWatchdog"/> <see langword="class"/>.
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
		/// <param name="gameIOManager">The <see cref="IIOManager"/> pointing to the game directory for the <see cref="WindowsWatchdog"/>..</param>
		/// <param name="symlinkFactory">The <see cref="ISymlinkFactory"/> for the <see cref="WindowsWatchdog"/>.</param>
		/// <param name="logger">The <see cref="ILogger"/> for the <see cref="WatchdogBase"/>.</param>
		/// <param name="initialLaunchParameters">The <see cref="DreamDaemonLaunchParameters"/> for the <see cref="WatchdogBase"/>.</param>
		/// <param name="instance">The <see cref="Api.Models.Instance"/> for the <see cref="WatchdogBase"/>.</param>
		/// <param name="autoStart">The autostart value for the <see cref="WatchdogBase"/>.</param>
		public PosixWatchdog(
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
			IIOManager gameIOManager,
			ISymlinkFactory symlinkFactory,
			ILogger<PosixWatchdog> logger,
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
				  gameIOManager,
				  symlinkFactory,
				  logger,
				  initialLaunchParameters,
				  instance,
				  autoStart)
		{ }

		/// <inheritdoc />
		protected override async Task InitialLink(SwappableDmbProvider swappableDmbProvider, CancellationToken cancellationToken)
		{
			// Instead of symlinking to begin with we actually rename the directory
			Logger.LogTrace("Hard linking compile job...");
			await GameIOManager.MoveDirectory(
				swappableDmbProvider.CompileJob.DirectoryName.ToString(),
				swappableDmbProvider.Directory,
				cancellationToken)
				.ConfigureAwait(false);
			directoryHardLinked = true;
		}

		/// <inheritdoc />
		protected override async Task InitControllers(Task chatTask, ReattachInformation reattachInfo, CancellationToken cancellationToken)
		{
			try
			{
				await base.InitControllers(chatTask, reattachInfo, cancellationToken).ConfigureAwait(false);
			}
			finally
			{
				// Then we move it back and apply the symlink
				if (directoryHardLinked)
				{
					Logger.LogTrace("Unhardlinking compile job...");
					Server?.Suspend();
					await GameIOManager.MoveDirectory(
						ActiveSwappable.Directory,
						ActiveSwappable.CompileJob.DirectoryName.ToString(),
						default)
						.ConfigureAwait(false);
					directoryHardLinked = false;
				}
			}

			await ActiveSwappable.MakeActive(cancellationToken).ConfigureAwait(false);
			Server.Resume();
		}
	}
}
