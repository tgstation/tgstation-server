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

namespace Tgstation.Server.Host.GraphQL.Types
{
	/// <summary>
	/// Represents a group of <see cref="User"/>s.
	/// </summary>
	[Node]
	public sealed class UserGroup : NamedEntity
	{
		/// <summary>
		/// Node resolver for <see cref="User"/>s.
		/// </summary>
		/// <param name="id">The <see cref="Entity.Id"/> to lookup.</param>
		/// <param name="userGroupAuthority">The <see cref="IGraphQLAuthorityInvoker{TAuthority}"/> for the <see cref="IUserGroupAuthority"/>.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask"/> resulting in the queried <see cref="User"/>, if present.</returns>
		public static ValueTask<UserGroup?> GetUserGroup(
			long id,
			[Service] IGraphQLAuthorityInvoker<IUserGroupAuthority> userGroupAuthority,
			CancellationToken cancellationToken)
		{
			ArgumentNullException.ThrowIfNull(userGroupAuthority);
			return userGroupAuthority.InvokeTransformableAllowMissing<Models.UserGroup, UserGroup, UserGroupGraphQLTransformer>(
				authority => authority.GetId(id, false, cancellationToken));
		}

		/// <summary>
		/// The <see cref="PermissionSet"/> owned by the <see cref="UserGroup"/>.
		/// </summary>
		/// <param name="permissionSetAuthority">The <see cref="IGraphQLAuthorityInvoker{TAuthority}"/> for the <see cref="IPermissionSetAuthority"/>.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in the <see cref="PermissionSet"/> owned by the <see cref="UserGroup"/>.</returns>
		public async ValueTask<PermissionSet> PermissionSet(
			[Service] IGraphQLAuthorityInvoker<IPermissionSetAuthority> permissionSetAuthority,
			CancellationToken cancellationToken)
		{
			ArgumentNullException.ThrowIfNull(permissionSetAuthority);

			return await permissionSetAuthority.InvokeTransformable<Models.PermissionSet, PermissionSet, PermissionSetGraphQLTransformer>(
				authority => authority.GetId(Id, PermissionSetLookupType.GroupId, cancellationToken));
		}

		/// <summary>
		/// Queries all registered <see cref="User"/>s in the <see cref="UserGroup"/>.
		/// </summary>
		/// <param name="userAuthority">The <see cref="IGraphQLAuthorityInvoker{TAuthority}"/> for the <see cref="IUserAuthority"/>.</param>
		/// <returns>A <see cref="IQueryable{T}"/> of all registered <see cref="User"/>s in the <see cref="UserGroup"/>.</returns>
		[UsePaging]
		[UseFiltering]
		[UseSorting]
		public async ValueTask<IQueryable<User>> QueryableUsersByGroup(
			[Service] IGraphQLAuthorityInvoker<IUserAuthority> userAuthority)
		{
			ArgumentNullException.ThrowIfNull(userAuthority);
			var dtoQueryable = await userAuthority.InvokeTransformableQueryable<Models.User, User, UserGraphQLTransformer>(
				authority => authority
					.Queryable(false),
				queryable => queryable.Where(user => user.GroupId == Id));
			return dtoQueryable;
		}
	}
}
