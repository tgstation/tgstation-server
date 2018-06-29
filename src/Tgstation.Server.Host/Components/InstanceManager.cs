using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Api.Models.Internal;
using Tgstation.Server.Host.Core;

namespace Tgstation.Server.Host.Components
{
	/// <inheritdoc />
	sealed class InstanceManager : IInstanceManager, IHostedService, IInteropRegistrar
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
		public InstanceManager(IInstanceFactory instanceFactory, IIOManager ioManager, IDatabaseContextFactory databaseContextFactory)
		{
			this.instanceFactory = instanceFactory ?? throw new ArgumentNullException(nameof(instanceFactory));
			this.ioManager = ioManager ?? throw new ArgumentNullException(nameof(ioManager));
			this.databaseContextFactory = databaseContextFactory ?? throw new ArgumentNullException(nameof(databaseContextFactory));
			instances = new Dictionary<long, IInstance>();
		}

		/// <inheritdoc />
		public IInstance GetInstance(Host.Models.Instance metadata)
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
		public async Task MoveInstance(Host.Models.Instance instance, string newPath, CancellationToken cancellationToken)
		{
			if (newPath == null)
				throw new ArgumentNullException(nameof(newPath));
			if (instance.Online)
				await OfflineInstance(instance, cancellationToken).ConfigureAwait(false);
			Task instanceOnlineTask = null;
			try
			{
				var oldPath = instance.Path;
				await ioManager.CopyDirectory(oldPath, newPath, null, cancellationToken).ConfigureAwait(false);
				instance.Path = ioManager.ResolvePath(newPath);
				instanceOnlineTask = OnlineInstance(instance, default);
				await ioManager.DeleteDirectory(oldPath, cancellationToken).ConfigureAwait(false);
			}
			finally
			{
				if (instance.Online)
					if (instanceOnlineTask == null)
						await OnlineInstance(instance, default).ConfigureAwait(false);
					else
						await instanceOnlineTask.ConfigureAwait(false);
			}
		}

		/// <inheritdoc />
		public async Task OfflineInstance(Host.Models.Instance metadata, CancellationToken cancellationToken)
		{
			if (metadata == null)
				throw new ArgumentNullException(nameof(metadata));
			IInstance instance;
			lock (this)
			{
				if (!instances.TryGetValue(metadata.Id, out instance))
					throw new InvalidOperationException("Instance not online!");
				instances.Remove(metadata.Id);
			}
			await instance.StopAsync(cancellationToken).ConfigureAwait(false);
		}

		/// <inheritdoc />
		public async Task OnlineInstance(Host.Models.Instance metadata, CancellationToken cancellationToken)
		{
			if (metadata == null)
				throw new ArgumentNullException(nameof(metadata));
			var instance = instanceFactory.CreateInstance(metadata);
			lock (this)
			{
				if (instances.ContainsKey(metadata.Id))
					throw new InvalidOperationException("Instance already online!");
				instances.Add(metadata.Id, instance);
			}
			await instance.StartAsync(cancellationToken).ConfigureAwait(false);
		}

		/// <inheritdoc />
		public Task StartAsync(CancellationToken cancellationToken) => databaseContextFactory.UseContext(async databaseContext =>
		{
			await databaseContext.Initialize(cancellationToken).ConfigureAwait(false);
			var dbInstances = databaseContext.Instances.Where(x => x.Online).Include(x => x.RepositorySettings).Include(x => x.ChatSettings).Include(x => x.DreamDaemonSettings).ToAsyncEnumerable();
			var tasks = new List<Task>();
			await dbInstances.ForEachAsync(metadata => tasks.Add(OnlineInstance(metadata, cancellationToken)), cancellationToken).ConfigureAwait(false);
			await Task.WhenAll(tasks).ConfigureAwait(false);
		});

		/// <inheritdoc />
		public async Task StopAsync(CancellationToken cancellationToken)
		{
			await Task.WhenAll(instances.Select(x => x.Value.StopAsync(cancellationToken))).ConfigureAwait(false);
			instances.Clear();
		}

		/// <inheritdoc />
		public Task<bool> PreserveActiveExecutablesIfNecessary(DreamDaemonLaunchParameters launchParameters, string accessToken, int pid, bool primary)
		{
			if (launchParameters == null)
				throw new ArgumentNullException(nameof(launchParameters));
			if (accessToken == null)
				throw new ArgumentNullException(nameof(accessToken));
			throw new NotImplementedException();
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
