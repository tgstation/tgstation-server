using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Api;
using Tgstation.Server.Api.Models;

namespace Tgstation.Server.Client
{
	/// <inheritdoc />
	sealed class UsersClient : PaginatedClient, IUsersClient
	{
		/// <summary>
		/// Construct an <see cref="UsersClient"/>
		/// </summary>
		/// <param name="apiClient">The <see cref="IApiClient"/> for the <see cref="PaginatedClient"/>.</param>
		public UsersClient(IApiClient apiClient)
			: base(apiClient)
		{
		}

		/// <inheritdoc />
		public Task<User> Create(UserUpdate user, CancellationToken cancellationToken) => ApiClient.Create<UserUpdate, User>(Routes.User, user ?? throw new ArgumentNullException(nameof(user)), cancellationToken);

		/// <inheritdoc />
		public Task<User> GetId(Api.Models.Internal.User user, CancellationToken cancellationToken) => ApiClient.Read<User>(Routes.SetID(Routes.User, user?.Id ?? throw new ArgumentNullException(nameof(user))), cancellationToken);

		/// <inheritdoc />
		public Task<IReadOnlyList<User>> List(PaginationSettings? paginationSettings, CancellationToken cancellationToken)
			=> ReadPaged<User>(paginationSettings, Routes.ListRoute(Routes.User), null, cancellationToken);

		/// <inheritdoc />
		public Task<User> Read(CancellationToken cancellationToken) => ApiClient.Read<User>(Routes.User, cancellationToken);

		/// <inheritdoc />
		public Task<User> Update(UserUpdate user, CancellationToken cancellationToken) => ApiClient.Update<UserUpdate, User>(Routes.User, user ?? throw new ArgumentNullException(nameof(user)), cancellationToken);
	}
}
