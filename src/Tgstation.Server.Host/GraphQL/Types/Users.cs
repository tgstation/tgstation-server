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

#pragma warning disable CA1724 // conflict with GitLabApiClient.Models.Users. They can fuck off

namespace Tgstation.Server.Host.GraphQL.Types
{
	/// <summary>
	/// Wrapper for accessing <see cref="User"/>s.
	/// </summary>
	public sealed class Users
	{
		/// <summary>
		/// Gets the swarm's <see cref="UserGroups"/>.
		/// </summary>
		/// <returns>A new <see cref="UserGroups"/>.</returns>
		public UserGroups Groups() => new();

		/// <summary>
		/// Gets the current <see cref="User"/>.
		/// </summary>
		/// <param name="userAuthority">The <see cref="IGraphQLAuthorityInvoker{TAuthority}"/> <see cref="IUserAuthority"/>.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in the current <see cref="User"/>.</returns>
		[TgsGraphQLAuthorize<IUserAuthority>(nameof(IUserAuthority.Read))]
		public ValueTask<User> Current(
			[Service] IGraphQLAuthorityInvoker<IUserAuthority> userAuthority,
			CancellationToken cancellationToken)
		{
			ArgumentNullException.ThrowIfNull(userAuthority);
			return userAuthority.InvokeTransformable<Models.User, User, UserGraphQLTransformer>(authority => authority.Read(cancellationToken));
		}

		/// <summary>
		/// Gets a <see cref="User"/> by <see cref="Entity.Id"/>.
		/// </summary>
		/// <param name="id">The <see cref="Entity.Id"/> of the <see cref="User"/>.</param>
		/// <param name="userAuthority">The <see cref="IGraphQLAuthorityInvoker{TAuthority}"/> <see cref="IUserAuthority"/>.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>The <see cref="User"/> represented by <paramref name="id"/>, if any.</returns>
		[Error(typeof(ErrorMessageException))]
		[TgsGraphQLAuthorize<IUserAuthority>(nameof(IUserAuthority.GetId))]
		public ValueTask<User?> ById(
			[ID(nameof(User))] long id,
			[Service] IGraphQLAuthorityInvoker<IUserAuthority> userAuthority,
			CancellationToken cancellationToken)
			=> User.GetUser(id, userAuthority, cancellationToken);

		/// <summary>
		/// Queries all registered <see cref="User"/>s.
		/// </summary>
		/// <param name="userAuthority">The <see cref="IGraphQLAuthorityInvoker{TAuthority}"/> <see cref="IUserAuthority"/>.</param>
		/// <returns>A <see cref="IQueryable{T}"/> of all registered <see cref="User"/>s.</returns>
		[UsePaging]
		[UseFiltering]
		[UseSorting]
		[TgsGraphQLAuthorize<IUserAuthority>(nameof(IUserAuthority.Queryable))]
		public IQueryable<User> QueryableUsers(
			[Service] IGraphQLAuthorityInvoker<IUserAuthority> userAuthority)
		{
			ArgumentNullException.ThrowIfNull(userAuthority);
			var dtoQueryable = userAuthority.InvokeTransformableQueryable<Models.User, User, UserGraphQLTransformer>(authority => authority.Queryable(false));
			return dtoQueryable;
		}
	}
}
