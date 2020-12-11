using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Api;
using Tgstation.Server.Api.Models;

namespace Tgstation.Server.Client
{
	/// <inheritdoc />
	sealed class UserGroupsClient : IUserGroupsClient
	{
		/// <summary>
		/// The <see cref="apiClient"/> for the <see cref="UserGroupsClient"/>.
		/// </summary>
		readonly IApiClient apiClient;

		/// <summary>
		/// Initializes a new instance of the <see cref="UserGroupsClient"/> <see langword="class"/>.
		/// </summary>
		/// <param name="apiClient">The value of <see cref="apiClient"/>.</param>
		public UserGroupsClient(IApiClient apiClient)
		{
			this.apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
		}

		/// <inheritdoc />
		public Task<UserGroup> Create(UserGroup group, CancellationToken cancellationToken) => apiClient.Create<UserGroup, UserGroup>(Routes.UserGroup, group ?? throw new ArgumentNullException(nameof(group)), cancellationToken);

		/// <inheritdoc />
		public Task<UserGroup> GetId(EntityId group, CancellationToken cancellationToken) => apiClient.Read<UserGroup>(Routes.SetID(Routes.UserGroup, group?.Id ?? throw new ArgumentNullException(nameof(group))), cancellationToken);

		/// <inheritdoc />
		public Task<IReadOnlyList<UserGroup>> List(CancellationToken cancellationToken) => apiClient.Read<IReadOnlyList<UserGroup>>(Routes.ListRoute(Routes.UserGroup), cancellationToken);

		/// <inheritdoc />
		public Task<UserGroup> Update(UserGroup group, CancellationToken cancellationToken) => apiClient.Update<UserGroup, UserGroup>(Routes.UserGroup, group ?? throw new ArgumentNullException(nameof(group)), cancellationToken);

		/// <inheritdoc />
		public Task Delete(EntityId group, CancellationToken cancellationToken) => apiClient.Delete(Routes.SetID(Routes.UserGroup, group?.Id ?? throw new ArgumentNullException(nameof(group))), cancellationToken);
	}
}
