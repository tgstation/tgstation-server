using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Api;
using Tgstation.Server.Api.Models;

namespace Tgstation.Server.Client
{
	/// <inheritdoc />
	sealed class UserGroupsClient : PaginatedClient, IUserGroupsClient
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="UserGroupsClient"/> <see langword="class"/>.
		/// </summary>
		/// <param name="apiClient">The <see cref="IApiClient"/> for the <see cref="PaginatedClient"/>.</param>
		public UserGroupsClient(IApiClient apiClient)
			: base(apiClient)
		{
		}

		/// <inheritdoc />
		public Task<UserGroup> Create(UserGroup group, CancellationToken cancellationToken) => ApiClient.Create<UserGroup, UserGroup>(Routes.UserGroup, group ?? throw new ArgumentNullException(nameof(group)), cancellationToken);

		/// <inheritdoc />
		public Task<UserGroup> GetId(EntityId group, CancellationToken cancellationToken) => ApiClient.Read<UserGroup>(Routes.SetID(Routes.UserGroup, group?.Id ?? throw new ArgumentNullException(nameof(group))), cancellationToken);

		/// <inheritdoc />
		public Task<IReadOnlyList<UserGroup>> List(PaginationSettings? paginationSettings, CancellationToken cancellationToken)
			=> ReadPaged<UserGroup>(paginationSettings, Routes.ListRoute(Routes.UserGroup), null, cancellationToken);

		/// <inheritdoc />
		public Task<UserGroup> Update(UserGroup group, CancellationToken cancellationToken) => ApiClient.Update<UserGroup, UserGroup>(Routes.UserGroup, group ?? throw new ArgumentNullException(nameof(group)), cancellationToken);

		/// <inheritdoc />
		public Task Delete(EntityId group, CancellationToken cancellationToken) => ApiClient.Delete(Routes.SetID(Routes.UserGroup, group?.Id ?? throw new ArgumentNullException(nameof(group))), cancellationToken);
	}
}
