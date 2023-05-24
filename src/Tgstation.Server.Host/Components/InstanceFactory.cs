using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Tgstation.Server.Host.Components.Byond;
using Tgstation.Server.Host.Components.Chat;
using Tgstation.Server.Host.Components.Chat.Commands;
using Tgstation.Server.Host.Components.Deployment;
using Tgstation.Server.Host.Components.Deployment.Remote;
using Tgstation.Server.Host.Components.Events;
using Tgstation.Server.Host.Components.Interop.Bridge;
using Tgstation.Server.Host.Components.Repository;
using Tgstation.Server.Host.Components.Session;
using Tgstation.Server.Host.Components.Watchdog;
using Tgstation.Server.Host.Configuration;
using Tgstation.Server.Host.Core;
using Tgstation.Server.Host.Database;
using Tgstation.Server.Host.IO;
using Tgstation.Server.Host.Jobs;
using Tgstation.Server.Host.Security;
using Tgstation.Server.Host.System;
using Tgstation.Server.Host.Transfer;
using Tgstation.Server.Host.Utils;

namespace Tgstation.Server.Host.Components
{
	/// <inheritdoc />
	sealed class InstanceFactory : IInstanceFactory
	{
		/// <summary>
		/// The <see cref="IIOManager"/> for the <see cref="InstanceFactory"/>.
		/// </summary>
		readonly IIOManager ioManager;

		/// <summary>
		/// The <see cref="IDatabaseContextFactory"/> for the <see cref="InstanceFactory"/>.
		/// </summary>
		readonly IDatabaseContextFactory databaseContextFactory;

		/// <summary>
		/// The <see cref="IAssemblyInformationProvider"/> for the <see cref="InstanceFactory"/>.
		/// </summary>
		readonly IAssemblyInformationProvider assemblyInformationProvider;

		/// <summary>
		/// The <see cref="ILoggerFactory"/> for the <see cref="InstanceFactory"/>.
		/// </summary>
		readonly ILoggerFactory loggerFactory;

		/// <summary>
		/// The <see cref="ITopicClientFactory"/> for the <see cref="InstanceFactory"/>.
		/// </summary>
		readonly ITopicClientFactory topicClientFactory;

		/// <summary>
		/// The <see cref="ICryptographySuite"/> for the <see cref="InstanceFactory"/>.
		/// </summary>
		readonly ICryptographySuite cryptographySuite;

		/// <summary>
		/// The <see cref="ISynchronousIOManager"/> for the <see cref="InstanceFactory"/>.
		/// </summary>
		readonly ISynchronousIOManager synchronousIOManager;

		/// <summary>
		/// The <see cref="ISymlinkFactory"/> for the <see cref="InstanceFactory"/>.
		/// </summary>
		readonly ISymlinkFactory symlinkFactory;

		/// <summary>
		/// The <see cref="IByondInstaller"/> for the <see cref="InstanceFactory"/>.
		/// </summary>
		readonly IByondInstaller byondInstaller;

		/// <summary>
		/// The <see cref="IChatManagerFactory"/> for the <see cref="InstanceFactory"/>.
		/// </summary>
		readonly IChatManagerFactory chatFactory;

		/// <summary>
		/// The <see cref="IProcessExecutor"/> for the <see cref="InstanceFactory"/>.
		/// </summary>
		readonly IProcessExecutor processExecutor;

		/// <summary>
		/// The <see cref="IPostWriteHandler"/> for the <see cref="InstanceFactory"/>.
		/// </summary>
		readonly IPostWriteHandler postWriteHandler;

		/// <summary>
		/// The <see cref="IWatchdogFactory"/> for the <see cref="InstanceFactory"/>.
		/// </summary>
		readonly IWatchdogFactory watchdogFactory;

		/// <summary>
		/// The <see cref="IJobManager"/> for the <see cref="InstanceFactory"/>.
		/// </summary>
		readonly IJobManager jobManager;

