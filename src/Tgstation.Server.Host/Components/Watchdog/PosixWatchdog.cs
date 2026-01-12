using System;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Prometheus;

using Tgstation.Server.Api.Models.Internal;
using Tgstation.Server.Host.Components.Chat;
using Tgstation.Server.Host.Components.Deployment;
using Tgstation.Server.Host.Components.Deployment.Remote;
using Tgstation.Server.Host.Components.Events;
using Tgstation.Server.Host.Components.Session;
using Tgstation.Server.Host.Configuration;
using Tgstation.Server.Host.Core;
using Tgstation.Server.Host.IO;
using Tgstation.Server.Host.Jobs;
using Tgstation.Server.Host.Utils;

namespace Tgstation.Server.Host.Components.Watchdog
{
	/// <summary>
	/// A variant of the <see cref="AdvancedWatchdog"/> that works on POSIX systems.
	/// </summary>
	[UnsupportedOSPlatform("windows")]
	sealed class PosixWatchdog : AdvancedWatchdog
	{
		/// <summary>
		/// The <see cref="IOptionsMonitor{TOptions}"/> of <see cref="GeneralConfiguration"/> for the <see cref="PosixWatchdog"/>.
		/// </summary>
		readonly IOptionsMonitor<GeneralConfiguration> generalConfigurationOptions;

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
		/// <param name="metricFactory">The <see cref="IMetricFactory"/> for the <see cref="WatchdogBase"/>.</param>
		/// <param name="gameIOManager">The <see cref="IIOManager"/> pointing to the game directory for the <see cref="AdvancedWatchdog"/>..</param>
		/// <param name="linkFactory">The <see cref="IFilesystemLinkFactory"/> for the <see cref="AdvancedWatchdog"/>.</param>
		/// <param name="generalConfigurationOptions">The value of <see cref="generalConfigurationOptions"/>.</param>
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
			IMetricFactory metricFactory,
			IIOManager gameIOManager,
			IFilesystemLinkFactory linkFactory,
			IOptionsMonitor<GeneralConfiguration> generalConfigurationOptions,
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
				  metricFactory,
				  gameIOManager,
				  linkFactory,
				  logger,
				  initialLaunchParameters,
				  instance,
				  autoStart)
		{
			this.generalConfigurationOptions = generalConfigurationOptions ?? throw new ArgumentNullException(nameof(generalConfigurationOptions));
		}

		/// <inheritdoc />
		protected override ValueTask ApplyInitialDmb(CancellationToken cancellationToken)
			=> ValueTask.CompletedTask; // not necessary to hold initial .dmb on Linux because of based inode deletes

		/// <inheritdoc />
		protected override SwappableDmbProvider CreateSwappableDmbProvider(IDmbProvider dmbProvider)
			=> new HardLinkDmbProvider(
				dmbProvider,
				GameIOManager,
				LinkFactory,
				Logger,
				generalConfigurationOptions.CurrentValue,
				ActiveLaunchParameters.SecurityLevel!.Value);
	}
}
