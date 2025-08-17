using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using GreenDonut.Data;

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
	/// Wrapper for accessing <see cref="UserGroup"/>s.
	/// </summary>
	public sealed class UserGroupsRepository
	{
		/// <summary>
		/// Gets the current <see cref="User"/>.
		/// </summary>
		/// <param name="userGroupAuthority">The <see cref="IGraphQLAuthorityInvoker{TAuthority}"/> for the <see cref="IUserGroupAuthority"/>.</param>
		/// <param name="queryContext">The <see cref="QueryContext{TEntity}"/> for the operation.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in the current <see cref="User"/>'s <see cref="UserGroup"/>.</returns>
		public ValueTask<UserGroup?> Current(
			[Service] IGraphQLAuthorityInvoker<IUserGroupAuthority> userGroupAuthority,
			QueryContext<UserGroup>? queryContext,
			CancellationToken cancellationToken)
		{
			ArgumentNullException.ThrowIfNull(userGroupAuthority);
			return userGroupAuthority.InvokeTransformableAllowMissing<Models.UserGroup, UserGroup, UserGroupTransformer>(
				authority => authority.Read<UserGroup>(cancellationToken),
				queryContext);
		}

		/// <summary>
		/// Gets a <see cref="UserGroup"/> by <see cref="Entity.Id"/>.
		/// </summary>
		/// <param name="id">The <see cref="Entity.Id"/> of the <see cref="User"/>.</param>
		/// <param name="userGroupAuthority">The <see cref="IGraphQLAuthorityInvoker{TAuthority}"/> for the <see cref="IUserGroupAuthority"/>.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>The <see cref="UserGroup"/> represented by <paramref name="id"/>, if any.</returns>
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
		[UseProjection]
		[UseFiltering]
		[UseSorting]
		[Cost(Costs.NonIndexedQueryable)]
		public async ValueTask<IQueryable<UserGroup>> QueryableGroups(
			[Service] IGraphQLAuthorityInvoker<IUserGroupAuthority> userGroupAuthority)
		{
			ArgumentNullException.ThrowIfNull(userGroupAuthority);
			var dtoQueryable = await userGroupAuthority.InvokeTransformableQueryable<Models.UserGroup, UserGroup, UserGroupTransformer>(
				authority => authority.Queryable(true));
			return dtoQueryable;
		}

		/// <summary>
		/// Queries all registered <see cref="User"/>s in a <see cref="UserGroup"/> indicated by <paramref name="groupId"/>.
		/// </summary>
		/// <param name="groupId">The <see cref="UserGroup"/> <see cref="Entity.Id"/>.</param>
		/// <param name="userAuthority">The <see cref="IGraphQLAuthorityInvoker{TAuthority}"/> for the <see cref="IUserAuthority"/>.</param>
		/// <returns>A <see cref="IQueryable{T}"/> of all registered <see cref="User"/>s in the <see cref="UserGroup"/> indicated by <paramref name="groupId"/>.</returns>
		[UsePaging]
		[UseProjection]
		[UseFiltering]
		[UseSorting]
		public async ValueTask<IQueryable<User>> UsersByGroupId(
			[ID(nameof(UserGroup))] long groupId,
			[Service] IGraphQLAuthorityInvoker<IUserAuthority> userAuthority)
		{
			ArgumentNullException.ThrowIfNull(userAuthority);
			var dtoQueryable = await userAuthority.InvokeTransformableQueryable<Models.User, User, UserTransformer>(
				authority => authority
					.Queryable(),
				queryable => queryable.Where(user => user.GroupId == groupId));
			return dtoQueryable;
		}
	}
}
