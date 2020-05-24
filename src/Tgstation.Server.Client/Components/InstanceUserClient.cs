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
		public Task<InstanceUser> Create(InstanceUser instanceUser, CancellationToken cancellationToken) => apiClient.Create<InstanceUser, InstanceUser>(Routes.InstanceUser, instanceUser ?? throw new ArgumentNullException(nameof(instanceUser)), instance.Id, cancellationToken);

		/// <inheritdoc />
		public Task Delete(InstanceUser instanceUser, CancellationToken cancellationToken) => apiClient.Delete(
			Routes.SetID(
				Routes.InstanceUser,
				instanceUser.UserId ?? throw new ArgumentException("Missing instanceUser.UserId!", nameof(instanceUser))),
			instance.Id,
			cancellationToken);

		/// <inheritdoc />
		public Task<InstanceUser> Read(CancellationToken cancellationToken) => apiClient.Read<InstanceUser>(Routes.InstanceUser, instance.Id, cancellationToken);

		/// <inheritdoc />
		public Task<InstanceUser> Update(InstanceUser instanceUser, CancellationToken cancellationToken) => apiClient.Update<InstanceUser, InstanceUser>(Routes.InstanceUser, instanceUser ?? throw new ArgumentNullException(nameof(instanceUser)), instance.Id, cancellationToken);

		/// <inheritdoc />
		public Task<IReadOnlyList<InstanceUser>> List(CancellationToken cancellationToken) => apiClient.Read<IReadOnlyList<InstanceUser>>(Routes.ListRoute(Routes.InstanceUser), instance.Id, cancellationToken);

		/// <inheritdoc />
		public Task<InstanceUser> GetId(InstanceUser instanceUser, CancellationToken cancellationToken) => apiClient.Read<InstanceUser>(Routes.SetID(Routes.InstanceUser, instanceUser?.UserId ?? throw new ArgumentNullException(nameof(instanceUser))), instance.Id, cancellationToken);
	}
}