using System;
using System.Threading;
using System.Threading.Tasks;

using HotChocolate;
using HotChocolate.Types;

using Tgstation.Server.Host.Authority;
using Tgstation.Server.Host.GraphQL.Mutations;

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
		public ValueTask<LoginPayload> Login(
			[Service] IGraphQLAuthorityInvoker<ILoginAuthority> loginAuthority,
			CancellationToken cancellationToken)
		{
			ArgumentNullException.ThrowIfNull(loginAuthority);

			return loginAuthority.Invoke<LoginPayload, LoginPayload>(
				authority => authority.AttemptLogin(cancellationToken))!;
		}
	}
}