		/// <summary>
		/// The <see cref="INetworkPromptReaper"/> for the <see cref="InstanceFactory"/>.
		/// </summary>
		readonly INetworkPromptReaper networkPromptReaper;

		/// <summary>
		/// The <see cref="IPlatformIdentifier"/> for the <see cref="InstanceFactory"/>.
		/// </summary>
		readonly IPlatformIdentifier platformIdentifier;

		/// <summary>
		/// The <see cref="ILibGit2RepositoryFactory"/> for the <see cref="InstanceFactory"/>.
		/// </summary>
		readonly ILibGit2RepositoryFactory repositoryFactory;

		/// <summary>
		/// The <see cref="ILibGit2Commands"/> for the <see cref="InstanceFactory"/>.
		/// </summary>
		readonly ILibGit2Commands repositoryCommands;

		/// <summary>
		/// The <see cref="IServerPortProvider"/> for the <see cref="InstanceFactory"/>.
		/// </summary>
		readonly IServerPortProvider serverPortProvider;

		/// <summary>
		/// The <see cref="IFileTransferTicketProvider"/> for the <see cref="InstanceFactory"/>.
		/// </summary>
		readonly IFileTransferTicketProvider fileTransferService;

		/// <summary>
		/// The <see cref="IGitRemoteFeaturesFactory"/> for the <see cref="InstanceFactory"/>.
		/// </summary>
		readonly IGitRemoteFeaturesFactory gitRemoteFeaturesFactory;

		/// <summary>
		/// The <see cref="IRemoteDeploymentManagerFactory"/> for the <see cref="InstanceFactory"/>.
		/// </summary>
		readonly IRemoteDeploymentManagerFactory remoteDeploymentManagerFactory;

		/// <summary>
		/// The <see cref="IAsyncDelayer"/> for the <see cref="InstanceFactory"/>.
		/// </summary>
		readonly IAsyncDelayer asyncDelayer;

		/// <summary>
		/// The <see cref="GeneralConfiguration"/> for the <see cref="InstanceFactory"/>.
		/// </summary>
		readonly GeneralConfiguration generalConfiguration;

		/// <summary>
		/// The <see cref="SessionConfiguration"/> for the <see cref="InstanceFactory"/>.
		/// </summary>
		readonly SessionConfiguration sessionConfiguration;

