using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using Tgstation.Server.Api.Models.Internal;
using Tgstation.Server.Host.Components.Chat;
using Tgstation.Server.Host.Components.Deployment;
using Tgstation.Server.Host.Components.Session;
using Tgstation.Server.Host.Configuration;
using Tgstation.Server.Host.Core;
using Tgstation.Server.Host.Database;
using Tgstation.Server.Host.IO;
using Tgstation.Server.Host.Jobs;

namespace Tgstation.Server.Host.Components.Watchdog
{
	/// <inheritdoc />
	class WatchdogFactory : IWatchdogFactory
	{
		/// <summary>
		/// The <see cref="IServerControl"/> for the <see cref="WatchdogFactory"/>
		/// </summary>
		protected IServerControl ServerControl { get; }

		/// <summary>
		/// The <see cref="ILoggerFactory"/> for the <see cref="WatchdogFactory"/>
		/// </summary>
		protected ILoggerFactory LoggerFactory { get; }

		/// <summary>
		/// The <see cref="IDatabaseContextFactory"/> for the <see cref="WatchdogFactory"/>
		/// </summary>
		protected IDatabaseContextFactory DatabaseContextFactory { get; }

		/// <summary>
		/// The <see cref="IJobManager"/> for the <see cref="WatchdogFactory"/>
		/// </summary>
		protected IJobManager JobManager { get; }

		/// <summary>
		/// The <see cref="IAsyncDelayer"/> for the <see cref="WatchdogFactory"/>
		/// </summary>
		protected IAsyncDelayer AsyncDelayer { get; }

		/// <summary>
		/// The <see cref="Configuration.GeneralConfiguration"/> for the <see cref="WatchdogFactory"/>
		/// </summary>
		protected GeneralConfiguration GeneralConfiguration { get; }

		/// <summary>
		/// Construct a <see cref="WatchdogFactory"/>
		/// </summary>
		/// <param name="serverControl">The value of <see cref="ServerControl"/></param>
		/// <param name="loggerFactory">The value of <see cref="LoggerFactory"/></param>
		/// <param name="databaseContextFactory">The value of <see cref="DatabaseContextFactory"/></param>
		/// <param name="jobManager">The value of <see cref="JobManager"/></param>
		/// <param name="asyncDelayer">The value of <see cref="AsyncDelayer"/></param>
		/// <param name="generalConfigurationOptions">The <see cref="IOptions{TOptions}"/> containing the value of <see cref="GeneralConfiguration"/></param>
		public WatchdogFactory(
			IServerControl serverControl,
			ILoggerFactory loggerFactory,
			IDatabaseContextFactory databaseContextFactory,
			IJobManager jobManager,
			IAsyncDelayer asyncDelayer,
			IOptions<GeneralConfiguration> generalConfigurationOptions)
		{
			ServerControl = serverControl ?? throw new ArgumentNullException(nameof(serverControl));
			LoggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
			DatabaseContextFactory = databaseContextFactory ?? throw new ArgumentNullException(nameof(databaseContextFactory));
			JobManager = jobManager ?? throw new ArgumentNullException(nameof(jobManager));
			AsyncDelayer = asyncDelayer ?? throw new ArgumentNullException(nameof(asyncDelayer));
			GeneralConfiguration = generalConfigurationOptions?.Value ?? throw new ArgumentNullException(nameof(generalConfigurationOptions));
		}

		/// <inheritdoc />
		public IWatchdog CreateWatchdog(
			IChatManager chat,
			IDmbFactory dmbFactory,
			IReattachInfoHandler reattachInfoHandler,
			ISessionControllerFactory sessionControllerFactory,
			IIOManager gameIOManager,
			IIOManager diagnosticsIOManager,
			Api.Models.Instance instance,
			DreamDaemonSettings settings)
		{
			if (GeneralConfiguration.UseExperimentalWatchdog)
				return new ExperimentalWatchdog(
					chat,
					sessionControllerFactory,
					dmbFactory,
					reattachInfoHandler,
					DatabaseContextFactory,
					JobManager,
					ServerControl,
					AsyncDelayer,
					diagnosticsIOManager,
					LoggerFactory.CreateLogger<ExperimentalWatchdog>(),
					settings,
					instance,
					settings.AutoStart.Value);

			return CreateNonExperimentalWatchdog(
				chat,
				dmbFactory,
				reattachInfoHandler,
				sessionControllerFactory,
				gameIOManager,
				diagnosticsIOManager,
				instance,
				settings);
		}

		/// <summary>
		/// Create a <see cref="IWatchdog"/> that isn't the <see cref="ExperimentalWatchdog"/>.
		/// </summary>
		/// <param name="chat">The <see cref="IChatManager"/> for the <see cref="IWatchdog"/></param>
		/// <param name="dmbFactory">The <see cref="IDmbFactory"/> for the <see cref="IWatchdog"/> with</param>
		/// <param name="reattachInfoHandler">The <see cref="IReattachInfoHandler"/> for the <see cref="IWatchdog"/></param>
		/// <param name="sessionControllerFactory">The <see cref="ISessionControllerFactory"/> for the <see cref="IWatchdog"/></param>
		/// <param name="gameIOManager">The <see cref="IIOManager"/> pointing to the Game directory for the <see cref="IWatchdog"/>.</param>
		/// <param name="diagnosticsIOManager">The <see cref="IIOManager"/> pointing to the Diagnostics directory for the <see cref="IWatchdog"/>.</param>
		/// <param name="instance">The <see cref="Instance"/> for the <see cref="IWatchdog"/></param>
		/// <param name="settings">The initial <see cref="DreamDaemonSettings"/> for the <see cref="IWatchdog"/></param>
		/// <returns>A new <see cref="IWatchdog"/></returns>
		protected virtual IWatchdog CreateNonExperimentalWatchdog(
			IChatManager chat,
			IDmbFactory dmbFactory,
			IReattachInfoHandler reattachInfoHandler,
			ISessionControllerFactory sessionControllerFactory,
			IIOManager gameIOManager,
			IIOManager diagnosticsIOManager,
			Api.Models.Instance instance,
			DreamDaemonSettings settings)
			=> new BasicWatchdog(
				chat,
				sessionControllerFactory,
				dmbFactory,
				reattachInfoHandler,
				DatabaseContextFactory,
				JobManager,
				ServerControl,
				AsyncDelayer,
				diagnosticsIOManager,
				LoggerFactory.CreateLogger<BasicWatchdog>(),
				settings,
				instance,
				settings.AutoStart.Value);
	}
}
