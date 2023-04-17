using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Tgstation.Server.Api.Models;
using Tgstation.Server.Host.Components.Interop;
using Tgstation.Server.Host.Components.Interop.Bridge;
using Tgstation.Server.Host.Configuration;
using Tgstation.Server.Host.Controllers;
using Tgstation.Server.Host.Core;
using Tgstation.Server.Host.Database;
using Tgstation.Server.Host.Extensions;
using Tgstation.Server.Host.IO;
using Tgstation.Server.Host.Jobs;
using Tgstation.Server.Host.Security;
using Tgstation.Server.Host.Swarm;
using Tgstation.Server.Host.System;

namespace Tgstation.Server.Host.Components
{
	/// <inheritdoc />
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
		/// The <see cref="IJobManager"/> for the <see cref="InstanceManager"/>.
		/// </summary>
		readonly IJobManager jobManager;

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
		/// The <see cref="ISwarmService"/> for the <see cref="InstanceManager"/>.
		/// </summary>
		readonly ISwarmService swarmService;

		/// <summary>
		/// The <see cref="ILogger"/> for the <see cref="InstanceManager"/>.
		/// </summary>
		readonly ILogger<InstanceManager> logger;

		/// <summary>
		/// Map of instance <see cref="EntityId.Id"/>s to respective <see cref="InstanceContainer"/>s. Also used as a <see langword="lock"/> <see cref="object"/>.
		/// </summary>
		readonly IDictionary<long, InstanceContainer> instances;

		/// <summary>
		/// Map of <see cref="DMApiParameters.AccessIdentifier"/>s to their respective <see cref="IBridgeHandler"/>s.
		/// </summary>
		readonly IDictionary<string, IBridgeHandler> bridgeHandlers;

		/// <summary>
		/// <see cref="SemaphoreSlim"/> used to guard calls to <see cref="OnlineInstance(Models.Instance, CancellationToken)"/> and <see cref="OfflineInstance(Models.Instance, Models.User, CancellationToken)"/>.
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
		/// The <see cref="TaskCompletionSource"/> for <see cref="Ready"/>.
		/// </summary>
		readonly TaskCompletionSource readyTcs;

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
		/// <param name="jobManager">The value of <see cref="jobManager"/>.</param>
		/// <param name="serverControl">The value of <see cref="serverControl"/>.</param>
		/// <param name="systemIdentityFactory">The value of <see cref="systemIdentityFactory"/>.</param>
		/// <param name="asyncDelayer">The value of <see cref="asyncDelayer"/>.</param>
		/// <param name="serverPortProvider">The value of <see cref="serverPortProvider"/>.</param>
		/// <param name="swarmService">The value of <see cref="swarmService"/>.</param>
		/// <param name="generalConfigurationOptions">The <see cref="IOptions{TOptions}"/> containing the value of <see cref="generalConfiguration"/>.</param>
		/// <param name="swarmConfigurationOptions">The <see cref="IOptions{TOptions}"/> containing the value of <see cref="swarmConfiguration"/>.</param>
		/// <param name="logger">The value of <see cref="logger"/>.</param>
		public InstanceManager(
			IInstanceFactory instanceFactory,
			IIOManager ioManager,
			IDatabaseContextFactory databaseContextFactory,
			IAssemblyInformationProvider assemblyInformationProvider,
			IJobManager jobManager,
			IServerControl serverControl,
			ISystemIdentityFactory systemIdentityFactory,
			IAsyncDelayer asyncDelayer,
			IServerPortProvider serverPortProvider,
			ISwarmService swarmService,
			IOptions<GeneralConfiguration> generalConfigurationOptions,
			IOptions<SwarmConfiguration> swarmConfigurationOptions,
			ILogger<InstanceManager> logger)
		{
			this.instanceFactory = instanceFactory ?? throw new ArgumentNullException(nameof(instanceFactory));
			this.ioManager = ioManager ?? throw new ArgumentNullException(nameof(ioManager));
			this.databaseContextFactory = databaseContextFactory ?? throw new ArgumentNullException(nameof(databaseContextFactory));
			this.assemblyInformationProvider = assemblyInformationProvider ?? throw new ArgumentNullException(nameof(assemblyInformationProvider));
			this.jobManager = jobManager ?? throw new ArgumentNullException(nameof(jobManager));
			this.serverControl = serverControl ?? throw new ArgumentNullException(nameof(serverControl));
			this.systemIdentityFactory = systemIdentityFactory ?? throw new ArgumentNullException(nameof(systemIdentityFactory));
			this.asyncDelayer = asyncDelayer ?? throw new ArgumentNullException(nameof(asyncDelayer));
			this.serverPortProvider = serverPortProvider ?? throw new ArgumentNullException(nameof(serverPortProvider));
			this.swarmService = swarmService ?? throw new ArgumentNullException(nameof(swarmService));
			generalConfiguration = generalConfigurationOptions?.Value ?? throw new ArgumentNullException(nameof(generalConfigurationOptions));
			swarmConfiguration = swarmConfigurationOptions?.Value ?? throw new ArgumentNullException(nameof(swarmConfigurationOptions));
			this.logger = logger ?? throw new ArgumentNullException(nameof(logger));

			instances = new Dictionary<long, InstanceContainer>();
			bridgeHandlers = new Dictionary<string, IBridgeHandler>();
			readyTcs = new TaskCompletionSource();
			instanceStateChangeSemaphore = new SemaphoreSlim(1);
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

			logger.LogInformation("Server shutdown");
		}

