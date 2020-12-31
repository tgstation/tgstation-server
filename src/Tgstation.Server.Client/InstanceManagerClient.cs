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
	sealed class InstanceManagerClient : PaginatedClient, IInstanceManagerClient
	{
		/// <summary>
		/// Construct an <see cref="InstanceManagerClient"/>
		/// </summary>
		/// <param name="apiClient">The <see cref="IApiClient"/> for the <see cref="PaginatedClient"/>.</param>
		public InstanceManagerClient(IApiClient apiClient)
			: base(apiClient)
		{ }

		/// <inheritdoc />
		public Task<Instance> CreateOrAttach(Instance instance, CancellationToken cancellationToken) => ApiClient.Create<Instance, Instance>(Routes.InstanceManager, instance ?? throw new ArgumentNullException(nameof(instance)), cancellationToken);

		/// <inheritdoc />
		public Task Detach(Instance instance, CancellationToken cancellationToken) => ApiClient.Delete(Routes.SetID(Routes.InstanceManager, instance?.Id ?? throw new ArgumentNullException(nameof(instance))), cancellationToken);

		/// <inheritdoc />
		public Task<IReadOnlyList<Instance>> List(PaginationSettings? paginationSettings, CancellationToken cancellationToken)
			=> ReadPaged<Instance>(paginationSettings, Routes.ListRoute(Routes.InstanceManager), null, cancellationToken);

		/// <inheritdoc />
		public Task<Instance> Update(Instance instance, CancellationToken cancellationToken) => ApiClient.Update<Instance, Instance>(Routes.InstanceManager, instance ?? throw new ArgumentNullException(nameof(instance)), cancellationToken);

		/// <inheritdoc />
		public Task<Instance> GetId(Instance instance, CancellationToken cancellationToken) => ApiClient.Read<Instance>(Routes.SetID(Routes.InstanceManager, instance?.Id ?? throw new ArgumentNullException(nameof(instance))), cancellationToken);

		/// <inheritdoc />
		public Task GrantPermissions(Instance instance, CancellationToken cancellationToken) => ApiClient.Patch(Routes.SetID(Routes.InstanceManager, instance?.Id ?? throw new ArgumentNullException(nameof(instance))), cancellationToken);

		/// <inheritdoc />
		public IInstanceClient CreateClient(Instance instance) => new InstanceClient(ApiClient, instance);
	}
}
