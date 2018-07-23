using Byond.TopicSender;
using Microsoft.Extensions.Logging;
using System;
using Tgstation.Server.Host.Components.Chat;
using Tgstation.Server.Host.Components.Chat.Commands;
using Tgstation.Server.Host.Components.Watchdog;
using Tgstation.Server.Host.Core;
using Tgstation.Server.Host.Security;

namespace Tgstation.Server.Host.Components
{
	/// <inheritdoc />
	sealed class InstanceFactory : IInstanceFactory
	{
		/// <summary>
		/// The <see cref="IIOManager"/> for the <see cref="InstanceFactory"/>
		/// </summary>
		readonly IIOManager ioManager;

		/// <summary>
		/// The <see cref="IDatabaseContextFactory"/> for the <see cref="InstanceFactory"/>
		/// </summary>
		readonly IDatabaseContextFactory databaseContextFactory;

		/// <summary>
		/// The <see cref="IApplication"/> for the <see cref="InstanceFactory"/>
		/// </summary>
		readonly IApplication application;

		/// <summary>
		/// The <see cref="ILoggerFactory"/> for the <see cref="InstanceFactory"/>
		/// </summary>
		readonly ILoggerFactory loggerFactory;

		/// <summary>
		/// The <see cref="IByondTopicSender"/> for the <see cref="InstanceFactory"/>
		/// </summary>
		readonly IByondTopicSender byondTopicSender;

		/// <summary>
		/// The <see cref="IServerUpdater"/> for the <see cref="InstanceFactory"/>
		/// </summary>
		readonly IServerUpdater serverUpdater;

		/// <summary>
		/// The <see cref="ICryptographySuite"/> for the <see cref="InstanceFactory"/>
		/// </summary>
		readonly ICryptographySuite cryptographySuite;

		/// <summary>
		/// The <see cref="IExecutor"/> for the <see cref="InstanceFactory"/>
		/// </summary>
		readonly IExecutor executor;

		/// <summary>
		/// The <see cref="ICommandFactory"/> for the <see cref="InstanceFactory"/>
		/// </summary>
		readonly ICommandFactory commandFactory;

		/// <summary>
		/// The <see cref="ISynchronousIOManager"/> for the <see cref="InstanceFactory"/>
		/// </summary>
		readonly ISynchronousIOManager synchronousIOManager;

		/// <summary>
		/// Construct an <see cref="InstanceFactory"/>
		/// </summary>
		/// <param name="ioManager">The value of <see cref="ioManager"/></param>
		/// <param name="databaseContextFactory">The value of <see cref="databaseContextFactory"/></param>
		/// <param name="application">The value of <see cref="application"/></param>
		/// <param name="loggerFactory">The value of <see cref="loggerFactory"/></param>
		/// <param name="byondTopicSender">The value of <see cref="byondTopicSender"/></param>
		/// <param name="serverUpdater">The value of <see cref="serverUpdater"/></param>
		/// <param name="cryptographySuite">The value of <see cref="cryptographySuite"/></param>
		/// <param name="executor">The value of <see cref="executor"/></param>
		/// <param name="commandFactory">The value of <see cref="commandFactory"/></param>
		/// <param name="synchronousIOManager">The value of <see cref="synchronousIOManager"/></param>
		public InstanceFactory(IIOManager ioManager, IDatabaseContextFactory databaseContextFactory, IApplication application, ILoggerFactory loggerFactory, IByondTopicSender byondTopicSender, IServerUpdater serverUpdater, ICryptographySuite cryptographySuite, IExecutor executor, ICommandFactory commandFactory, ISynchronousIOManager synchronousIOManager)
		{
			this.ioManager = ioManager ?? throw new ArgumentNullException(nameof(ioManager));
			this.databaseContextFactory = databaseContextFactory ?? throw new ArgumentNullException(nameof(databaseContextFactory));
			this.application = application ?? throw new ArgumentNullException(nameof(application));
			this.loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
			this.byondTopicSender = byondTopicSender ?? throw new ArgumentNullException(nameof(byondTopicSender));
			this.serverUpdater = serverUpdater ?? throw new ArgumentNullException(nameof(serverUpdater));
			this.cryptographySuite = cryptographySuite ?? throw new ArgumentNullException(nameof(cryptographySuite	));
			this.executor = executor ?? throw new ArgumentNullException(nameof(executor));
			this.commandFactory = commandFactory ?? throw new ArgumentNullException(nameof(commandFactory));
			this.synchronousIOManager = synchronousIOManager ?? throw new ArgumentNullException(nameof(synchronousIOManager));
		}

		/// <inheritdoc />
		public IInstance CreateInstance(Models.Instance metadata, IInteropRegistrar interopRegistrar)
		{
			//Create the ioManager for the instance
			var instanceIoManager = new ResolvingIOManager(ioManager, metadata.Path);

			//various other ioManagers
			var repoIoManager = new ResolvingIOManager(instanceIoManager, "Repository");
			var byondIOManager = new ResolvingIOManager(instanceIoManager, "Byond");
			var gameIoManager = new ResolvingIOManager(instanceIoManager, "Game");
			var configurationIoManager = new ResolvingIOManager(instanceIoManager, "Configuration");

			var dmbFactory = new DmbFactory(databaseContextFactory, gameIoManager, metadata);
			var commandFactory = new CommandFactory(application);
			var chatFactory = new ChatFactory(instanceIoManager, loggerFactory, commandFactory);

			var repoManager = new RepositoryManager(metadata.RepositorySettings, repoIoManager);

			IByond byond = null;
			var configuration = new Configuration(configurationIoManager, synchronousIOManager, loggerFactory.CreateLogger<Configuration>());
			
			var chat = chatFactory.CreateChat();
			var sessionControllerFactory = new SessionControllerFactory(executor, byond, byondTopicSender, interopRegistrar, cryptographySuite, application, gameIoManager, chat, loggerFactory, metadata);
			var reattachInfoHandler = new ReattachInfoHandler(databaseContextFactory, dmbFactory, metadata);
			var watchdogFactory = new WatchdogFactory(chat, sessionControllerFactory, serverUpdater, loggerFactory, reattachInfoHandler, databaseContextFactory, byondTopicSender, metadata);
			var watchdog = watchdogFactory.CreateWatchdog(dmbFactory, metadata.DreamDaemonSettings);

			var dreamMaker = new DreamMaker(byond, ioManager, configuration, sessionControllerFactory, dmbFactory, application, watchdog, loggerFactory.CreateLogger<DreamMaker>());

			throw new NotImplementedException();
			//return new Instance(metadata, repoManager, byond, dreamMaker, watchdog, chat, configuration, dmbFactory, databaseContextFactory, dmbFactory);
		}
	}
}
