using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

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
	/// A variant of the <see cref="AdvancedWatchdog"/> that works on Windows systems.
	/// </summary>
	sealed class WindowsWatchdog : AdvancedWatchdog
	{
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
		/// <param name="gameIOManager">The <see cref="IIOManager"/> pointing to the game directory for the <see cref="AdvancedWatchdog"/>..</param>
		/// <param name="linkFactory">The <see cref="IFilesystemLinkFactory"/> for the <see cref="AdvancedWatchdog"/>.</param>
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
			IFilesystemLinkFactory linkFactory,
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
				  gameIOManager,
				  linkFactory,
				  logger,
				  initialLaunchParameters,
				  instance,
				  autoStart)
		{
		}

		/// <inheritdoc />
		protected override async ValueTask ApplyInitialDmb(CancellationToken cancellationToken)
		{
			if (Server!.EngineVersion.Engine != EngineType.Byond)
			{
				Logger.LogTrace("Not setting InitialDmb for engine type {engineType}", Server.EngineVersion.Engine);
				return;
			}

			Server.ReattachInformation.InitialDmb = await DmbFactory.FromCompileJob(Server.CompileJob, "WindowsWatchdog Initial Deployment", cancellationToken);
		}

		/// <inheritdoc />
		protected override SwappableDmbProvider CreateSwappableDmbProvider(IDmbProvider dmbProvider)
			=> new SymlinkDmbProvider(dmbProvider, GameIOManager, LinkFactory);
	}
}
