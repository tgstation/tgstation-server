using Byond.TopicSender;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using Tgstation.Server.Api.Models.Internal;
using Tgstation.Server.Host.Components.Chat;
using Tgstation.Server.Host.Components.Compiler;
using Tgstation.Server.Host.Configuration;
using Tgstation.Server.Host.Core;
using Tgstation.Server.Host.IO;

namespace Tgstation.Server.Host.Components.Watchdog
{
	/// <summary>
	/// <see cref="IWatchdogFactory"/> for creating <see cref="WindowsWatchdog"/>s.
	/// </summary>
	sealed class WindowsWatchdogFactory : WatchdogFactory
	{
		/// <summary>
		/// The <see cref="ISymlinkFactory"/> for the <see cref="WindowsWatchdogFactory"/>.
		/// </summary>
		readonly ISymlinkFactory symlinkFactory;

		/// <summary>
		/// Initializes a new instance of the <see cref="WindowsWatchdogFactory"/> <see langword="class"/>.
		/// </summary>
		/// <param name="serverControl">The <see cref="IServerControl"/> for the <see cref="WatchdogFactory"/>.</param>
		/// <param name="loggerFactory">The <see cref="ILoggerFactory"/> for the <see cref="WatchdogFactory"/>.</param>
		/// <param name="databaseContextFactory">The <see cref="IDatabaseContextFactory"/> for the <see cref="WatchdogFactory"/>.</param>
		/// <param name="byondTopicSender">The <see cref="IByondTopicSender"/> for the <see cref="WatchdogFactory"/>.</param>
		/// <param name="jobManager">The <see cref="IJobManager"/> for the <see cref="WatchdogFactory"/>.</param>
		/// <param name="asyncDelayer">The <see cref="IAsyncDelayer"/> for the <see cref="WatchdogFactory"/>.</param>
		/// <param name="symlinkFactory">The value of <see cref="symlinkFactory"/>.</param>
		/// <param name="generalConfigurationOptions">The <see cref="IOptions{TOptions}"/> for <see cref="GeneralConfiguration"/> for the <see cref="WatchdogFactory"/>.</param>
		public WindowsWatchdogFactory(
			IServerControl serverControl,
			ILoggerFactory loggerFactory,
			IDatabaseContextFactory databaseContextFactory,
			IByondTopicSender byondTopicSender,
			IJobManager jobManager,
			IAsyncDelayer asyncDelayer,
			ISymlinkFactory symlinkFactory,
			IOptions<GeneralConfiguration> generalConfigurationOptions)
			: base(serverControl,
				  loggerFactory,
				  databaseContextFactory,
				  byondTopicSender,
				  jobManager,
				  asyncDelayer,
				  generalConfigurationOptions)
		{
			this.symlinkFactory = symlinkFactory ?? throw new ArgumentNullException(nameof(symlinkFactory));
		}

		/// <inheritdoc />
		protected override IWatchdog CreateNonExperimentalWatchdog(
			IChat chat,
			IDmbFactory dmbFactory,
			IReattachInfoHandler reattachInfoHandler,
			IEventConsumer eventConsumer,
			ISessionControllerFactory sessionControllerFactory,
			IIOManager ioManager,
			Api.Models.Instance instance,
			DreamDaemonSettings settings)
			=> new WindowsWatchdog(
				chat,
				sessionControllerFactory,
				dmbFactory,
				reattachInfoHandler,
				DatabaseContextFactory,
				ByondTopicSender,
				eventConsumer,
				JobManager,
				ServerControl,
				AsyncDelayer,
				ioManager,
				symlinkFactory,
				LoggerFactory.CreateLogger<WindowsWatchdog>(),
				settings,
				instance,
				settings.AutoStart.Value);
	}
}
