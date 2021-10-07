using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Tgstation.Server.Api.Models;
using Tgstation.Server.Api.Models.Response;

namespace Tgstation.Server.Client
{
	/// <summary>
	/// Factory for creating <see cref="IServerClient"/>s.
	/// </summary>
	public interface IServerClientFactory
	{
		/// <summary>
		/// Gets the <see cref="ServerInformationResponse"/> for a given <paramref name="host"/>.
		/// </summary>
		/// <param name="host">The URL to access TGS.</param>
		/// <param name="requestLoggers">Optional <see cref="IRequestLogger"/>s.</param>
		/// <param name="timeout">Optional <see cref="TimeSpan"/> representing timeout for the HTTP request.</param>
		/// <param name="cancellationToken">Optional <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="ServerInformationResponse"/>.</returns>
		Task<ServerInformationResponse> GetServerInformation(
			   Uri host,
			   IEnumerable<IRequestLogger>? requestLoggers = null,
			   TimeSpan? timeout = null,
			   CancellationToken cancellationToken = default);

		/// <summary>
		/// Create a <see cref="IServerClient"/> using a password login.
		/// </summary>
		/// <param name="host">The URL to access TGS.</param>
		/// <param name="username">The username to for the <see cref="IServerClient"/>.</param>
		/// <param name="password">The password for the <see cref="IServerClient"/>.</param>
		/// <param name="requestLoggers">Optional initial <see cref="IRequestLogger"/>s to add to the <see cref="IServerClient"/>.</param>
		/// <param name="timeout">Optional <see cref="TimeSpan"/> representing timeout for the connection.</param>
		/// <param name="attemptLoginRefresh">Attempt to refresh the received <see cref="TokenResponse"/> when it expires or becomes invalid. <paramref name="username"/> and <paramref name="password"/> will be stored in memory if this is <see langword="true"/>.</param>
		/// <param name="cancellationToken">Optional <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in a new <see cref="IServerClient"/>.</returns>
		Task<IServerClient> CreateFromLogin(
			Uri host,
			string username,
			string password,
			IEnumerable<IRequestLogger>? requestLoggers = null,
			TimeSpan? timeout = null,
			bool attemptLoginRefresh = true,
			CancellationToken cancellationToken = default);

		/// <summary>
		/// Create a <see cref="IServerClient"/> using am OAuth login.
		/// </summary>
		/// <param name="host">The URL to access TGS.</param>
		/// <param name="oAuthCode">The OAuth code used to complete the flow.</param>
		/// <param name="oAuthProvider">The <see cref="OAuthProvider"/>.</param>
		/// <param name="requestLoggers">Optional initial <see cref="IRequestLogger"/>s to add to the <see cref="IServerClient"/>.</param>
		/// <param name="timeout">Optional <see cref="TimeSpan"/> representing timeout for the connection.</param>
		/// <param name="cancellationToken">Optional <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in a new <see cref="IServerClient"/>.</returns>
		Task<IServerClient> CreateFromOAuth(
			Uri host,
			string oAuthCode,
			OAuthProvider oAuthProvider,
			IEnumerable<IRequestLogger>? requestLoggers = null,
			TimeSpan? timeout = null,
			CancellationToken cancellationToken = default);

		/// <summary>
		/// Create a <see cref="IServerClient"/>.
		/// </summary>
		/// <param name="host">The URL to access TGS.</param>
		/// <param name="token">The <see cref="TokenResponse"/> to access the API with.</param>
		/// <returns>A new <see cref="IServerClient"/>.</returns>
		IServerClient CreateFromToken(
			Uri host,
			TokenResponse token);
	}
}
