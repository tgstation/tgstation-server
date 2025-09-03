using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Prometheus;

using Tgstation.Server.Host.Components.Chat;
using Tgstation.Server.Host.Components.Chat.Commands;
using Tgstation.Server.Host.Components.Deployment;
using Tgstation.Server.Host.Components.Deployment.Remote;
using Tgstation.Server.Host.Components.Engine;
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
		/// The <see cref="IFilesystemLinkFactory"/> for the <see cref="InstanceFactory"/>.
		/// </summary>
		readonly IFilesystemLinkFactory linkFactory;

		/// <summary>
		/// The <see cref="IEngineInstaller"/> for the <see cref="InstanceFactory"/>.
		/// </summary>
		readonly IEngineInstaller engineInstaller;

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
		/// The <see cref="IRepositoryManagerFactory"/> for the <see cref=" InstanceFactory"/>.
		/// </summary>
		readonly IRepositoryManagerFactory repositoryManagerFactory;

		/// <summary>
		/// The <see cref="IServerPortProvider"/> for the <see cref="InstanceFactory"/>.
		/// </summary>
		readonly IServerPortProvider serverPortProvider;

		/// <summary>
		/// The <see cref="IFileTransferTicketProvider"/> for the <see cref="InstanceFactory"/>.
		/// </summary>
		readonly IFileTransferTicketProvider fileTransferService;

		/// <summary>
		/// The <see cref="IRemoteDeploymentManagerFactory"/> for the <see cref="InstanceFactory"/>.
		/// </summary>
		readonly IRemoteDeploymentManagerFactory remoteDeploymentManagerFactory;

		/// <summary>
		/// The <see cref="IAsyncDelayer"/> for the <see cref="InstanceFactory"/>.
		/// </summary>
		readonly IAsyncDelayer asyncDelayer;

		/// <summary>
		/// The <see cref="IDotnetDumpService"/> for the <see cref="InstanceFactory"/>.
		/// </summary>
		readonly IDotnetDumpService dotnetDumpService;

		/// <summary>
		/// The <see cref="IMetricFactory"/> for the <see cref="InstanceFactory"/>.
		/// </summary>
		readonly IMetricFactory metricFactory;

		/// <summary>
		/// The <see cref="IOptionsMonitor{TOptions}"/> of <see cref="GeneralConfiguration"/> for the <see cref="InstanceFactory"/>.
		/// </summary>
		readonly IOptionsMonitor<GeneralConfiguration> generalConfigurationOptions;

		/// <summary>
		/// The <see cref="IOptionsMonitor{TOptions}"/> of <see cref="SessionConfiguration"/> for the <see cref="InstanceFactory"/>.
		/// </summary>
		readonly IOptionsMonitor<SessionConfiguration> sessionConfigurationOptions;

		/// <summary>
		/// Create the <see cref="IIOManager"/> pointing to the "Game" directory of a given <paramref name="instanceIOManager"/>.
		/// </summary>
		/// <param name="instanceIOManager">The instance's <see cref="IIOManager"/>.</param>
		/// <returns>The <see cref="IIOManager"/> for the instance's "Game" directory.</returns>
		static IIOManager CreateGameIOManager(IIOManager instanceIOManager) => instanceIOManager.CreateResolverForSubdirectory("Game");

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
		/// <param name="linkFactory">The value of <see cref="linkFactory"/>.</param>
		/// <param name="engineInstaller">The value of <see cref="engineInstaller"/>.</param>
		/// <param name="chatFactory">The value of <see cref="chatFactory"/>.</param>
		/// <param name="processExecutor">The value of <see cref="processExecutor"/>.</param>
		/// <param name="postWriteHandler">The value of <see cref="postWriteHandler"/>.</param>
		/// <param name="watchdogFactory">The value of <see cref="watchdogFactory"/>.</param>
		/// <param name="jobManager">The value of <see cref="jobManager"/>.</param>
		/// <param name="networkPromptReaper">The value of <see cref="networkPromptReaper"/>.</param>
		/// <param name="platformIdentifier">The value of <see cref="platformIdentifier"/>.</param>
		/// <param name="repositoryManagerFactory">The value of <see cref="repositoryManagerFactory"/>.</param>
		/// <param name="serverPortProvider">The value of <see cref="serverPortProvider"/>.</param>
		/// <param name="fileTransferService">The value of <see cref="fileTransferService"/>.</param>
		/// <param name="remoteDeploymentManagerFactory">The value of <see cref="remoteDeploymentManagerFactory"/>.</param>
		/// <param name="asyncDelayer">The value of <see cref="asyncDelayer"/>.</param>
		/// <param name="dotnetDumpService">The value of <see cref="dotnetDumpService"/>.</param>
		/// <param name="metricFactory">The value of <see cref="metricFactory"/>.</param>
		/// <param name="generalConfigurationOptions">The value of <see cref="generalConfigurationOptions"/>.</param>
		/// <param name="sessionConfigurationOptions">The value of <see cref="sessionConfigurationOptions"/>.</param>
		public InstanceFactory(
			IIOManager ioManager,
			IDatabaseContextFactory databaseContextFactory,
			IAssemblyInformationProvider assemblyInformationProvider,
			ILoggerFactory loggerFactory,
			ITopicClientFactory topicClientFactory,
			ICryptographySuite cryptographySuite,
			ISynchronousIOManager synchronousIOManager,
			IFilesystemLinkFactory linkFactory,
			IEngineInstaller engineInstaller,
			IChatManagerFactory chatFactory,
			IProcessExecutor processExecutor,
			IPostWriteHandler postWriteHandler,
			IWatchdogFactory watchdogFactory,
			IJobManager jobManager,
			INetworkPromptReaper networkPromptReaper,
			IPlatformIdentifier platformIdentifier,
			IRepositoryManagerFactory repositoryManagerFactory,
			IServerPortProvider serverPortProvider,
			IFileTransferTicketProvider fileTransferService,
			IRemoteDeploymentManagerFactory remoteDeploymentManagerFactory,
			IAsyncDelayer asyncDelayer,
			IDotnetDumpService dotnetDumpService,
			IMetricFactory metricFactory,
			IOptionsMonitor<GeneralConfiguration> generalConfigurationOptions,
			IOptionsMonitor<SessionConfiguration> sessionConfigurationOptions)
		{
			this.ioManager = ioManager ?? throw new ArgumentNullException(nameof(ioManager));
			this.databaseContextFactory = databaseContextFactory ?? throw new ArgumentNullException(nameof(databaseContextFactory));
			this.assemblyInformationProvider = assemblyInformationProvider ?? throw new ArgumentNullException(nameof(assemblyInformationProvider));
			this.loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
			this.topicClientFactory = topicClientFactory ?? throw new ArgumentNullException(nameof(topicClientFactory));
			this.cryptographySuite = cryptographySuite ?? throw new ArgumentNullException(nameof(cryptographySuite));
			this.synchronousIOManager = synchronousIOManager ?? throw new ArgumentNullException(nameof(synchronousIOManager));
			this.linkFactory = linkFactory ?? throw new ArgumentNullException(nameof(linkFactory));
			this.engineInstaller = engineInstaller ?? throw new ArgumentNullException(nameof(engineInstaller));
			this.chatFactory = chatFactory ?? throw new ArgumentNullException(nameof(chatFactory));
			this.processExecutor = processExecutor ?? throw new ArgumentNullException(nameof(processExecutor));
			this.postWriteHandler = postWriteHandler ?? throw new ArgumentNullException(nameof(postWriteHandler));
			this.watchdogFactory = watchdogFactory ?? throw new ArgumentNullException(nameof(watchdogFactory));
			this.jobManager = jobManager ?? throw new ArgumentNullException(nameof(jobManager));
			this.networkPromptReaper = networkPromptReaper ?? throw new ArgumentNullException(nameof(networkPromptReaper));
			this.platformIdentifier = platformIdentifier ?? throw new ArgumentNullException(nameof(platformIdentifier));
			this.repositoryManagerFactory = repositoryManagerFactory ?? throw new ArgumentNullException(nameof(repositoryManagerFactory));
			this.serverPortProvider = serverPortProvider ?? throw new ArgumentNullException(nameof(serverPortProvider));
			this.fileTransferService = fileTransferService ?? throw new ArgumentNullException(nameof(fileTransferService));
			this.remoteDeploymentManagerFactory = remoteDeploymentManagerFactory ?? throw new ArgumentNullException(nameof(remoteDeploymentManagerFactory));
			this.asyncDelayer = asyncDelayer ?? throw new ArgumentNullException(nameof(asyncDelayer));
			this.dotnetDumpService = dotnetDumpService ?? throw new ArgumentNullException(nameof(dotnetDumpService));
			this.metricFactory = metricFactory ?? throw new ArgumentNullException(nameof(metricFactory));
			this.generalConfigurationOptions = generalConfigurationOptions ?? throw new ArgumentNullException(nameof(generalConfigurationOptions));
			this.sessionConfigurationOptions = sessionConfigurationOptions ?? throw new ArgumentNullException(nameof(sessionConfigurationOptions));
		}
