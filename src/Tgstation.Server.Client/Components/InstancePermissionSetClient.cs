using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Api;
using Tgstation.Server.Api.Models;

namespace Tgstation.Server.Client.Components
{
	/// <inheritdoc />
	sealed class InstancePermissionSetClient : IInstancePermissionSetClient
	{
		/// <summary>
		/// The <see cref="IApiClient"/> for the <see cref="InstancePermissionSetClient"/>
		/// </summary>
		readonly IApiClient apiClient;

		/// <summary>
		/// The <see cref="Instance"/> for the <see cref="InstancePermissionSetClient"/>
		/// </summary>
		readonly Instance instance;

		/// <summary>
		/// Construct an <see cref="InstancePermissionSetClient"/>
		/// </summary>
		/// <param name="apiClient">The value of <see cref="apiClient"/></param>
		/// <param name="instance">The value of <see cref="instance"/></param>
		public InstancePermissionSetClient(IApiClient apiClient, Instance instance)
		{
			this.apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
			this.instance = instance ?? throw new ArgumentNullException(nameof(instance));
		}

		/// <inheritdoc />
		public Task<InstancePermissionSet> Create(InstancePermissionSet instancePermissionSet, CancellationToken cancellationToken) => apiClient.Create<InstancePermissionSet, InstancePermissionSet>(Routes.InstancePermissionSet, instancePermissionSet ?? throw new ArgumentNullException(nameof(instancePermissionSet)), instance.Id, cancellationToken);

		/// <inheritdoc />
		public Task Delete(InstancePermissionSet instancePermissionSet, CancellationToken cancellationToken) => apiClient.Delete(
			Routes.SetID(
				Routes.InstancePermissionSet,
				instancePermissionSet.PermissionSetId),
			instance.Id,
			cancellationToken);

		/// <inheritdoc />
		public Task<InstancePermissionSet> Read(CancellationToken cancellationToken) => apiClient.Read<InstancePermissionSet>(Routes.InstancePermissionSet, instance.Id, cancellationToken);

		/// <inheritdoc />
		public Task<InstancePermissionSet> Update(InstancePermissionSet instancePermissionSet, CancellationToken cancellationToken) => apiClient.Update<InstancePermissionSet, InstancePermissionSet>(Routes.InstancePermissionSet, instancePermissionSet ?? throw new ArgumentNullException(nameof(instancePermissionSet)), instance.Id, cancellationToken);

		/// <inheritdoc />
		public Task<IReadOnlyList<InstancePermissionSet>> List(CancellationToken cancellationToken) => apiClient.Read<IReadOnlyList<InstancePermissionSet>>(Routes.ListRoute(Routes.InstancePermissionSet), instance.Id, cancellationToken);

		/// <inheritdoc />
		public Task<InstancePermissionSet> GetId(InstancePermissionSet instancePermissionSet, CancellationToken cancellationToken) => apiClient.Read<InstancePermissionSet>(Routes.SetID(Routes.InstancePermissionSet, instancePermissionSet?.PermissionSetId ?? throw new ArgumentNullException(nameof(instancePermissionSet))), instance.Id, cancellationToken);
	}
}
