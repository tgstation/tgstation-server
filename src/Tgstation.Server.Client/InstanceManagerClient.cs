using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Api;
using Tgstation.Server.Api.Models;
using Tgstation.Server.Api.Models.Request;
using Tgstation.Server.Api.Models.Response;
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
		public Task<InstanceResponse> CreateOrAttach(InstanceCreateRequest instance, CancellationToken cancellationToken) => ApiClient.Create<InstanceCreateRequest, InstanceResponse>(Routes.InstanceManager, instance ?? throw new ArgumentNullException(nameof(instance)), cancellationToken);

		/// <inheritdoc />
		public Task Detach(EntityId instance, CancellationToken cancellationToken) => ApiClient.Delete(Routes.SetID(Routes.InstanceManager, instance?.Id ?? throw new ArgumentNullException(nameof(instance))), cancellationToken);

		/// <inheritdoc />
		public Task<IReadOnlyList<InstanceResponse>> List(PaginationSettings? paginationSettings, CancellationToken cancellationToken)
			=> ReadPaged<InstanceResponse>(paginationSettings, Routes.ListRoute(Routes.InstanceManager), null, cancellationToken);

		/// <inheritdoc />
		public Task<InstanceResponse> Update(InstanceUpdateRequest instance, CancellationToken cancellationToken) => ApiClient.Update<InstanceUpdateRequest, InstanceResponse>(Routes.InstanceManager, instance ?? throw new ArgumentNullException(nameof(instance)), cancellationToken);

		/// <inheritdoc />
		public Task<InstanceResponse> GetId(EntityId instance, CancellationToken cancellationToken) => ApiClient.Read<InstanceResponse>(Routes.SetID(Routes.InstanceManager, instance?.Id ?? throw new ArgumentNullException(nameof(instance))), cancellationToken);

		/// <inheritdoc />
		public Task GrantPermissions(EntityId instance, CancellationToken cancellationToken) => ApiClient.Patch(Routes.SetID(Routes.InstanceManager, instance?.Id ?? throw new ArgumentNullException(nameof(instance))), cancellationToken);

		/// <inheritdoc />
		public IInstanceClient CreateClient(EntityId instance) => new InstanceClient(
			ApiClient,
			new InstanceResponse
			{
				Id = instance?.Id ?? throw new ArgumentNullException(nameof(instance))
			});
	}
}