#pragma warning restore CA1502

		/// <inheritdoc />
		public IIOManager CreateGameIOManager(Models.Instance metadata)
		{
			ArgumentNullException.ThrowIfNull(metadata);

			var instanceIoManager = CreateInstanceIOManager(metadata);
			return CreateGameIOManager(instanceIoManager);
		}

		/// <inheritdoc />
#pragma warning disable CA1506 // TODO: Decomplexify
		public async ValueTask<IInstance> CreateInstance(IBridgeRegistrar bridgeRegistrar, Models.Instance metadata)
		{
			ArgumentNullException.ThrowIfNull(bridgeRegistrar);
			ArgumentNullException.ThrowIfNull(metadata);

			// Create the ioManager for the instance
			var instanceIoManager = CreateInstanceIOManager(metadata);

			// various other ioManagers
			var repoIoManager = instanceIoManager.CreateResolverForSubdirectory("Repository");
			var byondIOManager = instanceIoManager.CreateResolverForSubdirectory("Byond");
			var gameIoManager = CreateGameIOManager(instanceIoManager);
			var diagnosticsIOManager = instanceIoManager.CreateResolverForSubdirectory("Diagnostics");
			var configurationIoManager = instanceIoManager.CreateResolverForSubdirectory("Configuration");

			var metricFactory = this.metricFactory.WithLabels(
				new Dictionary<string, string>
				{
					{ "instance_name", metadata.Name! },
					{ "instance_id", metadata.Id!.Value.ToString(CultureInfo.InvariantCulture) },
				});

			var configuration = new StaticFiles.Configuration(
				configurationIoManager,
				synchronousIOManager,
				linkFactory,
				processExecutor,
				postWriteHandler,
				platformIdentifier,
				fileTransferService,
				generalConfigurationOptions,
				sessionConfigurationOptions,
				loggerFactory.CreateLogger<StaticFiles.Configuration>(),
				metadata);
			var eventConsumer = new EventConsumer(configuration);
			var repoManager = repositoryManagerFactory.CreateRepositoryManager(repoIoManager, eventConsumer);
			try
			{
				var dmbFactory = new DmbFactory(
					databaseContextFactory,
					gameIoManager,
					remoteDeploymentManagerFactory,
					eventConsumer,
					asyncDelayer,
					loggerFactory.CreateLogger<DmbFactory>(),
					metadata);
				try
				{
					var engineManager = new EngineManager(
						byondIOManager,
						engineInstaller,
						eventConsumer,
						dmbFactory,
						loggerFactory.CreateLogger<EngineManager>());
					try
					{
						var commandFactory = new CommandFactory(assemblyInformationProvider, engineManager, repoManager, databaseContextFactory, dmbFactory, metadata);

						var chatManager = chatFactory.CreateChatManager(commandFactory, metadata.ChatSettings);
						try
						{
							var reattachInfoHandler = new SessionPersistor(
								databaseContextFactory,
								dmbFactory,
								processExecutor,
								loggerFactory.CreateLogger<SessionPersistor>(),
								metadata);

							var sessionControllerFactory = new SessionControllerFactory(
								processExecutor,
								engineManager,
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
								dotnetDumpService,
								metricFactory,
								loggerFactory,
								sessionConfigurationOptions,
								loggerFactory.CreateLogger<SessionControllerFactory>(),
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
								metricFactory,
								metadata,
								metadata.DreamDaemonSettings!);
							try
							{
								eventConsumer.SetWatchdog(watchdog);
								commandFactory.SetWatchdog(watchdog);

								Instance? instance = null;
								var dreamMaker = new DreamMaker(
									engineManager,
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
									metricFactory,
									sessionConfigurationOptions,
									loggerFactory.CreateLogger<DreamMaker>(),
									metadata);

								instance = new Instance(
									metadata,
									repoManager,
									engineManager,
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
							await chatManager.DisposeAsync();
							throw;
						}
					}
					catch
					{
						engineManager.Dispose();
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
				repoManager.Dispose();
				throw;
			}
		}
#pragma warning restore CA1506

		/// <inheritdoc />
		public Task StartAsync(CancellationToken cancellationToken)
			=> Task.WhenAll(
				repositoryManagerFactory.StartAsync(cancellationToken),
				engineInstaller.CleanCache(cancellationToken));

		/// <inheritdoc />
		public Task StopAsync(CancellationToken cancellationToken) => repositoryManagerFactory.StopAsync(cancellationToken);

		/// <summary>
		/// Create the <see cref="IIOManager"/> for a given set of instance <paramref name="metadata"/>.
		/// </summary>
		/// <param name="metadata">The <see cref="Models.Instance"/>.</param>
		/// <returns>The <see cref="IIOManager"/> for the <paramref name="metadata"/>.</returns>
		IIOManager CreateInstanceIOManager(Models.Instance metadata) => ioManager.CreateResolverForSubdirectory(metadata.Path!);
	}
}
