using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Tgstation.Server.Api;
using Tgstation.Server.Api.Models;
using Tgstation.Server.Api.Models.Request;
using Tgstation.Server.Api.Models.Response;

namespace Tgstation.Server.Client
{
	/// <inheritdoc cref="IUserGroupsClient" />
	sealed class UserGroupsClient : PaginatedClient, IUserGroupsClient
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="UserGroupsClient"/> class.
		/// </summary>
		/// <param name="apiClient">The <see cref="IApiClient"/> for the <see cref="PaginatedClient"/>.</param>
		public UserGroupsClient(IApiClient apiClient)
			: base(apiClient)
		{
		}

		/// <inheritdoc />
		public ValueTask<UserGroupResponse> Create(UserGroupCreateRequest group, CancellationToken cancellationToken) => ApiClient.Create<UserGroupCreateRequest, UserGroupResponse>(Routes.UserGroup, group ?? throw new ArgumentNullException(nameof(group)), cancellationToken);

		/// <inheritdoc />
		public ValueTask<UserGroupResponse> GetId(EntityId group, CancellationToken cancellationToken) => ApiClient.Read<UserGroupResponse>(Routes.SetID(Routes.UserGroup, group?.Id ?? throw new ArgumentNullException(nameof(group))), cancellationToken);

		/// <inheritdoc />
		public ValueTask<List<UserGroupResponse>> List(PaginationSettings? paginationSettings, CancellationToken cancellationToken)
			=> ReadPaged<UserGroupResponse>(paginationSettings, Routes.ListRoute(Routes.UserGroup), null, cancellationToken);

		/// <inheritdoc />
		public ValueTask<UserGroupResponse> Update(UserGroupUpdateRequest group, CancellationToken cancellationToken) => ApiClient.Update<UserGroupUpdateRequest, UserGroupResponse>(Routes.UserGroup, group ?? throw new ArgumentNullException(nameof(group)), cancellationToken);

		/// <inheritdoc />
		public ValueTask Delete(EntityId group, CancellationToken cancellationToken) => ApiClient.Delete(Routes.SetID(Routes.UserGroup, group?.Id ?? throw new ArgumentNullException(nameof(group))), cancellationToken);
	}
}
