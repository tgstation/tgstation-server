using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Api.Models;
using Tgstation.Server.Host.Components.Interop;
using Tgstation.Server.Host.Components.Interop.Bridge;
using Tgstation.Server.Host.Configuration;
using Tgstation.Server.Host.Controllers;
using Tgstation.Server.Host.Core;
using Tgstation.Server.Host.Database;
using Tgstation.Server.Host.IO;
using Tgstation.Server.Host.Jobs;
using Tgstation.Server.Host.Security;
using Tgstation.Server.Host.System;

namespace Tgstation.Server.Host.Components
{
	/// <inheritdoc />
	sealed class InstanceManager :
		IInstanceManager,
		IInstanceCoreProvider,
		IRestartHandler,
		IHostedService,
		IBridgeRegistrar,
		IAsyncDisposable
	{
		/// <inheritdoc />
		public Task Ready => readyTcs.Task;

		/// <summary>
		/// The <see cref="Lazy{T}"/> <see cref="IRestartRegistration"/> for the <see cref="InstanceManager"/>;
		/// </summary>
		readonly Lazy<IRestartRegistration> lazyRestartRegistration;

		/// <summary>
		/// The <see cref="IInstanceFactory"/> for the <see cref="InstanceManager"/>
		/// </summary>
		readonly IInstanceFactory instanceFactory;

		/// <summary>
		/// The <see cref="IIOManager"/> for the <see cref="InstanceManager"/>
		/// </summary>
		readonly IIOManager ioManager;

		/// <summary>
		/// The <see cref="IDatabaseContextFactory"/> for the <see cref="InstanceManager"/>
		/// </summary>
		readonly IDatabaseContextFactory databaseContextFactory;

		/// <summary>
		/// The <see cref="IAssemblyInformationProvider"/> for the <see cref="InstanceManager"/>
		/// </summary>
		readonly IAssemblyInformationProvider assemblyInformationProvider;

		/// <summary>
		/// The <see cref="IJobManager"/> for the <see cref="InstanceManager"/>
		/// </summary>
		readonly IJobManager jobManager;

		/// <summary>
		/// The <see cref="IServerControl"/> for the <see cref="InstanceManager"/>
		/// </summary>
		readonly IServerControl serverControl;

		/// <summary>
		/// The <see cref="ISystemIdentityFactory"/> for the <see cref="InstanceManager"/>
		/// </summary>
		readonly ISystemIdentityFactory systemIdentityFactory;

		/// <summary>
		/// The <see cref="IAsyncDelayer"/> for the <see cref="InstanceManager"/>
		/// </summary>
		readonly IAsyncDelayer asyncDelayer;

		/// <summary>
		/// The <see cref="IDatabaseSeeder"/> for the <see cref="InstanceManager"/>
		/// </summary>
		readonly IDatabaseSeeder databaseSeeder;

		/// <summary>
		/// The <see cref="IServerPortProvider"/> for the <see cref="InstanceManager"/>
		/// </summary>
		readonly IServerPortProvider serverPortProvider;

		/// <summary>
		/// The <see cref="ILogger"/> for the <see cref="InstanceManager"/>
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
		/// The <see cref="TaskCompletionSource{TResult}"/> for <see cref="Ready"/>.
		/// </summary>
		readonly TaskCompletionSource<object> readyTcs;

		/// <summary>
		/// Used in <see cref="StopAsync(CancellationToken)"/> to determine if database downgrades must be made
		/// </summary>
		Version downgradeVersion;

		/// <summary>
		/// If the <see cref="InstanceManager"/> has been <see cref="DisposeAsync"/>'d
		/// </summary>
		bool disposed;

		/// <summary>
		/// Construct an <see cref="InstanceManager"/>
		/// </summary>
		/// <param name="instanceFactory">The value of <see cref="instanceFactory"/></param>
		/// <param name="ioManager">The value of <paramref name="ioManager"/></param>
		/// <param name="databaseContextFactory">The value of <paramref name="databaseContextFactory"/></param>
		/// <param name="assemblyInformationProvider">The value of <see cref="assemblyInformationProvider"/></param>
		/// <param name="jobManager">The value of <see cref="jobManager"/></param>
		/// <param name="serverControl">The value of <see cref="serverControl"/></param>
		/// <param name="systemIdentityFactory">The value of <see cref="systemIdentityFactory"/>.</param>
		/// <param name="asyncDelayer">The value of <see cref="asyncDelayer"/>.</param>
		/// <param name="databaseSeeder">The value of <see cref="databaseSeeder"/>.</param>
		/// <param name="serverPortProvider">The value of <see cref="serverPortProvider"/>.</param>
		/// <param name="generalConfigurationOptions">The <see cref="IOptions{TOptions}"/> containing the value of <see cref="generalConfiguration"/>.</param>
		/// <param name="logger">The value of <see cref="logger"/></param>
		public InstanceManager(
			IInstanceFactory instanceFactory,
			IIOManager ioManager,
			IDatabaseContextFactory databaseContextFactory,
			IAssemblyInformationProvider assemblyInformationProvider,
			IJobManager jobManager,
			IServerControl serverControl,
			ISystemIdentityFactory systemIdentityFactory,
			IAsyncDelayer asyncDelayer,
			IDatabaseSeeder databaseSeeder,
			IServerPortProvider serverPortProvider,
			IOptions<GeneralConfiguration> generalConfigurationOptions,
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
			this.databaseSeeder = databaseSeeder ?? throw new ArgumentNullException(nameof(databaseSeeder));
			this.serverPortProvider = serverPortProvider ?? throw new ArgumentNullException(nameof(serverPortProvider));
			generalConfiguration = generalConfigurationOptions?.Value ?? throw new ArgumentNullException(nameof(generalConfigurationOptions));
			this.logger = logger ?? throw new ArgumentNullException(nameof(logger));

			lazyRestartRegistration = new Lazy<IRestartRegistration>(() => serverControl.RegisterForRestart(this));

			instances = new Dictionary<long, InstanceContainer>();
			bridgeHandlers = new Dictionary<string, IBridgeHandler>();
			readyTcs = new TaskCompletionSource<object>();
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

			foreach (var I in instances)
				await I.Value.Instance.DisposeAsync().ConfigureAwait(false);

			lazyRestartRegistration.Value.Dispose();
			instanceStateChangeSemaphore.Dispose();

			logger.LogInformation("Server shutdown");
		}

		/// <inheritdoc />
		public IInstanceReference GetInstanceReference(Models.Instance metadata)
		{
			if (metadata == null)
				throw new ArgumentNullException(nameof(metadata));

			lock (instances)
			{
				if (!instances.TryGetValue(metadata.Id, out var instance))
				{
					logger.LogTrace("Cannot reference instance {0} as it is not online!", metadata.Id);
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
			if (GetInstanceReference(instance) != null)
				throw new InvalidOperationException("Cannot move an online instance!");
			var newPath = instance.Path;
			try
			{
				await ioManager.MoveDirectory(oldPath, newPath, cancellationToken).ConfigureAwait(false);
			}
			catch (Exception ex)
			{
				logger.LogError(
					"Error moving instance {0}! Exception: {2}",
					instance.Id,
					ex);
				try
				{
					logger.LogDebug("Reverting instance {0}'s path to {1} in the DB...", instance.Id, oldPath);

					// DCT: Operation must always run
					await databaseContextFactory.UseContext(db =>
					{
						var targetInstance = new Models.Instance
						{
							Id = instance.Id
						};
						db.Instances.Attach(targetInstance);
						targetInstance.Path = oldPath;
						return db.Save(default);
					}).ConfigureAwait(false);
				}
				catch (Exception innerEx)
				{
					logger.LogCritical(
						"Error reverting database after failing to move instance {0}! Attempting to detach. Exception: {1}",
						ex);

					try
					{
						// DCT: Operation must always run
						await ioManager.WriteAllBytes(
							ioManager.ConcatPath(oldPath, InstanceController.InstanceAttachFileName),
							Array.Empty<byte>(),
							default)
							.ConfigureAwait(false);
					}
					catch (Exception tripleEx)
					{
						logger.LogCritical(
							"Okay, what gamma radiation are you under? Failed to write instance attach file! Exception: {0}",
							tripleEx);

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

			using var _ = await SemaphoreSlimContext.Lock(instanceStateChangeSemaphore, cancellationToken).ConfigureAwait(false);

			logger.LogInformation("Offlining instance ID {0}", metadata.Id);
			InstanceContainer container;
			lock (instances)
			{
				if (!instances.TryGetValue(metadata.Id, out container))
				{
					logger.LogDebug("Not offlining removed instance {0}", metadata.Id);
					return;
				}

				instances.Remove(metadata.Id);
			}

			try
			{
				await container.OnZeroReferences.ConfigureAwait(false);

				// we are the one responsible for cancelling his jobs
				var tasks = new List<Task>();
				await databaseContextFactory.UseContext(async db =>
				{
					var jobs = db
						.Jobs
						.AsQueryable()
						.Where(x => x.Instance.Id == metadata.Id)
						.Select(x => new Models.Job
						{
							Id = x.Id
						});
					await jobs.ForEachAsync(job =>
					{
						lock (tasks)
							tasks.Add(jobManager.CancelJob(job, user, true, cancellationToken));
					}, cancellationToken).ConfigureAwait(false);
				}).ConfigureAwait(false);

				await Task.WhenAll(tasks).ConfigureAwait(false);

				await container.Instance.StopAsync(cancellationToken).ConfigureAwait(false);
			}
			finally
			{
				await container.Instance.DisposeAsync().ConfigureAwait(false);
			}
		}

		/// <inheritdoc />
		public async Task OnlineInstance(Models.Instance metadata, CancellationToken cancellationToken)
		{
			if (metadata == null)
				throw new ArgumentNullException(nameof(metadata));

			using var _ = await SemaphoreSlimContext.Lock(instanceStateChangeSemaphore, cancellationToken).ConfigureAwait(false);
			lock (instances)
				if (instances.ContainsKey(metadata.Id))
				{
					logger.LogDebug("Aborting instance creation due to it seemingly already being online");
					return;
				}

			logger.LogInformation("Onlining instance ID {0} ({1}) at {2}", metadata.Id, metadata.Name, metadata.Path);
			var instance = await instanceFactory.CreateInstance(this, metadata).ConfigureAwait(false);
			try
			{
				await instance.StartAsync(cancellationToken).ConfigureAwait(false);

				try
				{
					lock (instances)
						instances.Add(metadata.Id, new InstanceContainer(instance));
				}
				catch (Exception ex)
				{
					logger.LogError("Unable to commit onlined instance {0} into service, offlining!", metadata.Id);
					try
					{
						await instance.StopAsync(default).ConfigureAwait(false);
					}
					catch (Exception innerEx)
					{
						throw new AggregateException(innerEx, ex);
					}
				}
			}
			catch
			{
				await instance.DisposeAsync().ConfigureAwait(false);
				throw;
			}
		}

		/// <inheritdoc />
		#pragma warning disable CA1506 // TODO: Decomplexify
		public Task StartAsync(CancellationToken cancellationToken) => databaseContextFactory.UseContext(async databaseContext =>
		{
			logger.LogInformation(assemblyInformationProvider.VersionString);

			// we do this here because making the restart registration triggers a trace log message
			// The above log message should be the first one one startup
			var _ = lazyRestartRegistration.Value;

			try
			{
				generalConfiguration.CheckCompatibility(logger);

				CheckSystemCompatibility();
				var factoryStartup = instanceFactory.StartAsync(cancellationToken);
				await databaseSeeder.Initialize(databaseContext, cancellationToken).ConfigureAwait(false);
				await jobManager.StartAsync(cancellationToken).ConfigureAwait(false);
				var dbInstances = await databaseContext
					.Instances
					.AsQueryable()
					.Where(x => x.Online.Value)
					.Include(x => x.RepositorySettings)
					.Include(x => x.ChatSettings)
					.ThenInclude(x => x.Channels)
					.Include(x => x.DreamDaemonSettings)
					.ToListAsync(cancellationToken)
					.ConfigureAwait(false);
				await factoryStartup.ConfigureAwait(false);
				var tasks = dbInstances.Select(
					async metadata =>
					{
						try
						{
							await OnlineInstance(metadata, cancellationToken).ConfigureAwait(false);
						}
						catch (Exception ex)
						{
							logger.LogError("Failed to online instance {0}! Exception: {0}", ex);
						}
					})
					.ToList();
				await Task.WhenAll(tasks).ConfigureAwait(false);
				logger.LogInformation("Server ready!");
				readyTcs.SetResult(null);
			}
			catch (OperationCanceledException)
			{
				logger.LogInformation("Cancelled instance manager initialization!");
			}
			catch (Exception e)
			{
				logger.LogCritical("Instance manager startup error! Exception: {0}", e);
				try
				{
					await serverControl.Die(e).ConfigureAwait(false);
					return;
				}
				catch (Exception e2)
				{
					logger.LogCritical("Failed to kill server! Exception: {0}", e2);
				}

				throw;
			}
		});
		#pragma warning restore CA1506 // TODO: Decomplexify

		/// <inheritdoc />
		public async Task StopAsync(CancellationToken cancellationToken)
		{
			await jobManager.StopAsync(cancellationToken).ConfigureAwait(false);
			await Task.WhenAll(instances.Select(x => x.Value.Instance.StopAsync(cancellationToken))).ConfigureAwait(false);
			await instanceFactory.StopAsync(cancellationToken).ConfigureAwait(false);

			// downgrade the db if necessary
			if (downgradeVersion != null)
				await databaseContextFactory.UseContext(db => databaseSeeder.Downgrade(db, downgradeVersion, cancellationToken)).ConfigureAwait(false);
		}

		/// <inheritdoc />
		public Task HandleRestart(Version updateVersion, CancellationToken cancellationToken)
		{
			downgradeVersion = updateVersion != null && updateVersion < assemblyInformationProvider.Version ? updateVersion : null;
			return Task.CompletedTask;
		}

		/// <summary>
		/// Check we have a valid system identity.
		/// </summary>
		private void CheckSystemCompatibility()
		{
			using (var systemIdentity = systemIdentityFactory.GetCurrent())
			{
				if (!systemIdentity.CanCreateSymlinks)
					throw new InvalidOperationException("The user running tgstation-server cannot create symlinks! Please try running as an administrative user!");
			}

			// This runs before the real socket is opened, ensures we don't perform reattaches unless we're fairly certain the bind won't fail
			// If it does fail, DD will be killed.
			using var hostingSocket = new Socket(SocketType.Stream, ProtocolType.Tcp);
			hostingSocket.Bind(new IPEndPoint(IPAddress.Any, serverPortProvider.HttpApiPort));
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

				await delayTask.ConfigureAwait(false);
			}

			if (bridgeHandler == null)
				lock (bridgeHandlers)
					if (!bridgeHandlers.TryGetValue(parameters.AccessIdentifier, out bridgeHandler))
					{
						logger.LogWarning("Recieved invalid bridge request with accees identifier: {0}", parameters.AccessIdentifier);
						return null;
					}

			return await bridgeHandler.ProcessBridgeRequest(parameters, cancellationToken).ConfigureAwait(false);
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
				logger.LogTrace("Registered bridge handler: {0}", accessIdentifier);
			}

			return new BridgeRegistration(() =>
			{
				lock (bridgeHandlers)
				{
					bridgeHandlers.Remove(accessIdentifier);
					logger.LogTrace("Unregistered bridge handler: {0}", accessIdentifier);
				}
			});
		}

		/// <inheritdoc />
		public IInstanceCore GetInstance(Models.Instance metadata)
		{
			lock (instances)
			{
				instances.TryGetValue(metadata.Id, out var container);
				return container?.Instance;
			}
		}
	}
}
