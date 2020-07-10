using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Api.Models;
using Tgstation.Server.Host.Components.Interop;
using Tgstation.Server.Host.Components.Interop.Bridge;
using Tgstation.Server.Host.Configuration;
using Tgstation.Server.Host.Core;
using Tgstation.Server.Host.Database;
using Tgstation.Server.Host.IO;
using Tgstation.Server.Host.Jobs;
using Tgstation.Server.Host.Security;
using Tgstation.Server.Host.System;

namespace Tgstation.Server.Host.Components
{
	/// <inheritdoc />
	sealed class InstanceManager : IInstanceManager, IRestartHandler, IHostedService, IBridgeRegistrar, IDisposable
	{
		/// <inheritdoc />
		public Task Ready => readyTcs.Task;

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
		/// The <see cref="ILogger"/> for the <see cref="InstanceManager"/>
		/// </summary>
		readonly ILogger<InstanceManager> logger;

		/// <summary>
		/// Map of instance <see cref="EntityId.Id"/>s to respective <see cref="IInstance"/>s. Also used as a <see langword="lock"/> <see cref="object"/>.
		/// </summary>
		readonly IDictionary<long, IInstance> instances;

		/// <summary>
		/// Map of <see cref="DMApiParameters.AccessIdentifier"/>s to their respective <see cref="IBridgeHandler"/>s.
		/// </summary>
		readonly IDictionary<string, IBridgeHandler> bridgeHandlers;

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
		/// If the <see cref="InstanceManager"/> has been <see cref="Dispose"/>d
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
			generalConfiguration = generalConfigurationOptions?.Value ?? throw new ArgumentNullException(nameof(generalConfigurationOptions));
			this.logger = logger ?? throw new ArgumentNullException(nameof(logger));

			serverControl.RegisterForRestart(this);

			instances = new Dictionary<long, IInstance>();
			bridgeHandlers = new Dictionary<string, IBridgeHandler>();
			readyTcs = new TaskCompletionSource<object>();
		}

		/// <inheritdoc />
		public void Dispose()
		{
			lock (instances)
			{
				if (disposed)
					return;
				disposed = true;
			}

			foreach (var I in instances)
				I.Value.Dispose();
		}

		/// <inheritdoc />
		public IInstance GetInstance(Models.Instance metadata)
		{
			if (metadata == null)
				throw new ArgumentNullException(nameof(metadata));
			lock (instances)
			{
				if (!instances.TryGetValue(metadata.Id, out IInstance instance))
					throw new InvalidOperationException("Instance not online!");
				return instance;
			}
		}

		/// <inheritdoc />
		public async Task MoveInstance(Models.Instance instance, string newPath, CancellationToken cancellationToken)
		{
			if (newPath == null)
				throw new ArgumentNullException(nameof(newPath));
			if (instance.Online.Value)
				throw new InvalidOperationException("Cannot move an online instance!");
			var oldPath = instance.Path;
			await ioManager.CopyDirectory(oldPath, newPath, null, cancellationToken).ConfigureAwait(false);
			await databaseContextFactory.UseContext(db =>
			{
				var targetInstance = new Models.Instance
				{
					Id = instance.Id
				};
				db.Instances.Attach(targetInstance);
				targetInstance.Path = newPath;
				return db.Save(cancellationToken);
			}).ConfigureAwait(false);
			await ioManager.DeleteDirectory(oldPath, cancellationToken).ConfigureAwait(false);
		}

		/// <inheritdoc />
		public async Task OfflineInstance(Models.Instance metadata, Models.User user, CancellationToken cancellationToken)
		{
			if (metadata == null)
				throw new ArgumentNullException(nameof(metadata));
			logger.LogInformation("Offlining instance ID {0}", metadata.Id);
			IInstance instance;
			lock (instances)
			{
				if (!instances.TryGetValue(metadata.Id, out instance))
					throw new InvalidOperationException("Instance not online!");
				instances.Remove(metadata.Id);
			}

			try
			{
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

				await instance.StopAsync(cancellationToken).ConfigureAwait(false);
			}
			finally
			{
				instance.Dispose();
			}
		}

		/// <inheritdoc />
		public async Task OnlineInstance(Models.Instance metadata, CancellationToken cancellationToken)
		{
			if (metadata == null)
				throw new ArgumentNullException(nameof(metadata));
			logger.LogInformation("Onlining instance ID {0} ({1}) at {2}", metadata.Id, metadata.Name, metadata.Path);
			var instance = instanceFactory.CreateInstance(this, metadata);
			try
			{
				lock (instances)
				{
					if (instances.ContainsKey(metadata.Id))
						throw new InvalidOperationException("Instance already online!");
					instances.Add(metadata.Id, instance);
				}
			}
			catch
			{
				instance.Dispose();
				throw;
			}

			await instance.StartAsync(cancellationToken).ConfigureAwait(false);
		}

		/// <inheritdoc />
		#pragma warning disable CA1506 // TODO: Decomplexify
		public Task StartAsync(CancellationToken cancellationToken) => databaseContextFactory.UseContext(async databaseContext =>
		{
			logger.LogInformation(assemblyInformationProvider.VersionString);

			try
			{
				generalConfiguration.CheckCompatibility(logger);

				CheckSystemCompatibility();
				var factoryStartup = instanceFactory.StartAsync(cancellationToken);
				await databaseSeeder.Initialize(databaseContext, cancellationToken).ConfigureAwait(false);
				await jobManager.StartAsync(cancellationToken).ConfigureAwait(false);
				var dbInstances = databaseContext
					.Instances
					.AsQueryable()
					.Where(x => x.Online.Value)
					.Include(x => x.RepositorySettings)
					.Include(x => x.DreamDaemonSettings)
					.Include(x => x.ChatSettings)
					.ThenInclude(x => x.Channels)
					.ToAsyncEnumerable();
				var tasks = new List<Task>();
				await factoryStartup.ConfigureAwait(false);
				await dbInstances.ForEachAsync(metadata => tasks.Add(metadata.Online.Value ? OnlineInstance(metadata, cancellationToken) : Task.CompletedTask), cancellationToken).ConfigureAwait(false);
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
			await Task.WhenAll(instances.Select(x => x.Value.StopAsync(cancellationToken))).ConfigureAwait(false);
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
			using var systemIdentity = systemIdentityFactory.GetCurrent();
			if (!systemIdentity.CanCreateSymlinks)
				throw new InvalidOperationException("The user running tgstation-server cannot create symlinks! Please try running as an administrative user!");
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
	}
}
