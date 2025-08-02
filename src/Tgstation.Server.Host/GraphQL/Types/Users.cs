using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using HotChocolate;
using HotChocolate.Data;
using HotChocolate.Types;
using HotChocolate.Types.Relay;

using Microsoft.Extensions.Options;

using Tgstation.Server.Host.Authority;
using Tgstation.Server.Host.Configuration;
using Tgstation.Server.Host.Models.Transformers;

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
		/// <param name="userAuthority">The <see cref="IGraphQLAuthorityInvoker{TAuthority}"/> for the <see cref="IUserAuthority"/>.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>The <see cref="User"/> represented by <paramref name="id"/>, if any.</returns>
		[Error(typeof(ErrorMessageException))]
		public ValueTask<User?> ById(
			[ID(nameof(User))] long id,
			[Service] IGraphQLAuthorityInvoker<IUserAuthority> userAuthority,
			CancellationToken cancellationToken)
			=> User.GetUser(id, userAuthority, cancellationToken);

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
