using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using HotChocolate;
using HotChocolate.Data;
using HotChocolate.Types;
using HotChocolate.Types.Relay;

using Tgstation.Server.Host.Authority;
using Tgstation.Server.Host.Models.Transformers;
using Tgstation.Server.Host.Security;

namespace Tgstation.Server.Host.GraphQL.Types
{
	/// <summary>
	/// Wrapper for accessing <see cref="UserGroup"/>s.
	/// </summary>
	public sealed class UserGroups
	{
		/// <summary>
		/// Gets the current <see cref="User"/>.
		/// </summary>
		/// <param name="userGroupAuthority">The <see cref="IGraphQLAuthorityInvoker{TAuthority}"/> for the <see cref="IUserGroupAuthority"/>.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in the current <see cref="User"/>'s <see cref="UserGroup"/>.</returns>
		public ValueTask<UserGroup?> Current(
			[Service] IGraphQLAuthorityInvoker<IUserGroupAuthority> userGroupAuthority)
		{
			ArgumentNullException.ThrowIfNull(userGroupAuthority);
			return userGroupAuthority.InvokeTransformableAllowMissing<Models.UserGroup, UserGroup, UserGroupGraphQLTransformer>(authority => authority.Read());
		}

		/// <summary>
		/// Gets a <see cref="UserGroup"/> by <see cref="Entity.Id"/>.
		/// </summary>
		/// <param name="id">The <see cref="Entity.Id"/> of the <see cref="User"/>.</param>
		/// <param name="userGroupAuthority">The <see cref="IGraphQLAuthorityInvoker{TAuthority}"/> for the <see cref="IUserGroupAuthority"/>.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>The <see cref="UserGroup"/> represented by <paramref name="id"/>, if any.</returns>
		[TgsGraphQLAuthorize<IUserAuthority>(nameof(IUserGroupAuthority.GetId))]
		public ValueTask<UserGroup?> ById(
			[ID(nameof(UserGroup))] long id,
			[Service] IGraphQLAuthorityInvoker<IUserGroupAuthority> userGroupAuthority,
			CancellationToken cancellationToken)
			=> UserGroup.GetUserGroup(id, userGroupAuthority, cancellationToken);

		/// <summary>
		/// Queries all registered <see cref="UserGroup"/>s.
		/// </summary>
		/// <param name="userGroupAuthority">The <see cref="IGraphQLAuthorityInvoker{TAuthority}"/> for the <see cref="IUserGroupAuthority"/>.</param>
		/// <returns>A <see cref="IQueryable{T}"/> of all registered <see cref="UserGroup"/>s.</returns>
		[UsePaging]
		[UseFiltering]
		[UseSorting]
		[TgsGraphQLAuthorize<IUserGroupAuthority>(nameof(IUserGroupAuthority.Queryable))]
		public IQueryable<UserGroup> QueryableGroups(
			[Service] IGraphQLAuthorityInvoker<IUserGroupAuthority> userGroupAuthority)
		{
			ArgumentNullException.ThrowIfNull(userGroupAuthority);
			var dtoQueryable = userGroupAuthority.InvokeTransformableQueryable<Models.UserGroup, UserGroup, UserGroupGraphQLTransformer>(authority => authority.Queryable(false));
			return dtoQueryable;
		}

		/// <summary>
		/// Queries all registered <see cref="User"/>s in a <see cref="UserGroup"/> indicated by <paramref name="groupId"/>.
		/// </summary>
		/// <param name="groupId">The <see cref="UserGroup"/> <see cref="Entity.Id"/>.</param>
		/// <param name="userAuthority">The <see cref="IGraphQLAuthorityInvoker{TAuthority}"/> for the <see cref="IUserAuthority"/>.</param>
		/// <returns>A <see cref="IQueryable{T}"/> of all registered <see cref="User"/>s in the <see cref="UserGroup"/> indicated by <paramref name="groupId"/>.</returns>
		[UsePaging]
		[UseFiltering]
		[UseSorting]
		[TgsGraphQLAuthorize<IUserGroupAuthority>(nameof(IUserAuthority.Queryable))]
		public IQueryable<User> QueryableUsersByGroupId(
			[ID(nameof(UserGroup))]long groupId,
			[Service] IGraphQLAuthorityInvoker<IUserAuthority> userAuthority)
		{
			ArgumentNullException.ThrowIfNull(userAuthority);
			var dtoQueryable = userAuthority.InvokeTransformableQueryable<Models.User, User, UserGraphQLTransformer>(
				authority => authority
					.Queryable(false)
					.Where(user => user.GroupId == groupId));
			return dtoQueryable;
		}
	}
}
