using System;
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
		/// <param name="apiClient">The value of <see cref="apiClient"/></param>
		public InstanceManagerClient(IApiClient apiClient)
		{
			this.apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));

			cachedClients = new Dictionary<long, IInstanceClient>();
		}

		/// <inheritdoc />
		public Task<Instance> CreateOrAttach(Instance instance, CancellationToken cancellationToken) => apiClient.Create<Instance, Instance>(Routes.InstanceManager, instance ?? throw new ArgumentNullException(nameof(instance)), cancellationToken);

		/// <inheritdoc />
		public Task Detach(Instance instance, CancellationToken cancellationToken) => apiClient.Delete(Routes.SetID(Routes.InstanceManager, instance?.Id ?? throw new ArgumentNullException(nameof(instance))), cancellationToken);

		/// <inheritdoc />
		public Task<IReadOnlyList<Instance>> List(CancellationToken cancellationToken) => apiClient.Read<IReadOnlyList<Instance>>(Routes.ListRoute(Routes.InstanceManager), cancellationToken);

		/// <inheritdoc />
		public Task<Instance> Update(Instance instance, CancellationToken cancellationToken) => apiClient.Update<Instance, Instance>(Routes.InstanceManager, instance ?? throw new ArgumentNullException(nameof(instance)), cancellationToken);

		/// <inheritdoc />
		public Task<Instance> GetId(Instance instance, CancellationToken cancellationToken) => apiClient.Read<Instance>(Routes.SetID(Routes.InstanceManager, instance?.Id ?? throw new ArgumentNullException(nameof(instance))), cancellationToken);

		/// <inheritdoc />
		public Task GrantPermissions(Instance instance, CancellationToken cancellationToken) => apiClient.Patch(Routes.SetID(Routes.InstanceManager, instance?.Id ?? throw new ArgumentNullException(nameof(instance))), cancellationToken);

		/// <inheritdoc />
		public IInstanceClient CreateClient(Instance instance)
		{
			if (!cachedClients.TryGetValue(instance?.Id ?? throw new ArgumentNullException(nameof(instance)), out var client))
			{
				client = new InstanceClient(apiClient, instance);
				cachedClients.Add(instance.Id, client);
			}

			return client;
		}
	}
}