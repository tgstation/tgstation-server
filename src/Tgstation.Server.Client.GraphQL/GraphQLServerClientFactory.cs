using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;

using Tgstation.Server.Api;
using Tgstation.Server.Client.GraphQL.Serializers;

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
			throw new NotImplementedException();
		}

		/// <inheritdoc />
		public ValueTask<IAuthenticatedGraphQLServerClient> CreateFromOAuth(Uri host, string oAuthCode, OAuthProvider oAuthProvider, CancellationToken cancellationToken = default)
		{
			throw new NotImplementedException();
		}

		/// <inheritdoc />
		public IAuthenticatedGraphQLServerClient CreateFromToken(Uri host, string token)
		{
			throw new NotImplementedException();
		}

		/// <inheritdoc />
		public IGraphQLServerClient CreateUnauthenticated(Uri host)
		{
			var serviceCollection = new ServiceCollection();

			var clientBuilder = serviceCollection
				.AddGraphQLClient();
			var graphQLEndpoint = new Uri(host, Routes.GraphQL);
			clientBuilder.ConfigureHttpClient(client => client.BaseAddress = graphQLEndpoint);

			serviceCollection.AddSerializer<UnsignedIntSerializer>();
			serviceCollection.AddSerializer<SemverSerializer>();

			var serviceProvider = serviceCollection.BuildServiceProvider();

			return new GraphQLServerClient(
				serviceProvider.GetRequiredService<IGraphQLClient>(),
				serviceProvider);
		}
	}
}
