using System;
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
	/// A variant of the <see cref="WindowsWatchdog"/> that works on POSIX systems.
	/// </summary>
	sealed class PosixWatchdog : WindowsWatchdog
	{
		/// <summary>
		/// If the swappable game directory is currently a rename of the compile job.
		/// </summary>
		IDmbProvider hardLinkedDmb;

		/// <summary>
		/// Initializes a new instance of the <see cref="PosixWatchdog"/> class.
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
			ISessionPersistor sessionPersistor,
			IJobManager jobManager,
			IServerControl serverControl,
			IAsyncDelayer asyncDelayer,
			IIOManager diagnosticsIOManager,
			IEventConsumer eventConsumer,
			IRemoteDeploymentManagerFactory remoteDeploymentManagerFactory,
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
				  sessionPersistor,
				  jobManager,
				  serverControl,
				  asyncDelayer,
				  diagnosticsIOManager,
				  eventConsumer,
				  remoteDeploymentManagerFactory,
				  gameIOManager,
				  symlinkFactory,
				  logger,
				  initialLaunchParameters,
				  instance,
				  autoStart)
		{
		}

		/// <inheritdoc />
		protected override Task ApplyInitialDmb(CancellationToken cancellationToken) => Task.CompletedTask; // not necessary to hold initial .dmb on Linux because of based inode deletes

		/// <inheritdoc />
		protected override async Task InitialLink(CancellationToken cancellationToken)
		{
			// The logic to check for an active live directory is in SwappableDmbProvider, so we just do it again here for safety
			Logger.LogTrace("Hard linking compile job...");

			// Symlinks are counted as a file on linux??
			if (await GameIOManager.DirectoryExists(ActiveSwappable.Directory, cancellationToken))
				await GameIOManager.DeleteDirectory(ActiveSwappable.Directory, cancellationToken);
			else
				await GameIOManager.DeleteFile(ActiveSwappable.Directory, cancellationToken);

			// Instead of symlinking to begin with we actually rename the directory
			await GameIOManager.MoveDirectory(
				ActiveSwappable.CompileJob.DirectoryName.ToString(),
				ActiveSwappable.Directory,
				cancellationToken);

			hardLinkedDmb = ActiveSwappable;
		}

		/// <inheritdoc />
		protected override async Task InitController(Task chatTask, ReattachInformation reattachInfo, CancellationToken cancellationToken)
		{
			var suspended = false;
			try
			{
				await base.InitController(chatTask, reattachInfo, cancellationToken);
			}
			finally
			{
				// Then we move it back and apply the symlink
				if (hardLinkedDmb != null)
				{
					try
					{
						Logger.LogTrace("Unhardlinking compile job...");
						Server?.Suspend();
						suspended = true;
						var hardLink = hardLinkedDmb.Directory;
						var originalPosition = hardLinkedDmb.CompileJob.DirectoryName.ToString();
						await GameIOManager.MoveDirectory(
							hardLink,
							originalPosition,
							default);
					}
					catch (Exception ex)
					{
						Logger.LogError(
							ex,
							"Failed to un-hard link compile job #{compileJobId} ({compileJobDirectory})",
							hardLinkedDmb.CompileJob.Id,
							hardLinkedDmb.CompileJob.DirectoryName);
					}

					hardLinkedDmb = null;
				}
			}

			if (reattachInfo != null)
			{
				Logger.LogTrace("Skipping symlink due to reattach");
				return;
			}

			Logger.LogTrace("Symlinking compile job...");
			await ActiveSwappable.MakeActive(cancellationToken);
			if (suspended)
				Server.Resume();
		}
	}
}
