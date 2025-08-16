using System;

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
	/// <inheritdoc />
	class WatchdogFactory : IWatchdogFactory
	{
		/// <summary>
		/// The <see cref="IServerControl"/> for the <see cref="WatchdogFactory"/>.
		/// </summary>
		protected IServerControl ServerControl { get; }

		/// <summary>
		/// The <see cref="ILoggerFactory"/> for the <see cref="WatchdogFactory"/>.
		/// </summary>
		protected ILoggerFactory LoggerFactory { get; }

		/// <summary>
		/// The <see cref="IJobManager"/> for the <see cref="WatchdogFactory"/>.
		/// </summary>
		protected IJobManager JobManager { get; }

		/// <summary>
		/// The <see cref="IAsyncDelayer"/> for the <see cref="WatchdogFactory"/>.
		/// </summary>
		protected IAsyncDelayer AsyncDelayer { get; }

		/// <summary>
		/// The <see cref="IOptionsMonitor{TOptions}"/> of <see cref="GeneralConfiguration"/> for the <see cref="WatchdogFactory"/>.
		/// </summary>
		protected IOptionsMonitor<GeneralConfiguration> GeneralConfigurationOptions { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="WatchdogFactory"/> class.
		/// </summary>
		/// <param name="serverControl">The value of <see cref="ServerControl"/>.</param>
		/// <param name="loggerFactory">The value of <see cref="LoggerFactory"/>.</param>
		/// <param name="jobManager">The value of <see cref="JobManager"/>.</param>
		/// <param name="asyncDelayer">The value of <see cref="AsyncDelayer"/>.</param>
		/// <param name="generalConfigurationOptions">The <see cref="IOptions{TOptions}"/> containing the value of <see cref="GeneralConfigurationOptions"/>.</param>
		public WatchdogFactory(
			IServerControl serverControl,
			ILoggerFactory loggerFactory,
			IJobManager jobManager,
			IAsyncDelayer asyncDelayer,
			IOptionsMonitor<GeneralConfiguration> generalConfigurationOptions)
		{
			ServerControl = serverControl ?? throw new ArgumentNullException(nameof(serverControl));
			LoggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
			JobManager = jobManager ?? throw new ArgumentNullException(nameof(jobManager));
			AsyncDelayer = asyncDelayer ?? throw new ArgumentNullException(nameof(asyncDelayer));
			GeneralConfigurationOptions = generalConfigurationOptions ?? throw new ArgumentNullException(nameof(generalConfigurationOptions));
		}

		/// <inheritdoc />
		public virtual IWatchdog CreateWatchdog(
			IChatManager chat,
			IDmbFactory dmbFactory,
			ISessionPersistor sessionPersistor,
			ISessionControllerFactory sessionControllerFactory,
			IIOManager gameIOManager,
			IIOManager diagnosticsIOManager,
			IEventConsumer eventConsumer,
			IRemoteDeploymentManagerFactory remoteDeploymentManagerfactory,
			IMetricFactory metricFactory,
			Api.Models.Instance instance,
			DreamDaemonSettings settings)
			=> new BasicWatchdog(
				chat,
				sessionControllerFactory,
				dmbFactory,
				sessionPersistor,
				JobManager,
				ServerControl,
				AsyncDelayer,
				diagnosticsIOManager,
				eventConsumer,
				remoteDeploymentManagerfactory,
				metricFactory,
				gameIOManager,
				LoggerFactory.CreateLogger<BasicWatchdog>(),
				settings,
				instance,
				settings.AutoStart ?? throw new ArgumentNullException(nameof(settings)));
	}
}
