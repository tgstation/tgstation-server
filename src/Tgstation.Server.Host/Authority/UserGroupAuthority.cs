using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using GreenDonut;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using Tgstation.Server.Api.Rights;
using Tgstation.Server.Host.Authority.Core;
using Tgstation.Server.Host.Database;
using Tgstation.Server.Host.Models;
using Tgstation.Server.Host.Security;

namespace Tgstation.Server.Host.Authority
{
	/// <inheritdoc cref="IUserGroupAuthority" />
	sealed class UserGroupAuthority : AuthorityBase, IUserGroupAuthority
	{
		/// <summary>
		/// The <see cref="IUserGroupsDataLoader"/> for the <see cref="UserGroupAuthority"/>.
		/// </summary>
		readonly IUserGroupsDataLoader userGroupsDataLoader;

		/// <summary>
		/// Implements the <see cref="userGroupsDataLoader"/>.
		/// </summary>
		/// <param name="ids">The <see cref="IReadOnlyCollection{T}"/> of <see cref="UserGroup"/> <see cref="Api.Models.EntityId.Id"/>s to load.</param>
		/// <param name="databaseContext">The <see cref="IDatabaseContext"/> to load from.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in a <see cref="Dictionary{TKey, TValue}"/> of the requested <see cref="UserGroup"/>s.</returns>
		[DataLoader]
		public static Task<Dictionary<long, UserGroup>> GetUserGroups(
			IReadOnlyList<long> ids,
			IDatabaseContext databaseContext,
			CancellationToken cancellationToken)
		{
			ArgumentNullException.ThrowIfNull(ids);
			ArgumentNullException.ThrowIfNull(databaseContext);

			return databaseContext
				.Groups
				.Where(group => ids.Contains(group.Id!.Value))
				.ToDictionaryAsync(userGroup => userGroup.Id!.Value, cancellationToken);
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="UserGroupAuthority"/> class.
		/// </summary>
		/// <param name="authenticationContext">The <see cref="IAuthenticationContext"/> to use.</param>
		/// <param name="logger">The <see cref="ILogger"/> to use.</param>
		/// <param name="userGroupsDataLoader">The value of <see cref="userGroupsDataLoader"/>.</param>
		public UserGroupAuthority(
			IAuthenticationContext authenticationContext,
			ILogger<UserGroupAuthority> logger,
			IUserGroupsDataLoader userGroupsDataLoader)
			: base(authenticationContext, logger)
		{
			this.userGroupsDataLoader = userGroupsDataLoader ?? throw new ArgumentNullException(nameof(userGroupsDataLoader));
		}

		/// <inheritdoc />
		public async ValueTask<AuthorityResponse<UserGroup>> GetId(long id, CancellationToken cancellationToken)
		{
			if (id != AuthenticationContext.User.GroupId && !((AdministrationRights)AuthenticationContext.GetRight(RightsType.Administration)).HasFlag(AdministrationRights.ReadUsers))
				return Forbid<UserGroup>();

			var userGroup = await userGroupsDataLoader.LoadAsync(id, cancellationToken);
			if (userGroup == null)
				return NotFound<UserGroup>();

			return new AuthorityResponse<UserGroup>(userGroup);
		}
	}
}