		/// <inheritdoc />
		public IInstanceReference GetInstanceReference(Api.Models.Instance metadata)
		{
			if (metadata == null)
				throw new ArgumentNullException(nameof(metadata));

			lock (instances)
			{
				if (!instances.TryGetValue(metadata.Id.Value, out var instance))
				{
					logger.LogTrace("Cannot reference instance {instanceId} as it is not online or on this node!", metadata.Id);
					return null;
				}

				return instance.AddReference();
			}
		}

		/// <inheritdoc />
		public async Task MoveInstance(Models.Instance instance, string oldPath, CancellationToken cancellationToken)
		{
			if (oldPath == null)
				throw new ArgumentNullException(nameof(oldPath));

			using var lockContext = await SemaphoreSlimContext.Lock(instanceStateChangeSemaphore, cancellationToken);
			using var instanceReferenceCheck = GetInstanceReference(instance);
			if (instanceReferenceCheck != null)
				throw new InvalidOperationException("Cannot move an online instance!");
			var newPath = instance.Path;
			try
			{
				await ioManager.MoveDirectory(oldPath, newPath, cancellationToken);

				// Delete the Game directory to clear out broken symlinks
				var instanceGameIOManager = instanceFactory.CreateGameIOManager(instance);
				await instanceGameIOManager.DeleteDirectory(".", cancellationToken);
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
					await databaseContextFactory.UseContext(db =>
					{
						var targetInstance = new Models.Instance
						{
							Id = instance.Id,
						};
						db.Instances.Attach(targetInstance);
						targetInstance.Path = oldPath;
						return db.Save(default);
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
							default)
							;
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
		public async Task OfflineInstance(Models.Instance metadata, Models.User user, CancellationToken cancellationToken)
		{
			if (metadata == null)
				throw new ArgumentNullException(nameof(metadata));

			using var lockContext = await SemaphoreSlimContext.Lock(instanceStateChangeSemaphore, cancellationToken);

			logger.LogInformation("Offlining instance ID {instanceId}", metadata.Id);
			InstanceContainer container;
			lock (instances)
			{
				if (!instances.TryGetValue(metadata.Id.Value, out container))
				{
					logger.LogDebug("Not offlining removed instance {instanceId}", metadata.Id);
					return;
				}

				instances.Remove(metadata.Id.Value);
			}

			try
			{
				await container.OnZeroReferences;

				// we are the one responsible for cancelling his jobs
				var tasks = new List<Task>();
				await databaseContextFactory.UseContext(
					async db =>
					{
						var jobs = await db
							.Jobs
							.AsQueryable()
							.Where(x => x.Instance.Id == metadata.Id && !x.StoppedAt.HasValue)
							.Select(x => new Models.Job
							{
								Id = x.Id,
							})
							.ToListAsync(cancellationToken);
						foreach (var job in jobs)
							tasks.Add(jobManager.CancelJob(job, user, true, cancellationToken));
					});

				await Task.WhenAll(tasks);

				await container.Instance.StopAsync(cancellationToken);
			}
			finally
			{
				await container.Instance.DisposeAsync();
			}
		}

		/// <inheritdoc />
		public async Task OnlineInstance(Models.Instance metadata, CancellationToken cancellationToken)
		{
			if (metadata == null)
				throw new ArgumentNullException(nameof(metadata));

			using var lockContext = await SemaphoreSlimContext.Lock(instanceStateChangeSemaphore, cancellationToken);
			lock (instances)
				if (instances.ContainsKey(metadata.Id.Value))
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
						instances.Add(metadata.Id.Value, new InstanceContainer(instance));
				}
				catch (Exception ex)
				{
					logger.LogError("Unable to commit onlined instance {instanceId} into service, offlining!", metadata.Id);
					try
					{
						// DCT: Must always run
						await instance.StopAsync(default);
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
		public async Task StartAsync(CancellationToken cancellationToken)
		{
			try
			{
				logger.LogInformation("{versionString}", assemblyInformationProvider.VersionString);
				generalConfiguration.CheckCompatibility(logger);

				CheckSystemCompatibility();

				await InitializeSwarm(cancellationToken);

				List<Models.Instance> dbInstances = null;
				var instanceEnumeration = databaseContextFactory.UseContext(
					async databaseContext => dbInstances = await databaseContext
						.Instances
						.AsQueryable()
						.Where(x => x.Online.Value && x.SwarmIdentifer == swarmConfiguration.Identifier)
						.Include(x => x.RepositorySettings)
						.Include(x => x.ChatSettings)
							.ThenInclude(x => x.Channels)
						.Include(x => x.DreamDaemonSettings)
						.ToListAsync(cancellationToken));

				var factoryStartup = instanceFactory.StartAsync(cancellationToken);
				var jobManagerStartup = jobManager.StartAsync(cancellationToken);

				await Task.WhenAll(instanceEnumeration, factoryStartup, jobManagerStartup);

				var instanceOnliningTasks = dbInstances.Select(
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

				jobManager.Activate(this);

				logger.LogInformation("Server ready!");
				readyTcs.SetResult();
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

				throw;
			}
		}

		/// <inheritdoc />
		public async Task StopAsync(CancellationToken cancellationToken)
		{
			try
			{
				logger.LogDebug("Stopping instance manager...");
				var instanceFactoryStopTask = instanceFactory.StopAsync(cancellationToken);
				await jobManager.StopAsync(cancellationToken);

				async Task OfflineInstanceImmediate(IInstance instance, CancellationToken cancellationToken)
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

				await Task.WhenAll(instances.Select(x => OfflineInstanceImmediate(x.Value.Instance, cancellationToken)));
				await instanceFactoryStopTask;

				await swarmService.Shutdown(cancellationToken);
			}
			catch (Exception ex)
			{
				logger.LogCritical(ex, "Instance manager stop exception!");
			}
		}

		/// <inheritdoc />
		public async Task<BridgeResponse> ProcessBridgeRequest(BridgeParameters parameters, CancellationToken cancellationToken)
		{
			if (parameters == null)
				throw new ArgumentNullException(nameof(parameters));

			IBridgeHandler bridgeHandler = null;
			for (var i = 0; bridgeHandler == null && i < 30; ++i)
			{
				// There's a miniscule time period where we could potentially receive a bridge request and not have the registration ready when we launch DD
				// This is a stopgap
				Task delayTask = Task.CompletedTask;
				lock (bridgeHandlers)
					if (!bridgeHandlers.TryGetValue(parameters.AccessIdentifier, out bridgeHandler))
						delayTask = asyncDelayer.Delay(TimeSpan.FromMilliseconds(100), cancellationToken);

				await delayTask;
			}

			if (bridgeHandler == null)
				lock (bridgeHandlers)
					if (!bridgeHandlers.TryGetValue(parameters.AccessIdentifier, out bridgeHandler))
					{
						logger.LogWarning("Recieved invalid bridge request with access identifier: {accessIdentifier}", parameters.AccessIdentifier);
						return null;
					}

			return await bridgeHandler.ProcessBridgeRequest(parameters, cancellationToken);
		}

		/// <inheritdoc />
		public IBridgeRegistration RegisterHandler(IBridgeHandler bridgeHandler)
		{
			if (bridgeHandler == null)
				throw new ArgumentNullException(nameof(bridgeHandler));

			var accessIdentifier = bridgeHandler.DMApiParameters.AccessIdentifier;
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
		public IInstanceCore GetInstance(Models.Instance metadata)
		{
			lock (instances)
			{
				instances.TryGetValue(metadata.Id.Value, out var container);
				return container?.Instance;
			}
		}

		/// <summary>
		/// Check we have a valid system identity.
		/// </summary>
		void CheckSystemCompatibility()
		{
			using (var systemIdentity = systemIdentityFactory.GetCurrent())
			{
				if (!systemIdentity.CanCreateSymlinks)
					throw new InvalidOperationException("The user running tgstation-server cannot create symlinks! Please try running as an administrative user!");
			}

			// This runs before the real socket is opened, ensures we don't perform reattaches unless we're fairly certain the bind won't fail
			// If it does fail, DD will be killed.
			SocketExtensions.BindTest(serverPortProvider.HttpApiPort, true);
		}

		/// <summary>
		/// Initializes the connection to the TGS swarm.
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task"/> representing the running operation.</returns>
		async Task InitializeSwarm(CancellationToken cancellationToken)
		{
			SwarmRegistrationResult registrationResult;
			do
			{
				registrationResult = await swarmService.Initialize(cancellationToken);

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
