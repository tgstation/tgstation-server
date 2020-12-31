using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
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

namespace Tgstation.Server.Host.Components.Watchdog
{
	/// <summary>
	/// <see cref="IWatchdogFactory"/> for creating <see cref="WindowsWatchdog"/>s.
	/// </summary>
	class WindowsWatchdogFactory : WatchdogFactory
	{
		/// <summary>
		/// The <see cref="ISymlinkFactory"/> for the <see cref="WindowsWatchdogFactory"/>.
		/// </summary>
		protected ISymlinkFactory SymlinkFactory { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="WindowsWatchdogFactory"/> <see langword="class"/>.
		/// </summary>
		/// <param name="serverControl">The <see cref="IServerControl"/> for the <see cref="WatchdogFactory"/>.</param>
		/// <param name="loggerFactory">The <see cref="ILoggerFactory"/> for the <see cref="WatchdogFactory"/>.</param>
		/// <param name="jobManager">The <see cref="IJobManager"/> for the <see cref="WatchdogFactory"/>.</param>
		/// <param name="asyncDelayer">The <see cref="IAsyncDelayer"/> for the <see cref="WatchdogFactory"/>.</param>
		/// <param name="symlinkFactory">The value of <see cref="SymlinkFactory"/>.</param>
		/// <param name="generalConfigurationOptions">The <see cref="IOptions{TOptions}"/> for <see cref="GeneralConfiguration"/> for the <see cref="WatchdogFactory"/>.</param>
		public WindowsWatchdogFactory(
			IServerControl serverControl,
			ILoggerFactory loggerFactory,
			IJobManager jobManager,
			IAsyncDelayer asyncDelayer,
			ISymlinkFactory symlinkFactory,
			IOptions<GeneralConfiguration> generalConfigurationOptions)
			: base(
				serverControl,
				loggerFactory,
				jobManager,
				asyncDelayer,
				generalConfigurationOptions)
		{
			SymlinkFactory = symlinkFactory ?? throw new ArgumentNullException(nameof(symlinkFactory));
		}

		/// <inheritdoc />
		public override IWatchdog CreateWatchdog(
			IChatManager chat,
			IDmbFactory dmbFactory,
			ISessionPersistor sessionPersistor,
			ISessionControllerFactory sessionControllerFactory,
			IIOManager gameIOManager,
			IIOManager diagnosticsIOManager,
			IEventConsumer eventConsumer,
			IRemoteDeploymentManagerFactory remoteDeploymentManagerFactory,
			Api.Models.Instance instance,
			DreamDaemonSettings settings)
			=> new WindowsWatchdog(
				chat,
				sessionControllerFactory,
				dmbFactory,
				sessionPersistor,
				JobManager,
				ServerControl,
				AsyncDelayer,
				diagnosticsIOManager,
				eventConsumer,
				remoteDeploymentManagerFactory,
				gameIOManager,
				SymlinkFactory,
				LoggerFactory.CreateLogger<WindowsWatchdog>(),
				settings,
				instance,
				settings.AutoStart ?? throw new ArgumentNullException(nameof(settings)));
	}
}
