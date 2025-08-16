using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Prometheus;

using Tgstation.Server.Api.Models;
using Tgstation.Server.Common;
using Tgstation.Server.Common.Extensions;
using Tgstation.Server.Host.Components.Interop;
using Tgstation.Server.Host.Components.Interop.Bridge;
using Tgstation.Server.Host.Configuration;
using Tgstation.Server.Host.Controllers;
using Tgstation.Server.Host.Core;
using Tgstation.Server.Host.Database;
using Tgstation.Server.Host.Extensions;
using Tgstation.Server.Host.IO;
using Tgstation.Server.Host.Jobs;
using Tgstation.Server.Host.Models;
using Tgstation.Server.Host.Security;
using Tgstation.Server.Host.Swarm;
using Tgstation.Server.Host.System;
using Tgstation.Server.Host.Utils;

namespace Tgstation.Server.Host.Components
{
	/// <inheritdoc cref="IInstanceManager" />
	sealed class InstanceManager :
		IInstanceManager,
		IInstanceCoreProvider,
		IHostedService,
		IBridgeRegistrar,
		IAsyncDisposable
	{
		/// <inheritdoc />
		public Task Ready => readyTcs.Task;

		/// <summary>
		/// The <see cref="IInstanceFactory"/> for the <see cref="InstanceManager"/>.
		/// </summary>
		readonly IInstanceFactory instanceFactory;

		/// <summary>
		/// The <see cref="IIOManager"/> for the <see cref="InstanceManager"/>.
		/// </summary>
		readonly IIOManager ioManager;

		/// <summary>
		/// The <see cref="IDatabaseContextFactory"/> for the <see cref="InstanceManager"/>.
		/// </summary>
		readonly IDatabaseContextFactory databaseContextFactory;

		/// <summary>
		/// The <see cref="IAssemblyInformationProvider"/> for the <see cref="InstanceManager"/>.
		/// </summary>
		readonly IAssemblyInformationProvider assemblyInformationProvider;

		/// <summary>
		/// The <see cref="IJobService"/> for the <see cref="InstanceManager"/>.
		/// </summary>
		readonly IJobService jobService;

		/// <summary>
		/// The <see cref="IServerControl"/> for the <see cref="InstanceManager"/>.
		/// </summary>
		readonly IServerControl serverControl;

		/// <summary>
		/// The <see cref="ISystemIdentityFactory"/> for the <see cref="InstanceManager"/>.
		/// </summary>
		readonly ISystemIdentityFactory systemIdentityFactory;

		/// <summary>
		/// The <see cref="IAsyncDelayer"/> for the <see cref="InstanceManager"/>.
		/// </summary>
		readonly IAsyncDelayer asyncDelayer;

		/// <summary>
		/// The <see cref="IServerPortProvider"/> for the <see cref="InstanceManager"/>.
		/// </summary>
		readonly IServerPortProvider serverPortProvider;

		/// <summary>
		/// The <see cref="ISwarmServiceController"/> for the <see cref="InstanceManager"/>.
		/// </summary>
		readonly ISwarmServiceController swarmServiceController;

		/// <summary>
		/// The <see cref="IConsole"/> for the <see cref="InstanceManager"/>.
		/// </summary>
		readonly IConsole console;

		/// <summary>
		/// The <see cref="IPlatformIdentifier"/> for the <see cref="InstanceManager"/>.
		/// </summary>
		readonly IPlatformIdentifier platformIdentifier;

		/// <summary>
		/// The <see cref="ILogger"/> for the <see cref="InstanceManager"/>.
		/// </summary>
		readonly ILogger<InstanceManager> logger;

		/// <summary>
		/// Map of instance <see cref="EntityId.Id"/>s to the respective <see cref="ReferenceCountingContainer{TWrapped, TReference}"/> for <see cref="IInstance"/>s. Also used as a <see langword="lock"/> <see cref="object"/>.
		/// </summary>
		readonly Dictionary<long, ReferenceCountingContainer<IInstance, InstanceWrapper>> instances;

		/// <summary>
		/// Map of <see cref="DMApiParameters.AccessIdentifier"/>s to their respective <see cref="IBridgeHandler"/>s.
		/// </summary>
		readonly Dictionary<string, IBridgeHandler> bridgeHandlers;

		/// <summary>
		/// <see cref="SemaphoreSlim"/> used to guard calls to <see cref="OnlineInstance(Models.Instance, CancellationToken)"/> and <see cref="OfflineInstance(Models.Instance, User, CancellationToken)"/>.
		/// </summary>
		readonly SemaphoreSlim instanceStateChangeSemaphore;

		/// <summary>
		/// The <see cref="GeneralConfiguration"/> for the <see cref="InstanceManager"/>.
		/// </summary>
		readonly GeneralConfiguration generalConfiguration;

		/// <summary>
		/// The <see cref="SwarmConfiguration"/> for the <see cref="InstanceManager"/>.
		/// </summary>
		readonly SwarmConfiguration swarmConfiguration;

		/// <summary>
		/// The <see cref="InternalConfiguration"/> for the <see cref="InstanceManager"/>.
		/// </summary>
		readonly InternalConfiguration internalConfiguration;

		/// <summary>
		/// The <see cref="TaskCompletionSource"/> for <see cref="Ready"/>.
		/// </summary>
		readonly TaskCompletionSource readyTcs;

		/// <summary>
		/// The <see cref="CancellationTokenSource"/> for <see cref="Initialize(CancellationToken)"/>.
		/// </summary>
		readonly CancellationTokenSource startupCancellationTokenSource;

		/// <summary>
		/// The <see cref="CancellationTokenSource"/> linked with the token given to <see cref="StopAsync(CancellationToken)"/>.
		/// </summary>
		readonly CancellationTokenSource shutdownCancellationTokenSource;

		/// <summary>
		/// The count of online instances.
		/// </summary>
		readonly Gauge onlineInstances;

		/// <summary>
		/// The original <see cref="IConsole.Title"/> of <see cref="console"/>.
		/// </summary>
		readonly string? originalConsoleTitle;

		/// <summary>
		/// The <see cref="Task"/> returned by <see cref="Initialize(CancellationToken)"/>.
		/// </summary>
		Task? startupTask;

		/// <summary>
		/// If the <see cref="InstanceManager"/> has been <see cref="DisposeAsync"/>'d.
		/// </summary>
		bool disposed;

		/// <summary>
		/// Initializes a new instance of the <see cref="InstanceManager"/> class.
		/// </summary>
		/// <param name="instanceFactory">The value of <see cref="instanceFactory"/>.</param>
		/// <param name="ioManager">The value of <paramref name="ioManager"/>.</param>
		/// <param name="databaseContextFactory">The value of <paramref name="databaseContextFactory"/>.</param>
		/// <param name="assemblyInformationProvider">The value of <see cref="assemblyInformationProvider"/>.</param>
		/// <param name="jobService">The value of <see cref="jobService"/>.</param>
		/// <param name="serverControl">The value of <see cref="serverControl"/>.</param>
		/// <param name="systemIdentityFactory">The value of <see cref="systemIdentityFactory"/>.</param>
		/// <param name="asyncDelayer">The value of <see cref="asyncDelayer"/>.</param>
		/// <param name="serverPortProvider">The value of <see cref="serverPortProvider"/>.</param>
		/// <param name="swarmServiceController">The value of <see cref="swarmServiceController"/>.</param>
		/// <param name="console">The value of <see cref="console"/>.</param>
		/// <param name="platformIdentifier">The value of <see cref="platformIdentifier"/>.</param>
		/// <param name="metricFactory">The <see cref="IMetricFactory"/> used to create metrics.</param>
		/// <param name="collectorRegistry">The <see cref="ICollectorRegistry"/> to use.</param>
		/// <param name="generalConfigurationOptions">The <see cref="IOptions{TOptions}"/> containing the value of <see cref="generalConfiguration"/>.</param>
		/// <param name="swarmConfigurationOptions">The <see cref="IOptions{TOptions}"/> containing the value of <see cref="swarmConfiguration"/>.</param>
		/// <param name="internalConfigurationOptions">The <see cref="IOptions{TOptions}"/> containing the value of <see cref="internalConfiguration"/>.</param>
		/// <param name="logger">The value of <see cref="logger"/>.</param>
		public InstanceManager(
			IInstanceFactory instanceFactory,
			IIOManager ioManager,
			IDatabaseContextFactory databaseContextFactory,
			IAssemblyInformationProvider assemblyInformationProvider,
			IJobService jobService,
			IServerControl serverControl,
			ISystemIdentityFactory systemIdentityFactory,
			IAsyncDelayer asyncDelayer,
			IServerPortProvider serverPortProvider,
			ISwarmServiceController swarmServiceController,
			IConsole console,
			IPlatformIdentifier platformIdentifier,
			IMetricFactory metricFactory,
			ICollectorRegistry collectorRegistry,
			IOptions<GeneralConfiguration> generalConfigurationOptions,
			IOptions<SwarmConfiguration> swarmConfigurationOptions,
			IOptions<InternalConfiguration> internalConfigurationOptions,
			ILogger<InstanceManager> logger)
		{
			this.instanceFactory = instanceFactory ?? throw new ArgumentNullException(nameof(instanceFactory));
			this.ioManager = ioManager ?? throw new ArgumentNullException(nameof(ioManager));
			this.databaseContextFactory = databaseContextFactory ?? throw new ArgumentNullException(nameof(databaseContextFactory));
			this.assemblyInformationProvider = assemblyInformationProvider ?? throw new ArgumentNullException(nameof(assemblyInformationProvider));
			this.jobService = jobService ?? throw new ArgumentNullException(nameof(jobService));
			this.serverControl = serverControl ?? throw new ArgumentNullException(nameof(serverControl));
			this.systemIdentityFactory = systemIdentityFactory ?? throw new ArgumentNullException(nameof(systemIdentityFactory));
			this.asyncDelayer = asyncDelayer ?? throw new ArgumentNullException(nameof(asyncDelayer));
			this.serverPortProvider = serverPortProvider ?? throw new ArgumentNullException(nameof(serverPortProvider));
			this.swarmServiceController = swarmServiceController ?? throw new ArgumentNullException(nameof(swarmServiceController));
			this.console = console ?? throw new ArgumentNullException(nameof(console));
			this.platformIdentifier = platformIdentifier ?? throw new ArgumentNullException(nameof(platformIdentifier));
			ArgumentNullException.ThrowIfNull(metricFactory);
			ArgumentNullException.ThrowIfNull(collectorRegistry);
			generalConfiguration = generalConfigurationOptions?.Value ?? throw new ArgumentNullException(nameof(generalConfigurationOptions));
			swarmConfiguration = swarmConfigurationOptions?.Value ?? throw new ArgumentNullException(nameof(swarmConfigurationOptions));
			internalConfiguration = internalConfigurationOptions?.Value ?? throw new ArgumentNullException(nameof(internalConfigurationOptions));
			this.logger = logger ?? throw new ArgumentNullException(nameof(logger));

			originalConsoleTitle = console.Title;

			onlineInstances = metricFactory.CreateGauge("tgs_online_instances", "The total number of instances online");

			instances = new Dictionary<long, ReferenceCountingContainer<IInstance, InstanceWrapper>>();
			bridgeHandlers = new Dictionary<string, IBridgeHandler>();
			readyTcs = new TaskCompletionSource();
			instanceStateChangeSemaphore = new SemaphoreSlim(1);
			startupCancellationTokenSource = new CancellationTokenSource();
			shutdownCancellationTokenSource = new CancellationTokenSource();

			collectorRegistry.AddBeforeCollectCallback(async cancellationToken =>
			{
				using (await SemaphoreSlimContext.Lock(instanceStateChangeSemaphore, cancellationToken))
					foreach (var container in instances.Values)
						container.Instance.Watchdog.RunMetricsScrape();
			});
		}

		/// <inheritdoc />
		public async ValueTask DisposeAsync()
		{
			lock (instances)
			{
				if (disposed)
					return;
				disposed = true;
			}

			foreach (var instanceKvp in instances)
				await instanceKvp.Value.Instance.DisposeAsync();

			instanceStateChangeSemaphore.Dispose();
			startupCancellationTokenSource.Dispose();
			shutdownCancellationTokenSource.Dispose();

			logger.LogInformation("Server shutdown");
		}

		/// <inheritdoc />
		public IInstanceReference? GetInstanceReference(long instanceId)
		{
			lock (instances)
			{
				if (!instances.TryGetValue(instanceId, out var instance))
					return null;

				return instance.AddReference();
			}
		}

		/// <inheritdoc />
		public async ValueTask MoveInstance(Models.Instance instance, string oldPath, CancellationToken cancellationToken)
		{
			ArgumentNullException.ThrowIfNull(oldPath);

			using var lockContext = await SemaphoreSlimContext.Lock(instanceStateChangeSemaphore, cancellationToken);
			using var instanceReferenceCheck = this.GetInstanceReference(instance);
			if (instanceReferenceCheck != null)
				throw new InvalidOperationException("Cannot move an online instance!");
			var newPath = instance.Path!;
			try
			{
				await ioManager.MoveDirectory(oldPath, newPath, cancellationToken);

				// Delete the Game directory to clear out broken symlinks
				var instanceGameIOManager = instanceFactory.CreateGameIOManager(instance);
				await instanceGameIOManager.DeleteDirectory(DefaultIOManager.CurrentDirectory, cancellationToken);
			}
			catch (Exception ex)
			{
				logger.LogError(
					ex,
					"Error moving instance {instanceId}!",
					instance.Id);
				try
				{
					logger.LogDebug("Reverting instance {instanceId}'s path to {oldPath} in the DB...", instance.Id, oldPath);

					// DCT: Operation must always run
					await databaseContextFactory.UseContextTaskReturn(db =>
					{
						var targetInstance = new Models.Instance
						{
							Id = instance.Id,
						};
						db.Instances.Attach(targetInstance);
						targetInstance.Path = oldPath;
						return db.Save(CancellationToken.None);
					});
				}
				catch (Exception innerEx)
				{
					logger.LogCritical(
						innerEx,
						"Error reverting database after failing to move instance {instanceId}! Attempting to detach...",
						instance.Id);

					try
					{
						// DCT: Operation must always run
						await ioManager.WriteAllBytes(
							ioManager.ConcatPath(oldPath, InstanceController.InstanceAttachFileName),
							Array.Empty<byte>(),
							CancellationToken.None);
					}
					catch (Exception tripleEx)
					{
						logger.LogCritical(
							tripleEx,
							"Okay, what gamma radiation are you under? Failed to write instance attach file!");

						throw new AggregateException(tripleEx, innerEx, ex);
					}

					throw new AggregateException(ex, innerEx);
				}

				throw;
			}
		}

		/// <inheritdoc />
		public async ValueTask OfflineInstance(Models.Instance metadata, User user, CancellationToken cancellationToken)
		{
			ArgumentNullException.ThrowIfNull(metadata);

			using (await SemaphoreSlimContext.Lock(instanceStateChangeSemaphore, cancellationToken))
			{
				ReferenceCountingContainer<IInstance, InstanceWrapper>? container;
				var instanceId = metadata.Require(x => x.Id);
				lock (instances)
				{
					if (!instances.TryGetValue(instanceId, out container))
					{
						logger.LogDebug("Not offlining removed instance {instanceId}", metadata.Id);
						return;
					}

					instances.Remove(instanceId);
				}

				logger.LogInformation("Offlining instance ID {instanceId}", metadata.Id);

				try
				{
					await container.OnZeroReferences.WaitAsync(cancellationToken);

					// we are the one responsible for cancelling his jobs
					ValueTask<Job?[]> groupedTask = default;
					await databaseContextFactory.UseContext(
						async db =>
						{
							var jobs = await db
								.Jobs
								.Where(x => x.Instance!.Id == metadata.Id && !x.StoppedAt.HasValue)
								.Select(x => new Job(x.Id!.Value))
								.ToListAsync(cancellationToken);

							groupedTask = ValueTaskExtensions.WhenAll(
								jobs.Select(job => jobService.CancelJob(job, user, true, cancellationToken)),
								jobs.Count);
						});

					await groupedTask;
				}
				catch
				{
					// not too late to change your mind
					lock (instances)
						instances.Add(instanceId, container);

					throw;
				}

				try
				{
					// at this point we can't really stop offlining the instance just because the request was cancelled
					await container.Instance.StopAsync(shutdownCancellationTokenSource.Token);
				}
				finally
				{
					await container.Instance.DisposeAsync();
					onlineInstances.Dec();
				}
			}
		}

		/// <inheritdoc />
		public async ValueTask OnlineInstance(Models.Instance metadata, CancellationToken cancellationToken)
		{
			ArgumentNullException.ThrowIfNull(metadata);

			var instanceId = metadata.Require(x => x.Id);
			using var lockContext = await SemaphoreSlimContext.Lock(instanceStateChangeSemaphore, cancellationToken);
			lock (instances)
				if (instances.ContainsKey(instanceId))
				{
					logger.LogDebug("Aborting instance creation due to it seemingly already being online");
					return;
				}

			logger.LogInformation("Onlining instance ID {instanceId} ({instanceName}) at {instancePath}", metadata.Id, metadata.Name, metadata.Path);
			var instance = await instanceFactory.CreateInstance(this, metadata);
			try
			{
				await instance.StartAsync(cancellationToken);

				try
				{
					lock (instances)
						instances.Add(
							instanceId,
							new ReferenceCountingContainer<IInstance, InstanceWrapper>(instance));

					onlineInstances.Inc();
				}
				catch (Exception ex)
				{
					logger.LogError("Unable to commit onlined instance {instanceId} into service, offlining!", metadata.Id);
					try
					{
						// DCT: Must always run
						await instance.StopAsync(CancellationToken.None);
					}
					catch (Exception innerEx)
					{
						throw new AggregateException(innerEx, ex);
					}

					throw;
				}
			}
			catch
			{
				await instance.DisposeAsync();
				throw;
			}
		}

		/// <inheritdoc />
		public Task StartAsync(CancellationToken cancellationToken)
		{
			cancellationToken.Register(startupCancellationTokenSource.Cancel);
			startupTask = Initialize(startupCancellationTokenSource.Token);
			return Task.CompletedTask;
		}

		/// <inheritdoc />
		public async Task StopAsync(CancellationToken cancellationToken)
		{
			try
			{
				using (cancellationToken.Register(shutdownCancellationTokenSource.Cancel))
					try
					{
						if (startupTask == null)
						{
							logger.LogWarning("InstanceManager was never started!");
							return;
						}

						logger.LogDebug("Stopping instance manager...");

						if (!startupTask.IsCompleted)
						{
							logger.LogTrace("Interrupting startup task...");
							startupCancellationTokenSource.Cancel();
							await startupTask;
						}

						var instanceFactoryStopTask = instanceFactory.StopAsync(cancellationToken);
						await jobService.StopAsync(cancellationToken);

						async ValueTask OfflineInstanceImmediate(IInstance instance, CancellationToken cancellationToken)
						{
							try
							{
								await instance.StopAsync(cancellationToken);
							}
							catch (Exception ex)
							{
								logger.LogError(ex, "Instance shutdown exception!");
							}
						}

						await ValueTaskExtensions.WhenAll(instances.Select(x => OfflineInstanceImmediate(x.Value.Instance, cancellationToken)));
						await instanceFactoryStopTask;

						await swarmServiceController.Shutdown(cancellationToken);
					}
					finally
					{
						if (originalConsoleTitle != null)
							console.SetTitle(originalConsoleTitle);
					}
			}
			catch (Exception ex)
			{
				logger.LogCritical(ex, "Instance manager stop exception!");
			}
		}

		/// <inheritdoc />
		public async ValueTask<BridgeResponse?> ProcessBridgeRequest(BridgeParameters parameters, CancellationToken cancellationToken)
		{
			ArgumentNullException.ThrowIfNull(parameters);

			var accessIdentifier = parameters.AccessIdentifier;
			if (accessIdentifier == null)
			{
				logger.LogWarning("Received invalid bridge request with null access identifier!");
				return null;
			}

			IBridgeHandler? bridgeHandler = null;
			var loggedDelay = false;
			for (var i = 0; bridgeHandler == null && i < 30; ++i)
			{
				// There's a miniscule time period where we could potentially receive a bridge request and not have the registration ready when we launch DD
				// This is a stopgap
				Task delayTask = Task.CompletedTask;
				lock (bridgeHandlers)
					if (!bridgeHandlers.TryGetValue(accessIdentifier, out bridgeHandler))
					{
						if (!loggedDelay)
						{
							logger.LogTrace("Received bridge request with unregistered access identifier \"{aid}\". Waiting up to 3 seconds for it to be registered...", accessIdentifier);
							loggedDelay = true;
						}

						delayTask = asyncDelayer.Delay(TimeSpan.FromMilliseconds(100), cancellationToken).AsTask();
					}

				await delayTask;
			}

			if (bridgeHandler == null)
				lock (bridgeHandlers)
					if (!bridgeHandlers.TryGetValue(accessIdentifier, out bridgeHandler))
					{
						logger.LogWarning("Received invalid bridge request with access identifier: {accessIdentifier}", accessIdentifier);
						return null;
					}

			return await bridgeHandler.ProcessBridgeRequest(parameters, cancellationToken);
		}

		/// <inheritdoc />
		public IBridgeRegistration RegisterHandler(IBridgeHandler bridgeHandler)
		{
			ArgumentNullException.ThrowIfNull(bridgeHandler);

			var accessIdentifier = bridgeHandler.DMApiParameters.AccessIdentifier
				?? throw new InvalidOperationException("Attempted bridge registration with null AccessIdentifier!");
			lock (bridgeHandlers)
			{
				bridgeHandlers.Add(accessIdentifier, bridgeHandler);
				logger.LogTrace("Registered bridge handler: {accessIdentifier}", accessIdentifier);
			}

			return new BridgeRegistration(() =>
			{
				lock (bridgeHandlers)
				{
					bridgeHandlers.Remove(accessIdentifier);
					logger.LogTrace("Unregistered bridge handler: {accessIdentifier}", accessIdentifier);
				}
			});
		}

		/// <inheritdoc />
		public IInstanceCore? GetInstance(Models.Instance metadata)
		{
			lock (instances)
			{
				instances.TryGetValue(metadata.Require(x => x.Id), out var container);
				return container?.Instance;
			}
		}

		/// <summary>
		/// Initializes the <see cref="InstanceManager"/>.
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task"/> representing the running operation.</returns>
		async Task Initialize(CancellationToken cancellationToken)
		{
			try
			{
				logger.LogInformation("{versionString}", assemblyInformationProvider.VersionString);
				console.SetTitle(assemblyInformationProvider.VersionString);

				PreflightChecks();

				// To let the web server startup immediately before we do any intense work
				await Task.Yield();

				await InitializeSwarm(cancellationToken);

				List<Models.Instance>? dbInstances = null;

				async ValueTask EnumerateInstances(IDatabaseContext databaseContext)
					=> dbInstances = await databaseContext
						.Instances
						.Where(x => x.Online!.Value && x.SwarmIdentifer == swarmConfiguration.Identifier)
						.Include(x => x.RepositorySettings)
						.Include(x => x.ChatSettings)
							.ThenInclude(x => x.Channels)
						.Include(x => x.DreamDaemonSettings)
						.ToListAsync(cancellationToken);

				var instanceEnumeration = databaseContextFactory.UseContext(EnumerateInstances);

				var factoryStartup = instanceFactory.StartAsync(cancellationToken);
				var jobManagerStartup = jobService.StartAsync(cancellationToken);

				await Task.WhenAll(instanceEnumeration.AsTask(), factoryStartup, jobManagerStartup);

				var instanceOnliningTasks = dbInstances!.Select(
					async metadata =>
					{
						try
						{
							await OnlineInstance(metadata, cancellationToken);
						}
						catch (Exception ex)
						{
							logger.LogError(ex, "Failed to online instance {instanceId}!", metadata.Id);
						}
					});

				await Task.WhenAll(instanceOnliningTasks);

				logger.LogInformation("Server ready!");
				readyTcs.SetResult();

				// this needs to happen after the HTTP API opens with readyTcs otherwise it can race and cause failed bridge requests with 503's
				jobService.Activate(this);
			}
			catch (OperationCanceledException ex)
			{
				logger.LogInformation(ex, "Cancelled instance manager initialization!");
			}
			catch (Exception e)
			{
				logger.LogCritical(e, "Instance manager startup error!");
				try
				{
					await serverControl.Die(e);
					return;
				}
				catch (Exception e2)
				{
					logger.LogCritical(e2, "Failed to kill server!");
				}
			}
		}

		/// <summary>
		/// Check we have a valid system and configuration.
		/// </summary>
		void PreflightChecks()
		{
			logger.LogDebug("Running as user: {username}", Environment.UserName);

			generalConfiguration.CheckCompatibility(logger, ioManager);

			using (var systemIdentity = systemIdentityFactory.GetCurrent())
			{
				if (!systemIdentity.CanCreateSymlinks)
					throw new InvalidOperationException($"The user running {Constants.CanonicalPackageName} cannot create symlinks! Please try running as an administrative user!");

				if (!platformIdentifier.IsWindows && systemIdentity.IsSuperUser && !internalConfiguration.UsingDocker)
				{
					logger.LogWarning("TGS is being run as the root account. This is not recommended.");
				}
			}

			// This runs before the real socket is opened, ensures we don't perform reattaches unless we're fairly certain the bind won't fail
			// If it does fail, DD will be killed.
			SocketExtensions.BindTest(platformIdentifier, serverPortProvider.HttpApiPort, true, false);
		}

		/// <summary>
		/// Initializes the connection to the TGS swarm.
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask"/> representing the running operation.</returns>
		async ValueTask InitializeSwarm(CancellationToken cancellationToken)
		{
			SwarmRegistrationResult registrationResult;
			do
			{
				registrationResult = await swarmServiceController.Initialize(cancellationToken);

				if (registrationResult == SwarmRegistrationResult.Unauthorized)
					throw new InvalidOperationException("Swarm private key does not match the swarm controller's!");

				if (registrationResult == SwarmRegistrationResult.VersionMismatch)
					throw new InvalidOperationException("Swarm controller's TGS version does not match our own!");

				if (registrationResult != SwarmRegistrationResult.Success)
					await asyncDelayer.Delay(TimeSpan.FromSeconds(5), cancellationToken);
			}
			while (registrationResult != SwarmRegistrationResult.Success && !cancellationToken.IsCancellationRequested);
		}
	}
}
