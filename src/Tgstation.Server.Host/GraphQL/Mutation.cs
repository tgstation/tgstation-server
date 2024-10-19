﻿using System;
using System.Threading;
using System.Threading.Tasks;

using HotChocolate;
using HotChocolate.Types;

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
		/// Generate a JWT for authenticating with server. This is the only operation that accepts and required basic authentication.
		/// </summary>
		/// <param name="loginAuthority">The <see cref="IGraphQLAuthorityInvoker{TAuthority}"/> for the <see cref="ILoginAuthority"/>.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A Bearer token to be used with further communication with the server.</returns>
		[Error(typeof(ErrorMessageException))]
		public ValueTask<LoginResult> Login(
			[Service] IGraphQLAuthorityInvoker<ILoginAuthority> loginAuthority,
			CancellationToken cancellationToken)
		{
			ArgumentNullException.ThrowIfNull(loginAuthority);

			return loginAuthority.Invoke<LoginResult, LoginResult>(
				authority => authority.AttemptLogin(cancellationToken));
		}
	}
}
