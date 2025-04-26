using System;
using System.Threading;
using System.Threading.Tasks;

using HotChocolate;

using Tgstation.Server.Host.Authority;
using Tgstation.Server.Host.GraphQL.Mutations.Payloads;

namespace Tgstation.Server.Host.GraphQL
{
	/// <summary>
	/// Root type for GraphQL mutations.
	/// </summary>
	/// <remarks>Intentionally left mostly empty, use type extensions to properly scope operations to domains.</remarks>
	[GraphQLDescription(GraphQLDescription)]
	public sealed class Mutation
	{
		/// <summary>
		/// Description to show on the <see cref="Mutation"/> type.
		/// </summary>
		public const string GraphQLDescription = "Root Mutation type";

		/// <summary>
		/// Generate a JWT for authenticating with server. This requires either the Basic authentication or OAuth authentication schemes.
		/// </summary>
		/// <param name="loginAuthority">The <see cref="IGraphQLAuthorityInvoker{TAuthority}"/> for the <see cref="ILoginAuthority"/>.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="LoginResult"/>.</returns>
		public ValueTask<LoginResult> Login(
			[Service] IGraphQLAuthorityInvoker<ILoginAuthority> loginAuthority,
			CancellationToken cancellationToken)
		{
			ArgumentNullException.ThrowIfNull(loginAuthority);

			return loginAuthority.Invoke<LoginResult, LoginResult>(
				authority => authority.AttemptLogin(cancellationToken));
		}

		/// <summary>
		/// Generate an OAuth user token for the requested service. This requires the OAuth authentication scheme.
		/// </summary>
		/// <param name="loginAuthority">The <see cref="IGraphQLAuthorityInvoker{TAuthority}"/> for the <see cref="ILoginAuthority"/>.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>An <see cref="OAuthGatewayLoginResult"/>.</returns>
		public ValueTask<OAuthGatewayLoginResult> OAuthGateway(
			[Service] IGraphQLAuthorityInvoker<ILoginAuthority> loginAuthority,
			CancellationToken cancellationToken)
		{
			ArgumentNullException.ThrowIfNull(loginAuthority);

			return loginAuthority.Invoke<OAuthGatewayLoginResult, OAuthGatewayLoginResult>(
				authority => authority.AttemptOAuthGatewayLogin(cancellationToken));
		}
	}
}
