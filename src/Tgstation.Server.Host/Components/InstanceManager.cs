using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Host.Core;
using Tgstation.Server.Host.Database;
using Tgstation.Server.Host.IO;
using Tgstation.Server.Host.Jobs;
using Tgstation.Server.Host.Security;
using Tgstation.Server.Host.System;

namespace Tgstation.Server.Host.Components
{
	/// <inheritdoc />
	sealed class InstanceManager : IInstanceManager, IRestartHandler, IHostedService, IDisposable
	{
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
		/// The <see cref="IApplication"/> for the <see cref="InstanceManager"/>
		/// </summary>
		readonly IApplication application;

		/// <summary>
		/// The <see cref="IJobManager"/> for the <see cref="InstanceManager"/>
		/// </summary>
		readonly IJobManager jobManager;

		/// <summary>
		/// The <see cref="IServerControl"/> for the <see cref="InstanceManager"/>
		/// </summary>
		readonly IServerControl serverControl;

		/// <summary>
		/// The <see cref="IPlatformIdentifier"/> for the <see cref="InstanceManager"/>
		/// </summary>
		readonly IPlatformIdentifier platformIdentifier;

		/// <summary>
		/// The <see cref="ISystemIdentityFactory"/> for the <see cref="InstanceManager"/>
		/// </summary>
		readonly ISystemIdentityFactory systemIdentityFactory;

		/// <summary>
		/// The <see cref="ILogger"/> for the <see cref="InstanceManager"/>
		/// </summary>
		readonly ILogger<InstanceManager> logger;

		/// <summary>
		/// Map of <see cref="Api.Models.Instance.Id"/>s to respective <see cref="IInstance"/>s
		/// </summary>
		readonly Dictionary<long, IInstance> instances;

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
		/// <param name="application">The value of <see cref="application"/></param>
		/// <param name="jobManager">The value of <see cref="jobManager"/></param>
		/// <param name="serverControl">The value of <see cref="serverControl"/></param>
		/// <param name="platformIdentifier">The value of <see cref="platformIdentifier"/>.</param>
		/// <param name="systemIdentityFactory">The value of <see cref="systemIdentityFactory"/>.</param>
		/// <param name="logger">The value of <see cref="logger"/></param>
		public InstanceManager(
			IInstanceFactory instanceFactory,
			IIOManager ioManager,
			IDatabaseContextFactory databaseContextFactory,
			IApplication application,
			IJobManager jobManager,
			IServerControl serverControl,
			IPlatformIdentifier platformIdentifier,
			ISystemIdentityFactory systemIdentityFactory,
			ILogger<InstanceManager> logger)
		{
			this.instanceFactory = instanceFactory ?? throw new ArgumentNullException(nameof(instanceFactory));
			this.ioManager = ioManager ?? throw new ArgumentNullException(nameof(ioManager));
			this.databaseContextFactory = databaseContextFactory ?? throw new ArgumentNullException(nameof(databaseContextFactory));
			this.application = application ?? throw new ArgumentNullException(nameof(application));
			this.jobManager = jobManager ?? throw new ArgumentNullException(nameof(jobManager));
			this.serverControl = serverControl ?? throw new ArgumentNullException(nameof(serverControl));
			this.platformIdentifier = platformIdentifier ?? throw new ArgumentNullException(nameof(platformIdentifier));
			this.systemIdentityFactory = systemIdentityFactory ?? throw new ArgumentNullException(nameof(systemIdentityFactory));
			this.logger = logger ?? throw new ArgumentNullException(nameof(logger));

			serverControl.RegisterForRestart(this);

			instances = new Dictionary<long, IInstance>();
		}

		/// <inheritdoc />
		public void Dispose()
		{
			lock (this)
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
			lock (this)
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
			lock (this)
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
					var jobs = db.Jobs.Where(x => x.Instance.Id == metadata.Id).Select(x => new Models.Job
					{
						Id = x.Id
					}).ToAsyncEnumerable();
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
			var instance = instanceFactory.CreateInstance(metadata);
			try
			{
				lock (this)
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
			try
			{
				CheckSystemCompatibility();
				var factoryStartup = instanceFactory.StartAsync(cancellationToken);
				await databaseContext.Initialize(cancellationToken).ConfigureAwait(false);
				await jobManager.StartAsync(cancellationToken).ConfigureAwait(false);
				var dbInstances = databaseContext.Instances.Where(x => x.Online.Value)
				.Include(x => x.RepositorySettings)
				.Include(x => x.ChatSettings)
				.ThenInclude(x => x.Channels)
				.Include(x => x.DreamDaemonSettings)
				.ToAsyncEnumerable();
				var tasks = new List<Task>();
				await factoryStartup.ConfigureAwait(false);
				await dbInstances.ForEachAsync(metadata => tasks.Add(metadata.Online.Value ? OnlineInstance(metadata, cancellationToken) : Task.CompletedTask), cancellationToken).ConfigureAwait(false);
				await Task.WhenAll(tasks).ConfigureAwait(false);
				logger.LogInformation("Server ready!");
				application.Ready(null);
			}
			catch (OperationCanceledException)
			{
				logger.LogInformation("Cancelled instance manager initialization!");
			}
			catch (Exception e)
			{
				logger.LogCritical("Instance manager startup error! Exception: {0}", e);
				application.Ready(e);
				try
				{
					await serverControl.Die(e).ConfigureAwait(false);
				}
				catch (Exception e2)
				{
					logger.LogCritical("Failed to kill server! Exception: {0}", e2);
				}
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
				await databaseContextFactory.UseContext(db => db.SchemaDowngradeForServerVersion(downgradeVersion, cancellationToken)).ConfigureAwait(false);
		}

		/// <inheritdoc />
		public Task HandleRestart(Version updateVersion, CancellationToken cancellationToken)
		{
			downgradeVersion = updateVersion != null && updateVersion < application.Version ? updateVersion : null;
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
	}
}
