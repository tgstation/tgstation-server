using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Host.Core;
using Tgstation.Server.Host.IO;

namespace Tgstation.Server.Host.Components
{
	/// <inheritdoc />
	sealed class InstanceManager : IInstanceManager, IHostedService, IInteropRegistrar, IDisposable
	{
		/// <summary>
		/// HTTP GET query key for interop access identifiers
		/// </summary>
		const string AccessIdentifierQueryKey = "access";

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
		/// The <see cref="ILogger"/> for the <see cref="InstanceManager"/>
		/// </summary>
		readonly ILogger<InstanceManager> logger;

		/// <summary>
		/// Map of <see cref="Api.Models.Instance.Id"/>s to respective <see cref="IInstance"/>s
		/// </summary>
		readonly Dictionary<long, IInstance> instances;
		/// <summary>
		/// Map of access identifiers to their respective <see cref="IInteropConsumer"/>
		/// </summary>
		readonly Dictionary<string, IInteropConsumer> interopConsumers;

		/// <summary>
		/// Construct an <see cref="InstanceManager"/>
		/// </summary>
		/// <param name="instanceFactory">The value of <see cref="instanceFactory"/></param>
		/// <param name="ioManager">The value of <paramref name="ioManager"/></param>
		/// <param name="databaseContextFactory">The value of <paramref name="databaseContextFactory"/></param>
		/// <param name="application">The value of <see cref="application"/></param>
		/// <param name="jobManager">The value of <see cref="jobManager"/></param>
		/// <param name="logger">The value of <see cref="logger"/></param>
		public InstanceManager(IInstanceFactory instanceFactory, IIOManager ioManager, IDatabaseContextFactory databaseContextFactory, IApplication application, IJobManager jobManager, ILogger<InstanceManager> logger)
		{
			this.instanceFactory = instanceFactory ?? throw new ArgumentNullException(nameof(instanceFactory));
			this.ioManager = ioManager ?? throw new ArgumentNullException(nameof(ioManager));
			this.databaseContextFactory = databaseContextFactory ?? throw new ArgumentNullException(nameof(databaseContextFactory));
			this.application = application ?? throw new ArgumentNullException(nameof(application));
			this.jobManager = jobManager ?? throw new ArgumentNullException(nameof(jobManager));
			this.logger = logger ?? throw new ArgumentNullException(nameof(logger));

			instances = new Dictionary<long, IInstance>();
			interopConsumers = new Dictionary<string, IInteropConsumer>();
		}

		/// <inheritdoc />
		public void Dispose()
		{
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
			instance.Path = ioManager.ResolvePath(newPath);
			await ioManager.DeleteDirectory(oldPath, cancellationToken).ConfigureAwait(false);
		}

		/// <inheritdoc />
		public async Task OfflineInstance(Models.Instance metadata, CancellationToken cancellationToken)
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
			var instance = instanceFactory.CreateInstance(metadata, this);
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
		public Task StartAsync(CancellationToken cancellationToken) => databaseContextFactory.UseContext(async databaseContext =>
		{
			try
			{
				await databaseContext.Initialize(cancellationToken).ConfigureAwait(false);
				await jobManager.StartAsync(cancellationToken).ConfigureAwait(false);
				var dbInstances = databaseContext.Instances.Where(x => x.Online.Value)
				.Include(x => x.RepositorySettings)
				.Include(x => x.ChatSettings)
				.ThenInclude(x => x.Channels)
				.Include(x => x.DreamDaemonSettings)
				.ToAsyncEnumerable();
				var tasks = new List<Task>();
				await dbInstances.ForEachAsync(metadata => tasks.Add(metadata.Online.Value ? OnlineInstance(metadata, cancellationToken) : Task.CompletedTask), cancellationToken).ConfigureAwait(false);
				await Task.WhenAll(tasks).ConfigureAwait(false);
				application.Ready(null);
			}
			catch (Exception e)
			{
				application.Ready(e);
			}
		});

		/// <inheritdoc />
		public async Task StopAsync(CancellationToken cancellationToken)
		{
			await Task.WhenAll(instances.Select(x => x.Value.StopAsync(cancellationToken))).ConfigureAwait(false);
			await jobManager.StopAsync(cancellationToken).ConfigureAwait(false);
		}

		/// <inheritdoc />
		public IInteropContext Register(string accessIdentifier, IInteropConsumer consumer)
		{
			if (accessIdentifier == null)
				throw new ArgumentNullException(nameof(accessIdentifier));
			if (consumer == null)
				throw new ArgumentNullException(nameof(consumer));

			lock (interopConsumers)
				interopConsumers.Add(accessIdentifier, consumer);
			return new InteropContext(() =>
			{
				lock (interopConsumers)
					interopConsumers.Remove(accessIdentifier);
			});
		}

		/// <inheritdoc />
		public async Task<object> HandleWorldExport(IQueryCollection query, CancellationToken cancellationToken)
		{
			if (query == null)
				throw new ArgumentNullException(nameof(query));

			if (!query.TryGetValue(AccessIdentifierQueryKey, out StringValues values))
				return null;
			var accessIdentifier = values.FirstOrDefault();
			if (accessIdentifier == default)
				return null;

			IInteropConsumer consumer;
			lock (interopConsumers)
				if (!interopConsumers.TryGetValue(accessIdentifier, out consumer))
					return null;

			return await consumer.HandleInterop(query, cancellationToken).ConfigureAwait(false);
		}
	}
}
