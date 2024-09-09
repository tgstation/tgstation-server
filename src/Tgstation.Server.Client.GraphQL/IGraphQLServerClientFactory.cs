using System;
using System.Threading;
using System.Threading.Tasks;

using Tgstation.Server.Api.Models.Response;

namespace Tgstation.Server.Client.GraphQL
{
	/// <summary>
	/// Factory for creating <see cref="IGraphQLServerClient"/>s.
	/// </summary>
	public interface IGraphQLServerClientFactory
	{
		/// <summary>
		/// Create an unauthenticated <see cref="IGraphQLServerClient"/>.
		/// </summary>
		/// <param name="host">The <see cref="Uri"/> of tgstation-server.</param>
		/// <returns>A new <see cref="IGraphQLServerClient"/>.</returns>
		IGraphQLServerClient CreateUnauthenticated(Uri host);

		/// <summary>
		/// Create a <see cref="IGraphQLServerClient"/> using a password login.
		/// </summary>
		/// <param name="host">The URL to access TGS.</param>
		/// <param name="username">The username to for the <see cref="IGraphQLServerClient"/>.</param>
		/// <param name="password">The password for the <see cref="IGraphQLServerClient"/>.</param>
		/// <param name="attemptLoginRefresh">Attempt to refresh the received <see cref="TokenResponse"/> when it expires or becomes invalid. <paramref name="username"/> and <paramref name="password"/> will be stored in memory if this is <see langword="true"/>.</param>
		/// <param name="cancellationToken">Optional <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in a new <see cref="IAuthenticatedGraphQLServerClient"/>.</returns>
		ValueTask<IAuthenticatedGraphQLServerClient> CreateFromLogin(
			Uri host,
			string username,
			string password,
			bool attemptLoginRefresh = true,
			CancellationToken cancellationToken = default);

		/// <summary>
		/// Create a <see cref="IGraphQLServerClient"/> using an OAuth login.
		/// </summary>
		/// <param name="host">The URL to access TGS.</param>
		/// <param name="oAuthCode">The OAuth code used to complete the flow.</param>
		/// <param name="oAuthProvider">The <see cref="OAuthProvider"/>.</param>
		/// <param name="cancellationToken">Optional <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in a new <see cref="IAuthenticatedGraphQLServerClient"/>.</returns>
		ValueTask<IAuthenticatedGraphQLServerClient> CreateFromOAuth(
			Uri host,
			string oAuthCode,
			OAuthProvider oAuthProvider,
			CancellationToken cancellationToken = default);

		/// <summary>
		/// Create a <see cref="IRestServerClient"/>.
		/// </summary>
		/// <param name="host">The URL to access TGS.</param>
		/// <param name="token">The <see cref="TokenResponse"/> to access the API with.</param>
		/// <returns>A new <see cref="IGraphQLServerClient"/>.</returns>
		IAuthenticatedGraphQLServerClient CreateFromToken(
			Uri host,
			string token);
	}
}
