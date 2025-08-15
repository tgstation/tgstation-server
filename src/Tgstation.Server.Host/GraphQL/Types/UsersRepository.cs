using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using GreenDonut.Data;

using HotChocolate;
using HotChocolate.Data;
using HotChocolate.Types;
using HotChocolate.Types.Relay;

using Microsoft.Extensions.Options;

using Tgstation.Server.Host.Authority;
using Tgstation.Server.Host.Configuration;
using Tgstation.Server.Host.Models.Transformers;

namespace Tgstation.Server.Host.GraphQL.Types
{
	/// <summary>
	/// Wrapper for accessing <see cref="User"/>s.
	/// </summary>
	public sealed class UsersRepository
	{
		/// <summary>
		/// If only OIDC logins and registration is allowed.
		/// </summary>
		/// <param name="securityConfigurationOptions">The <see cref="IOptions{TOptions}"/> containing the <see cref="SecurityConfiguration"/>.</param>
		/// <returns><see langword="true"/> if OIDC strict mode is enabled, <see langword="false"/> otherwise.</returns>
		public bool OidcStrictMode(
			[Service] IOptions<SecurityConfiguration> securityConfigurationOptions)
		{
			ArgumentNullException.ThrowIfNull(securityConfigurationOptions);
			return securityConfigurationOptions.Value.OidcStrictMode;
		}

		/// <summary>
		/// Gets the current <see cref="User"/>.
		/// </summary>
		/// <param name="userAuthority">The <see cref="IGraphQLAuthorityInvoker{TAuthority}"/> for the <see cref="IUserAuthority"/>.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in the current <see cref="User"/>.</returns>
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
		/// <param name="usersDataLoader">The <see cref="IUsersDataLoader"/> to use.</param>
		/// <param name="queryContext">The active <see cref="QueryContext{TEntity}"/>.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>The <see cref="User"/> represented by <paramref name="id"/>, if any.</returns>
		[Error(typeof(ErrorMessageException))]
		public ValueTask<User?> ById(
			[ID(nameof(User))] long id,
			[Service] IUsersDataLoader usersDataLoader,
			QueryContext<User>? queryContext,
			CancellationToken cancellationToken)
			=> User.GetUser(id, usersDataLoader, queryContext, cancellationToken);

		/// <summary>
		/// Queries all registered <see cref="User"/>s.
		/// </summary>
		/// <param name="userAuthority">The <see cref="IGraphQLAuthorityInvoker{TAuthority}"/> for the <see cref="IUserAuthority"/>.</param>
		/// <returns>A <see cref="IQueryable{T}"/> of all registered <see cref="User"/>s.</returns>
		[UsePaging]
		[UseFiltering]
		[UseSorting]
		public async ValueTask<IQueryable<User>> QueryableUsers(
			[Service] IGraphQLAuthorityInvoker<IUserAuthority> userAuthority)
		{
			ArgumentNullException.ThrowIfNull(userAuthority);
			var dtoQueryable = await userAuthority.InvokeTransformableQueryable<Models.User, User, UserGraphQLTransformer>(
				authority => authority.Queryable(false));
			return dtoQueryable;
		}
	}
}
