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
	/// <inheritdoc cref="Tgstation.Server.Client.IUsersClient" />
	sealed class UsersClient : PaginatedClient, IUsersClient
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="UsersClient"/> class.
		/// </summary>
		/// <param name="apiClient">The <see cref="IApiClient"/> for the <see cref="PaginatedClient"/>.</param>
		public UsersClient(IApiClient apiClient)
			: base(apiClient)
		{
		}

		/// <inheritdoc />
		public Task<UserResponse> Create(UserCreateRequest user, CancellationToken cancellationToken) => ApiClient.Create<UserCreateRequest, UserResponse>(Routes.User, user ?? throw new ArgumentNullException(nameof(user)), cancellationToken);

		/// <inheritdoc />
		public Task<UserResponse> GetId(EntityId user, CancellationToken cancellationToken) => ApiClient.Read<UserResponse>(Routes.SetID(Routes.User, user?.Id ?? throw new ArgumentNullException(nameof(user))), cancellationToken);

		/// <inheritdoc />
		public Task<IReadOnlyList<UserResponse>> List(PaginationSettings? paginationSettings, CancellationToken cancellationToken)
			=> ReadPaged<UserResponse>(paginationSettings, Routes.ListRoute(Routes.User), null, cancellationToken);

		/// <inheritdoc />
		public Task<UserResponse> Read(CancellationToken cancellationToken) => ApiClient.Read<UserResponse>(Routes.User, cancellationToken);

		/// <inheritdoc />
		public Task<UserResponse> Update(UserUpdateRequest user, CancellationToken cancellationToken) => ApiClient.Update<UserUpdateRequest, UserResponse>(Routes.User, user ?? throw new ArgumentNullException(nameof(user)), cancellationToken);
	}
}
