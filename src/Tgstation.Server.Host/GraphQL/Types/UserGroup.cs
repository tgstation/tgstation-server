using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using HotChocolate;
using HotChocolate.CostAnalysis.Types;
using HotChocolate.Data;
using HotChocolate.Types;
using HotChocolate.Types.Relay;

using Tgstation.Server.Host.Authority;
using Tgstation.Server.Host.GraphQL.Transformers;

namespace Tgstation.Server.Host.GraphQL.Types
{
	/// <summary>
	/// Represents a group of <see cref="User"/>s.
	/// </summary>
	[Node]
	public sealed class UserGroup : NamedEntity
	{
		/// <summary>
		/// The <see cref="Types.PermissionSet"/> for the <see cref="UserGroup"/>.
		/// </summary>
		public required PermissionSet PermissionSet { get; set; }

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
			return userGroupAuthority.InvokeTransformableAllowMissing<Models.UserGroup, UserGroup, UserGroupTransformer>(
				authority => authority.GetId(id, false, cancellationToken));
		}

		/// <summary>
		/// Queries all registered <see cref="User"/>s in the <see cref="UserGroup"/>.
		/// </summary>
		/// <param name="userAuthority">The <see cref="IGraphQLAuthorityInvoker{TAuthority}"/> for the <see cref="IUserAuthority"/>.</param>
		/// <returns>A <see cref="IQueryable{T}"/> of all registered <see cref="User"/>s in the <see cref="UserGroup"/>.</returns>
		[UsePaging]
		[UseProjection]
		[UseFiltering]
		[UseSorting]
		[Cost(Costs.NonIndexedQueryable)]
		public async ValueTask<IQueryable<User>> QueryableUsers(
			[Service] IGraphQLAuthorityInvoker<IUserAuthority> userAuthority)
		{
			ArgumentNullException.ThrowIfNull(userAuthority);
			var dtoQueryable = await userAuthority.InvokeTransformableQueryable<Models.User, User, UserTransformer>(
				authority => authority.Queryable(),
				queryable => queryable.Where(user => user.GroupId == Id));
			return dtoQueryable;
		}
	}
}