		/// <summary>
		/// Create the <see cref="IIOManager"/> pointing to the "Game" directory of a given <paramref name="instanceIOManager"/>.
		/// </summary>
		/// <param name="instanceIOManager">The instance's <see cref="IIOManager"/>.</param>
		/// <returns>The <see cref="IIOManager"/> for the instance's "Game" directory.</returns>
		static IIOManager CreateGameIOManager(IIOManager instanceIOManager) => new ResolvingIOManager(instanceIOManager, "Game");

#pragma warning disable CA1502 // TODO: Decomplexify
		/// <summary>
		/// Initializes a new instance of the <see cref="InstanceFactory"/> class.
		/// </summary>
		/// <param name="ioManager">The value of <see cref="ioManager"/>.</param>
		/// <param name="databaseContextFactory">The value of <see cref="databaseContextFactory"/>.</param>
		/// <param name="assemblyInformationProvider">The value of <see cref="assemblyInformationProvider"/>.</param>
		/// <param name="loggerFactory">The value of <see cref="loggerFactory"/>.</param>
		/// <param name="topicClientFactory">The value of <see cref="topicClientFactory"/>.</param>
		/// <param name="cryptographySuite">The value of <see cref="cryptographySuite"/>.</param>
		/// <param name="synchronousIOManager">The value of <see cref="synchronousIOManager"/>.</param>
		/// <param name="symlinkFactory">The value of <see cref="symlinkFactory"/>.</param>
		/// <param name="byondInstaller">The value of <see cref="byondInstaller"/>.</param>
		/// <param name="chatFactory">The value of <see cref="chatFactory"/>.</param>
		/// <param name="processExecutor">The value of <see cref="processExecutor"/>.</param>
		/// <param name="postWriteHandler">The value of <see cref="postWriteHandler"/>.</param>
		/// <param name="watchdogFactory">The value of <see cref="watchdogFactory"/>.</param>
		/// <param name="jobManager">The value of <see cref="jobManager"/>.</param>
		/// <param name="networkPromptReaper">The value of <see cref="networkPromptReaper"/>.</param>
		/// <param name="platformIdentifier">The value of <see cref="platformIdentifier"/>.</param>
		/// <param name="repositoryFactory">The value of <see cref="repositoryFactory"/>.</param>
		/// <param name="repositoryCommands">The value of <see cref="repositoryCommands"/>.</param>
		/// <param name="serverPortProvider">The value of <see cref="serverPortProvider"/>.</param>
		/// <param name="fileTransferService">The value of <see cref="fileTransferService"/>.</param>
		/// <param name="gitRemoteFeaturesFactory">The value of <see cref="gitRemoteFeaturesFactory"/>.</param>
		/// <param name="remoteDeploymentManagerFactory">The value of <see cref="remoteDeploymentManagerFactory"/>.</param>
		/// <param name="asyncDelayer">The value of <see cref="asyncDelayer"/>.</param>
		/// <param name="generalConfigurationOptions">The <see cref="IOptions{TOptions}"/> containing the value of <see cref="generalConfiguration"/>.</param>
		/// <param name="sessionConfigurationOptions">The <see cref="IOptions{TOptions}"/> containing the value of <see cref="sessionConfiguration"/>.</param>
		public InstanceFactory(
			IIOManager ioManager,
			IDatabaseContextFactory databaseContextFactory,
			IAssemblyInformationProvider assemblyInformationProvider,
			ILoggerFactory loggerFactory,
			ITopicClientFactory topicClientFactory,
			ICryptographySuite cryptographySuite,
			ISynchronousIOManager synchronousIOManager,
			ISymlinkFactory symlinkFactory,
			IByondInstaller byondInstaller,
			IChatManagerFactory chatFactory,
			IProcessExecutor processExecutor,
			IPostWriteHandler postWriteHandler,
			IWatchdogFactory watchdogFactory,
			IJobManager jobManager,
			INetworkPromptReaper networkPromptReaper,
			IPlatformIdentifier platformIdentifier,
			ILibGit2RepositoryFactory repositoryFactory,
			ILibGit2Commands repositoryCommands,
			IServerPortProvider serverPortProvider,
			IFileTransferTicketProvider fileTransferService,
			IGitRemoteFeaturesFactory gitRemoteFeaturesFactory,
			IRemoteDeploymentManagerFactory remoteDeploymentManagerFactory,
			IAsyncDelayer asyncDelayer,
			IOptions<GeneralConfiguration> generalConfigurationOptions,
			IOptions<SessionConfiguration> sessionConfigurationOptions)
		{
			this.ioManager = ioManager ?? throw new ArgumentNullException(nameof(ioManager));
			this.databaseContextFactory = databaseContextFactory ?? throw new ArgumentNullException(nameof(databaseContextFactory));
			this.assemblyInformationProvider = assemblyInformationProvider ?? throw new ArgumentNullException(nameof(assemblyInformationProvider));
			this.loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
			this.topicClientFactory = topicClientFactory ?? throw new ArgumentNullException(nameof(topicClientFactory));
			this.cryptographySuite = cryptographySuite ?? throw new ArgumentNullException(nameof(cryptographySuite));
			this.synchronousIOManager = synchronousIOManager ?? throw new ArgumentNullException(nameof(synchronousIOManager));
			this.symlinkFactory = symlinkFactory ?? throw new ArgumentNullException(nameof(symlinkFactory));
			this.byondInstaller = byondInstaller ?? throw new ArgumentNullException(nameof(byondInstaller));
			this.chatFactory = chatFactory ?? throw new ArgumentNullException(nameof(chatFactory));
			this.processExecutor = processExecutor ?? throw new ArgumentNullException(nameof(processExecutor));
			this.postWriteHandler = postWriteHandler ?? throw new ArgumentNullException(nameof(postWriteHandler));
			this.watchdogFactory = watchdogFactory ?? throw new ArgumentNullException(nameof(watchdogFactory));
			this.jobManager = jobManager ?? throw new ArgumentNullException(nameof(jobManager));
			this.networkPromptReaper = networkPromptReaper ?? throw new ArgumentNullException(nameof(networkPromptReaper));
			this.platformIdentifier = platformIdentifier ?? throw new ArgumentNullException(nameof(platformIdentifier));
			this.repositoryFactory = repositoryFactory ?? throw new ArgumentNullException(nameof(repositoryFactory));
			this.repositoryCommands = repositoryCommands ?? throw new ArgumentNullException(nameof(repositoryCommands));
			this.serverPortProvider = serverPortProvider ?? throw new ArgumentNullException(nameof(serverPortProvider));
			this.fileTransferService = fileTransferService ?? throw new ArgumentNullException(nameof(fileTransferService));
			this.gitRemoteFeaturesFactory = gitRemoteFeaturesFactory ?? throw new ArgumentNullException(nameof(gitRemoteFeaturesFactory));
			this.remoteDeploymentManagerFactory = remoteDeploymentManagerFactory ?? throw new ArgumentNullException(nameof(remoteDeploymentManagerFactory));
			this.asyncDelayer = asyncDelayer ?? throw new ArgumentNullException(nameof(asyncDelayer));
			generalConfiguration = generalConfigurationOptions?.Value ?? throw new ArgumentNullException(nameof(generalConfigurationOptions));
			sessionConfiguration = sessionConfigurationOptions?.Value ?? throw new ArgumentNullException(nameof(sessionConfigurationOptions));
		}
#pragma warning restore CA1502

