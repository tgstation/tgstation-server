using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using GreenDonut;
using GreenDonut.Data;

using HotChocolate;
using HotChocolate.CostAnalysis.Types;
using HotChocolate.Data;
using HotChocolate.Types;
using HotChocolate.Types.Relay;

using Tgstation.Server.Host.Authority;
using Tgstation.Server.Host.Authority.Core;
using Tgstation.Server.Host.Extensions;
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
		/// Implements the <see cref="IUserGroupsDataLoader"/>.
		/// </summary>
		/// <param name="ids">The <see cref="IReadOnlyList{T}"/> of <see cref="UserGroup"/> <see cref="Api.Models.EntityId.Id"/>s to load paired with if the system user should be allowed.</param>
		/// <param name="userAuthority">The <see cref="IGraphQLAuthorityInvoker{TAuthority}"/> for the <see cref="IUserAuthority"/>.</param>
		/// <param name="queryContext">The <see cref="QueryContext{TEntity}"/> for <see cref="User"/> mapped to an <see cref="AuthorityResponse{TResult}"/>.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in a <see cref="Dictionary{TKey, TValue}"/> of the requested <see cref="UserGroup"/> <see cref="AuthorityResponse{TResult}"/>s.</returns>
		[DataLoader(AccessModifier = DataLoaderAccessModifier.PublicInterface)]
		public static ValueTask<Dictionary<long, AuthorityResponse<UserGroup>>> GetUserGroups(
			IReadOnlyList<long> ids,
			IGraphQLAuthorityInvoker<IUserGroupAuthority> userAuthority,
			QueryContext<AuthorityResponse<UserGroup>>? queryContext,
			CancellationToken cancellationToken)
		{
			ArgumentNullException.ThrowIfNull(ids);
			ArgumentNullException.ThrowIfNull(userAuthority);

			return userAuthority.ExecuteDataLoader<Models.UserGroup, UserGroup, UserGroupTransformer>(
				(authority, id) => authority.GetId<UserGroup>(id, cancellationToken),
				ids,
				queryContext);
		}

		/// <summary>
		/// Node resolver for <see cref="User"/>s.
		/// </summary>
		/// <param name="id">The <see cref="Entity.Id"/> to lookup.</param>
		/// <param name="userGroupsDataLoader">The <see cref="IUserGroupsDataLoader"/> to use.</param>
		/// <param name="queryContext">The <see cref="QueryContext{TEntity}"/> for the operation.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask"/> resulting in the queried <see cref="User"/>, if present.</returns>
		public static ValueTask<UserGroup?> GetUserGroup(
			long id,
			[Service] IUserGroupsDataLoader userGroupsDataLoader,
			QueryContext<UserGroup>? queryContext,
			CancellationToken cancellationToken)
		{
			ArgumentNullException.ThrowIfNull(userGroupsDataLoader);
			return userGroupsDataLoader.LoadAuthorityResponse(queryContext, id, cancellationToken);
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
		public async ValueTask<IQueryable<User>> Users(
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
