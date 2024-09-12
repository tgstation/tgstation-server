using System;
using System.Threading;
using System.Threading.Tasks;

using HotChocolate;
using HotChocolate.Types;

using Tgstation.Server.Api.Models.Response;
using Tgstation.Server.Host.Authority;

namespace Tgstation.Server.Host.GraphQL
{
	/// <summary>
	/// Root type for GraphQL mutations.
	/// </summary>
	/// <remarks>Intentionally left mostly empty, use type extensions to properly scope operations to domains.</remarks>
	public sealed class Mutation
	{
		/// <summary>
		/// Generate JWT for authenticating with server.
		/// </summary>
		/// <param name="loginAuthority">The <see cref="IGraphQLAuthorityInvoker{TAuthority}"/> <see cref="ILoginAuthority"/>.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A Bearer token to be used with further communication with the server.</returns>
		[Error(typeof(ErrorMessageException))]
		public async ValueTask<string> Login(
			[Service] IGraphQLAuthorityInvoker<ILoginAuthority> loginAuthority,
			CancellationToken cancellationToken)
		{
			ArgumentNullException.ThrowIfNull(loginAuthority);

			var tokenResponse = await loginAuthority.Invoke<TokenResponse, TokenResponse>(
				authority => authority.AttemptLogin(cancellationToken));

			return tokenResponse!.Bearer!;
		}
	}
}