		/// <inheritdoc />
		public IIOManager CreateGameIOManager(Models.Instance metadata)
		{
			if (metadata == null)
				throw new ArgumentNullException(nameof(metadata));

			var instanceIoManager = CreateInstanceIOManager(metadata);
			return CreateGameIOManager(instanceIoManager);
		}

		/// <inheritdoc />
#pragma warning disable CA1506 // TODO: Decomplexify
		public async Task<IInstance> CreateInstance(IBridgeRegistrar bridgeRegistrar, Models.Instance metadata)
		{
			if (bridgeRegistrar == null)
				throw new ArgumentNullException(nameof(bridgeRegistrar));
			if (metadata == null)
				throw new ArgumentNullException(nameof(metadata));

			// Create the ioManager for the instance
			var instanceIoManager = CreateInstanceIOManager(metadata);

			// various other ioManagers
			var repoIoManager = new ResolvingIOManager(instanceIoManager, "Repository");
			var byondIOManager = new ResolvingIOManager(instanceIoManager, "Byond");
			var gameIoManager = CreateGameIOManager(instanceIoManager);
			var diagnosticsIOManager = new ResolvingIOManager(instanceIoManager, "Diagnostics");
			var configurationIoManager = new ResolvingIOManager(instanceIoManager, "Configuration");

			var configuration = new StaticFiles.Configuration(
				configurationIoManager,
				synchronousIOManager,
				symlinkFactory,
				processExecutor,
				postWriteHandler,
				platformIdentifier,
				fileTransferService,
				loggerFactory.CreateLogger<StaticFiles.Configuration>(),
				generalConfiguration);
			var eventConsumer = new EventConsumer(configuration);
			var repoManager = new RepositoryManager(
				repositoryFactory,
				repositoryCommands,
				repoIoManager,
				eventConsumer,
				postWriteHandler,
				gitRemoteFeaturesFactory,
				loggerFactory.CreateLogger<Repository.Repository>(),
				loggerFactory.CreateLogger<RepositoryManager>(),
				generalConfiguration);
			try
			{
				var byond = new ByondManager(byondIOManager, byondInstaller, eventConsumer, loggerFactory.CreateLogger<ByondManager>());

				var commandFactory = new CommandFactory(assemblyInformationProvider, byond, repoManager, databaseContextFactory, metadata);

				var chatManager = chatFactory.CreateChatManager(commandFactory, metadata.ChatSettings);
				try
				{
					var sessionControllerFactory = new SessionControllerFactory(
						processExecutor,
						byond,
						topicClientFactory,
						cryptographySuite,
						assemblyInformationProvider,
						gameIoManager,
						diagnosticsIOManager,
						chatManager,
						networkPromptReaper,
						platformIdentifier,
						bridgeRegistrar,
						serverPortProvider,
						eventConsumer,
						asyncDelayer,
						loggerFactory,
						loggerFactory.CreateLogger<SessionControllerFactory>(),
						sessionConfiguration,
						metadata);

					var dmbFactory = new DmbFactory(
						databaseContextFactory,
						gameIoManager,
						remoteDeploymentManagerFactory,
						eventConsumer,
						loggerFactory.CreateLogger<DmbFactory>(),
						metadata);
					try
					{
						var reattachInfoHandler = new SessionPersistor(
							databaseContextFactory,
							dmbFactory,
							processExecutor,
							loggerFactory.CreateLogger<SessionPersistor>(),
							metadata);
						var watchdog = watchdogFactory.CreateWatchdog(
							chatManager,
							dmbFactory,
							reattachInfoHandler,
							sessionControllerFactory,
							gameIoManager,
							diagnosticsIOManager,
							configuration, // watchdog doesn't need itself as an event consumer
							remoteDeploymentManagerFactory,
							metadata,
							metadata.DreamDaemonSettings);
						try
						{
							eventConsumer.SetWatchdog(watchdog);
							commandFactory.SetWatchdog(watchdog);

							Instance instance = null;
							var dreamMaker = new DreamMaker(
								byond,
								gameIoManager,
								configuration,
								sessionControllerFactory,
								eventConsumer,
								chatManager,
								processExecutor,
								dmbFactory,
								repoManager,
								remoteDeploymentManagerFactory,
								asyncDelayer,
								loggerFactory.CreateLogger<DreamMaker>(),
								sessionConfiguration,
								metadata);

							instance = new Instance(
								metadata,
								repoManager,
								byond,
								dreamMaker,
								watchdog,
								chatManager,
								configuration,
								dmbFactory,
								jobManager,
								eventConsumer,
								remoteDeploymentManagerFactory,
								asyncDelayer,
								loggerFactory.CreateLogger<Instance>());

							return instance;
						}
						catch
						{
							await watchdog.DisposeAsync();
							throw;
						}
					}
					catch
					{
						dmbFactory.Dispose();
						throw;
					}
				}
				catch
				{
					await chatManager.DisposeAsync();
					throw;
				}
			}
			catch
			{
				repoManager.Dispose();
				throw;
			}
		}
#pragma warning restore CA1506

		/// <inheritdoc />
		public Task StartAsync(CancellationToken cancellationToken)
		{
			CheckSystemCompatibility();
			return byondInstaller.CleanCache(cancellationToken);
		}

		/// <inheritdoc />
		public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

		/// <summary>
		/// Test that the <see cref="repositoryFactory"/> is functional.
		/// </summary>
		void CheckSystemCompatibility() => repositoryFactory.CreateInMemory();

		/// <summary>
		/// Create the <see cref="IIOManager"/> for a given set of instance <paramref name="metadata"/>.
		/// </summary>
		/// <param name="metadata">The <see cref="Models.Instance"/>.</param>
		/// <returns>The <see cref="IIOManager"/> for the <paramref name="metadata"/>.</returns>
		IIOManager CreateInstanceIOManager(Models.Instance metadata) => new ResolvingIOManager(ioManager, metadata.Path);
	}
}
