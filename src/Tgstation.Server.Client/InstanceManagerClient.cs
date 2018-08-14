using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Api;
using Tgstation.Server.Api.Models;
using Tgstation.Server.Client.Components;

namespace Tgstation.Server.Client
{
	/// <inheritdoc />
	sealed class InstanceManagerClient : IInstanceManagerClient
	{
		/// <summary>
		/// The <see cref="IApiClient"/> for the <see cref="InstanceManagerClient"/>
		/// </summary>
		readonly IApiClient apiClient;

		/// <summary>
		/// Map of already created <see cref="IInstanceClient"/>s
		/// </summary>
		readonly Dictionary<long, IInstanceClient> cachedClients;

		/// <summary>
		/// Construct an <see cref="InstanceManagerClient"/>
		/// </summary>
		/// <param name="apiClient"></param>
		public InstanceManagerClient(IApiClient apiClient)
		{
			this.apiClient = apiClient;

			cachedClients = new Dictionary<long, IInstanceClient>();
		}

		/// <inheritdoc />
		public Task<Instance> Create(Instance instance, CancellationToken cancellationToken) => apiClient.Create<Instance, Instance>(Routes.InstanceManager, instance, cancellationToken);

		/// <inheritdoc />
		public Task Delete(Instance instance, CancellationToken cancellationToken) => apiClient.Delete(Routes.SetID(Routes.InstanceManager, instance.Id), cancellationToken);

		/// <inheritdoc />
		public Task<IReadOnlyList<Instance>> List(CancellationToken cancellationToken) => apiClient.Read<IReadOnlyList<Instance>>(Routes.List(Routes.InstanceManager), cancellationToken);

		/// <inheritdoc />
		public Task<Instance> Update(Instance instance, CancellationToken cancellationToken) => apiClient.Update<Instance, Instance>(Routes.InstanceManager, instance, cancellationToken);

		/// <inheritdoc />
		public IInstanceClient CreateClient(Instance instance)
		{
			if (!cachedClients.TryGetValue(instance.Id, out var client))
			{
				client = new InstanceClient(apiClient, instance);
				cachedClients.Add(instance.Id, client);
			}
			return client;
		}
	}
}