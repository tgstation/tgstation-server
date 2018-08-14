using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Api;
using Tgstation.Server.Api.Models;

namespace Tgstation.Server.Client.Components
{
	/// <inheritdoc />
	sealed class InstanceUserClient : IInstanceUserClient
	{
		/// <summary>
		/// The <see cref="IApiClient"/> for the <see cref="InstanceUserClient"/>
		/// </summary>
		readonly IApiClient apiClient;
		/// <summary>
		/// The <see cref="Instance"/> for the <see cref="InstanceUserClient"/>
		/// </summary>
		readonly Instance instance;

		/// <summary>
		/// Construct an <see cref="InstanceUserClient"/>
		/// </summary>
		/// <param name="apiClient">The value of <see cref="apiClient"/></param>
		/// <param name="instance">The value of <see cref="instance"/></param>
		public InstanceUserClient(IApiClient apiClient, Instance instance)
		{
			this.apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
			this.instance = instance ?? throw new ArgumentNullException(nameof(instance)); 
		}

		/// <inheritdoc />
		public Task<InstanceUser> Create(InstanceUser user, CancellationToken cancellationToken) => apiClient.Create<InstanceUser, InstanceUser>(Routes.InstanceUser, user, instance.Id, cancellationToken);

		public Task Delete(InstanceUser instanceUser, CancellationToken cancellationToken) => apiClient.Delete(Routes.SetID(Routes.InstanceUser, instanceUser.UserId.Value), instance.Id, cancellationToken);

		/// <inheritdoc />
		public Task<InstanceUser> Read(CancellationToken cancellationToken) => apiClient.Read<InstanceUser>(Routes.InstanceUser, instance.Id, cancellationToken);

		/// <inheritdoc />
		public Task<InstanceUser> Update(InstanceUser user, CancellationToken cancellationToken) => apiClient.Update<InstanceUser, InstanceUser>(Routes.InstanceUser, user, instance.Id, cancellationToken);

		/// <inheritdoc />
		public Task<IReadOnlyList<InstanceUser>> List(CancellationToken cancellationToken) => apiClient.Read<IReadOnlyList<InstanceUser>>(Routes.List(Routes.InstanceUser), instance.Id, cancellationToken);
	}
}