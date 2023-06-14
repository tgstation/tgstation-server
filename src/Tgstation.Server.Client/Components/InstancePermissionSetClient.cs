using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Tgstation.Server.Api;
using Tgstation.Server.Api.Models;
using Tgstation.Server.Api.Models.Internal;
using Tgstation.Server.Api.Models.Request;
using Tgstation.Server.Api.Models.Response;

namespace Tgstation.Server.Client.Components
{
	/// <inheritdoc />
	sealed class InstancePermissionSetClient : PaginatedClient, IInstancePermissionSetClient
	{
		/// <summary>
		/// The <see cref="Instance"/> for the <see cref="InstancePermissionSetClient"/>.
		/// </summary>
		readonly Instance instance;

		/// <summary>
		/// Initializes a new instance of the <see cref="InstancePermissionSetClient"/> class.
		/// </summary>
		/// <param name="apiClient">The <see cref="IApiClient"/> for the <see cref="PaginatedClient"/>.</param>
		/// <param name="instance">The value of <see cref="instance"/>.</param>
		public InstancePermissionSetClient(IApiClient apiClient, Instance instance)
			: base(apiClient)
		{
			this.instance = instance ?? throw new ArgumentNullException(nameof(instance));
		}

		/// <inheritdoc />
		public ValueTask<InstancePermissionSetResponse> Create(InstancePermissionSetRequest instancePermissionSet, CancellationToken cancellationToken) => ApiClient.Create<InstancePermissionSetRequest, InstancePermissionSetResponse>(Routes.InstancePermissionSet, instancePermissionSet ?? throw new ArgumentNullException(nameof(instancePermissionSet)), instance.Id!.Value, cancellationToken);

		/// <inheritdoc />
		public ValueTask Delete(InstancePermissionSet instancePermissionSet, CancellationToken cancellationToken) => ApiClient.Delete(
			Routes.SetID(
				Routes.InstancePermissionSet,
				instancePermissionSet.PermissionSetId),
			instance.Id!.Value,
			cancellationToken);

		/// <inheritdoc />
		public ValueTask<InstancePermissionSetResponse> Read(CancellationToken cancellationToken) => ApiClient.Read<InstancePermissionSetResponse>(Routes.InstancePermissionSet, instance.Id!.Value, cancellationToken);

		/// <inheritdoc />
		public ValueTask<InstancePermissionSetResponse> Update(InstancePermissionSetRequest instancePermissionSet, CancellationToken cancellationToken) => ApiClient.Update<InstancePermissionSetRequest, InstancePermissionSetResponse>(Routes.InstancePermissionSet, instancePermissionSet ?? throw new ArgumentNullException(nameof(instancePermissionSet)), instance.Id!.Value, cancellationToken);

		/// <inheritdoc />
		public ValueTask<List<InstancePermissionSetResponse>> List(PaginationSettings? paginationSettings, CancellationToken cancellationToken)
			=> ReadPaged<InstancePermissionSetResponse>(
				paginationSettings,
				Routes.ListRoute(Routes.InstancePermissionSet),
				instance.Id,
				cancellationToken);

		/// <inheritdoc />
		public ValueTask<InstancePermissionSetResponse> GetId(InstancePermissionSet instancePermissionSet, CancellationToken cancellationToken) => ApiClient.Read<InstancePermissionSetResponse>(Routes.SetID(Routes.InstancePermissionSet, instancePermissionSet?.PermissionSetId ?? throw new ArgumentNullException(nameof(instancePermissionSet))), instance.Id!.Value, cancellationToken);
	}
}
