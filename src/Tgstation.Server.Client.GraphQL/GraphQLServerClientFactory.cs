using System;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using StrawberryShake;

using Tgstation.Server.Api;
using Tgstation.Server.Api.Models.Response;
using Tgstation.Server.Client.GraphQL.Serializers;
using Tgstation.Server.Common.Extensions;

namespace Tgstation.Server.Client.GraphQL
{
	/// <inheritdoc />
	public sealed class GraphQLServerClientFactory : IGraphQLServerClientFactory
	{
		/// <summary>
		/// The <see cref="IRestServerClientFactory"/> for the <see cref="GraphQLServerClientFactory"/>.
		/// </summary>
		readonly IRestServerClientFactory restClientFactory;

		/// <summary>
		/// Sets up a <see cref="ServiceProvider"/> for providing the <see cref="IGraphQLClient"/>.
		/// </summary>
		/// <param name="host">The <see cref="Uri"/> of the target tgstation-server.</param>
		/// <param name="addAuthorizationHandler">If the <see cref="AuthorizationMessageHandler"/> should be configured.</param>
		/// <param name="headerOverride">The <see cref="AuthenticationHeaderValue"/> override for the <see cref="AuthorizationMessageHandler"/>.</param>
		/// <param name="oAuthProvider">The <see cref="OAuthProvider"/>, if any.</param>
		/// <returns>A new <see cref="ServiceProvider"/>.</returns>
		static ServiceProvider SetupServiceProvider(Uri host, bool addAuthorizationHandler, AuthenticationHeaderValue? headerOverride = null, OAuthProvider? oAuthProvider = null)
		{
			var serviceCollection = new ServiceCollection();

			var clientBuilder = serviceCollection
				.AddGraphQLClient();
			var graphQLEndpoint = new Uri(host, Routes.GraphQL);

			clientBuilder.ConfigureHttpClient(
				client =>
				{
					client.BaseAddress = graphQLEndpoint;
					client.DefaultRequestHeaders.Add(ApiHeaders.ApiVersionHeader, $"Tgstation.Server.Api/{ApiHeaders.Version.Semver()}");
					if (oAuthProvider.HasValue)
					{
						client.DefaultRequestHeaders.Add(ApiHeaders.OAuthProviderHeader, oAuthProvider.ToString());
					}
				},
				clientBuilder =>
				{
					if (addAuthorizationHandler)
						clientBuilder.AddHttpMessageHandler(() => new AuthorizationMessageHandler(headerOverride));
				});

			serviceCollection.AddSerializer<UnsignedIntSerializer>();
			serviceCollection.AddSerializer<SemverSerializer>();
			serviceCollection.AddSerializer<JwtSerializer>();

			return serviceCollection.BuildServiceProvider();
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="GraphQLServerClientFactory"/> class.
		/// </summary>
		/// <param name="restClientFactory">The value of <see cref="restClientFactory"/>.</param>
		public GraphQLServerClientFactory(IRestServerClientFactory restClientFactory)
		{
			this.restClientFactory = restClientFactory ?? throw new ArgumentNullException(nameof(restClientFactory));
		}

		/// <inheritdoc />
		public ValueTask<IAuthenticatedGraphQLServerClient> CreateFromLogin(Uri host, string username, string password, bool attemptLoginRefresh = true, CancellationToken cancellationToken = default)
		{
			var basicCredentials = new AuthenticationHeaderValue(
				ApiHeaders.BasicAuthenticationScheme,
				Convert.ToBase64String(
					Encoding.UTF8.GetBytes($"{username}:{password}")));

			return CreateWithAuthCall(
				host,
				basicCredentials,
				null,
				attemptLoginRefresh,
				cancellationToken);
		}

		/// <inheritdoc />
		public ValueTask<IAuthenticatedGraphQLServerClient> CreateFromOAuth(Uri host, string oAuthCode, OAuthProvider oAuthProvider, CancellationToken cancellationToken = default)
		{
			var oAuthCredentials = new AuthenticationHeaderValue(
				ApiHeaders.OAuthAuthenticationScheme,
				oAuthCode);

			return CreateWithAuthCall(
				host,
				oAuthCredentials,
				oAuthProvider,
				false,
				cancellationToken);
		}

		/// <inheritdoc />
		public IAuthenticatedGraphQLServerClient CreateFromToken(Uri host, string token)
		{
			var authenticationHeader = new AuthenticationHeaderValue(
				ApiHeaders.BearerAuthenticationScheme,
				token);

			var serviceProvider = SetupServiceProvider(
				host,
				true,
				authenticationHeader);

			return new AuthenticatedGraphQLServerClient(
				serviceProvider.GetRequiredService<IGraphQLClient>(),
				serviceProvider,
				serviceProvider.GetRequiredService<ILogger<GraphQLServerClient>>(),
				CreateAuthenticatedTransferClient(host, token));
		}

		/// <inheritdoc />
		public IGraphQLServerClient CreateUnauthenticated(Uri host)
		{
			var serviceProvider = SetupServiceProvider(host, false);

			return new GraphQLServerClient(
				serviceProvider.GetRequiredService<IGraphQLClient>(),
				serviceProvider,
				serviceProvider.GetRequiredService<ILogger<GraphQLServerClient>>());
		}

		/// <summary>
		/// Create an <see cref="IAuthenticatedGraphQLServerClient"/> from a remote login call.
		/// </summary>
		/// <param name="host">The URL to access TGS.</param>
		/// <param name="initialCredentials">The initial <see cref="AuthenticationHeaderValue"/> to use to login.</param>
		/// <param name="oAuthProvider">The <see cref="OAuthProvider"/>, if any.</param>
		/// <param name="attemptLoginRefresh">If the client should attempt to renew its sessions with the <paramref name="initialCredentials"/>.</param>
		/// <param name="cancellationToken">Optional <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in a new <see cref="IAuthenticatedGraphQLServerClient"/>.</returns>
		/// <exception cref="AuthenticationException">Thrown when authentication fails.</exception>
		async ValueTask<IAuthenticatedGraphQLServerClient> CreateWithAuthCall(
			Uri host,
			AuthenticationHeaderValue initialCredentials,
			OAuthProvider? oAuthProvider,
			bool attemptLoginRefresh,
			CancellationToken cancellationToken)
		{
			var serviceProvider = SetupServiceProvider(
				host,
				true,
				oAuthProvider: oAuthProvider);
			try
			{
				var client = serviceProvider.GetRequiredService<IGraphQLClient>();

				IOperationResult<ILoginResult> result;

				var previousAuthHeader = AuthorizationMessageHandler.Header.Value;
				AuthorizationMessageHandler.Header.Value = initialCredentials;
				try
				{
					result = await client.Login.ExecuteAsync(cancellationToken).ConfigureAwait(false);
				}
				finally
				{
					AuthorizationMessageHandler.Header.Value = previousAuthHeader;
				}

				var serverClient = new AuthenticatedGraphQLServerClient(
					client,
					serviceProvider,
					serviceProvider.GetRequiredService<ILogger<GraphQLServerClient>>(),
					newHeader => AuthorizationMessageHandler.Header.Value = newHeader,
					attemptLoginRefresh ? initialCredentials : null,
					result,
					bearer => CreateAuthenticatedTransferClient(host, bearer));

				await Task.Yield();

				return serverClient;
			}
			catch
			{
				await serviceProvider.DisposeAsync().ConfigureAwait(false);
				throw;
			}
		}

		/// <summary>
		/// Create a <see cref="ITransferClient"/> for a given <paramref name="host"/> and <paramref name="bearer"/> token.
		/// </summary>
		/// <param name="host">The URL to access TGS.</param>
		/// <param name="bearer">The bearer token to access the API with.</param>
		/// <returns>A new <see cref="IRestServerClient"/>.</returns>
		IRestServerClient CreateAuthenticatedTransferClient(Uri host, string bearer)
		{
			var restClient = restClientFactory.CreateFromToken(
				host,
				new TokenResponse
				{
					Bearer = bearer,
				});

			return restClient;
		}
	}
}
