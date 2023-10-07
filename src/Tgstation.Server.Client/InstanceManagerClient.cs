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
	/// <inheritdoc cref="IInstanceManagerClient" />
	sealed class InstanceManagerClient : PaginatedClient, IInstanceManagerClient
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="InstanceManagerClient"/> class.
		/// </summary>
		/// <param name="apiClient">The <see cref="IApiClient"/> for the <see cref="PaginatedClient"/>.</param>
		public InstanceManagerClient(IApiClient apiClient)
			: base(apiClient)
		{
		}

		/// <inheritdoc />
		public ValueTask<InstanceResponse> CreateOrAttach(InstanceCreateRequest instance, CancellationToken cancellationToken) => ApiClient.Create<InstanceCreateRequest, InstanceResponse>(Routes.InstanceManager, instance ?? throw new ArgumentNullException(nameof(instance)), cancellationToken);

		/// <inheritdoc />
		public ValueTask Detach(EntityId instance, CancellationToken cancellationToken) => ApiClient.Delete(Routes.SetID(Routes.InstanceManager, instance?.Id ?? throw new ArgumentNullException(nameof(instance))), cancellationToken);

		/// <inheritdoc />
		public ValueTask<List<InstanceResponse>> List(PaginationSettings? paginationSettings, CancellationToken cancellationToken)
			=> ReadPaged<InstanceResponse>(paginationSettings, Routes.ListRoute(Routes.InstanceManager), null, cancellationToken);

		/// <inheritdoc />
		public ValueTask<InstanceResponse> Update(InstanceUpdateRequest instance, CancellationToken cancellationToken) => ApiClient.Update<InstanceUpdateRequest, InstanceResponse>(Routes.InstanceManager, instance ?? throw new ArgumentNullException(nameof(instance)), cancellationToken);

		/// <inheritdoc />
		public ValueTask<InstanceResponse> GetId(EntityId instance, CancellationToken cancellationToken) => ApiClient.Read<InstanceResponse>(Routes.SetID(Routes.InstanceManager, instance?.Id ?? throw new ArgumentNullException(nameof(instance))), cancellationToken);

		/// <inheritdoc />
		public ValueTask GrantPermissions(EntityId instance, CancellationToken cancellationToken) => ApiClient.Patch(Routes.SetID(Routes.InstanceManager, instance?.Id ?? throw new ArgumentNullException(nameof(instance))), cancellationToken);

		/// <inheritdoc />
		public IInstanceClient CreateClient(Instance instance)
		{
			if (instance == null)
				throw new ArgumentNullException(nameof(instance));

			if (!instance.Id.HasValue)
				throw new ArgumentException("Instance missing Id!", nameof(instance));

			return new InstanceClient(
				ApiClient,
				instance);
		}
	}
}
