using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

using Tgstation.Server.Api;
using Tgstation.Server.Api.Models;
using Tgstation.Server.Api.Models.Response;

namespace Tgstation.Server.Client
{
	/// <inheritdoc />
	public sealed class RestServerClientFactory : IRestServerClientFactory
	{
		/// <summary>
		/// The <see cref="IApiClientFactory"/> for the <see cref="RestServerClientFactory"/>.
		/// </summary>
		internal static IApiClientFactory ApiClientFactory { get; set; }

		/// <summary>
		/// The <see cref="ProductHeaderValue"/> for the <see cref="RestServerClientFactory"/>.
		/// </summary>
		readonly ProductHeaderValue productHeaderValue;

		/// <summary>
		/// Initializes static members of the <see cref="RestServerClientFactory"/> class.
		/// </summary>
		static RestServerClientFactory()
		{
			ApiClientFactory = new ApiClientFactory();
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="RestServerClientFactory"/> class.
		/// </summary>
		/// <param name="productHeaderValue">The value of <see cref="productHeaderValue"/>.</param>
		public RestServerClientFactory(ProductHeaderValue productHeaderValue)
		{
			this.productHeaderValue = productHeaderValue ?? throw new ArgumentNullException(nameof(productHeaderValue));
		}

		/// <inheritdoc />
		public ValueTask<IRestServerClient> CreateFromLogin(
			Uri host,
			string username,
			string password,
			IEnumerable<IRequestLogger>? requestLoggers = null,
			TimeSpan? timeout = null,
			bool attemptLoginRefresh = true,
			CancellationToken cancellationToken = default)
		{
			if (host == null)
				throw new ArgumentNullException(nameof(host));
			if (username == null)
				throw new ArgumentNullException(nameof(username));
			if (password == null)
				throw new ArgumentNullException(nameof(password));

			var loginHeaders = new ApiHeaders(productHeaderValue, username, password);
			return CreateWithNewToken(
				host,
				loginHeaders,
				requestLoggers,
				timeout,
				attemptLoginRefresh,
				cancellationToken);
		}

		/// <inheritdoc />
		public ValueTask<IRestServerClient> CreateFromOAuth(
			Uri host,
			string oAuthCode,
			OAuthProvider oAuthProvider,
			IEnumerable<IRequestLogger>? requestLoggers = null,
			TimeSpan? timeout = null,
			CancellationToken cancellationToken = default)
		{
			if (host == null)
				throw new ArgumentNullException(nameof(host));
			if (oAuthCode == null)
				throw new ArgumentNullException(nameof(oAuthCode));

			var loginHeaders = new ApiHeaders(productHeaderValue, oAuthCode, oAuthProvider);
			return CreateWithNewToken(
				host,
				loginHeaders,
				requestLoggers,
				timeout,
				false,
				cancellationToken);
		}

		/// <inheritdoc />
		public IRestServerClient CreateFromToken(Uri host, TokenResponse token)
		{
			if (host == null)
				throw new ArgumentNullException(nameof(host));
			if (token == null)
				throw new ArgumentNullException(nameof(token));
			if (token.Bearer == null)
				throw new InvalidOperationException("token.Bearer should not be null!");

			var serverClient = new RestServerClient(
				ApiClientFactory.CreateApiClient(
					host,
					new ApiHeaders(
						productHeaderValue,
						token),
					null,
					false));
			return serverClient;
		}

		/// <inheritdoc />
		public async ValueTask<ServerInformationResponse> GetServerInformation(
			Uri host,
			IEnumerable<IRequestLogger>? requestLoggers = null,
			TimeSpan? timeout = null,
			CancellationToken cancellationToken = default)
		{
			await using var api = ApiClientFactory.CreateApiClient(
				host,
				new ApiHeaders(
					productHeaderValue,
					new TokenResponse
					{
						Bearer = "unused",
					}),
				null,
				true);

			if (requestLoggers != null)
				foreach (var requestLogger in requestLoggers)
					api.AddRequestLogger(requestLogger);

			if (timeout.HasValue)
				api.Timeout = timeout.Value;

			return await api.Read<ServerInformationResponse>(Routes.ApiRoot, cancellationToken).ConfigureAwait(false);
		}

		/// <summary>
		/// Creates a <see cref="IRestServerClient"/> from a login operation.
		/// </summary>
		/// <param name="host">The URL to access TGS.</param>
		/// <param name="loginHeaders">The <see cref="ApiHeaders"/> to use for the login operation.</param>
		/// <param name="requestLoggers">Optional initial <see cref="IRequestLogger"/>s to add to the <see cref="IRestServerClient"/>.</param>
		/// <param name="timeout">Optional <see cref="TimeSpan"/> representing timeout for the connection.</param>
		/// <param name="attemptLoginRefresh">If <paramref name="loginHeaders"/> may be used to re-login in the future.</param>
		/// <param name="cancellationToken">Optional <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in a new <see cref="IRestServerClient"/>.</returns>
		async ValueTask<IRestServerClient> CreateWithNewToken(
			Uri host,
			ApiHeaders loginHeaders,
			IEnumerable<IRequestLogger>? requestLoggers,
			TimeSpan? timeout,
			bool attemptLoginRefresh,
			CancellationToken cancellationToken)
		{
			requestLoggers ??= Enumerable.Empty<IRequestLogger>();

			TokenResponse token;
			await using (var api = ApiClientFactory.CreateApiClient(host, loginHeaders, null, false))
			{
				foreach (var requestLogger in requestLoggers)
					api.AddRequestLogger(requestLogger);

				if (timeout.HasValue)
					api.Timeout = timeout.Value;
				token = await api.Update<TokenResponse>(Routes.ApiRoot, cancellationToken).ConfigureAwait(false);
			}

			var apiHeaders = new ApiHeaders(productHeaderValue, token);
			var client = new RestServerClient(
				ApiClientFactory.CreateApiClient(
					host,
					apiHeaders,
					attemptLoginRefresh ? loginHeaders : null,
					false));
			if (timeout.HasValue)
				client.Timeout = timeout.Value;

			foreach (var requestLogger in requestLoggers)
				client.AddRequestLogger(requestLogger);

			return client;
		}
	}
}
