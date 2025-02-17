using System;
using System.Net.Http.Headers;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;

namespace Tgstation.Server.Host.Utils.GitLab.GraphQL
{
	/// <summary>
	/// Factory for creating <see cref="IGraphQLGitLabClient"/>s.
	/// </summary>
	public sealed class GraphQLGitLabClientFactory
	{
		/// <summary>
		/// Sets up a <see cref="IGraphQLGitLabClient"/>.
		/// </summary>
		/// <param name="bearerToken">The token to use for authentication, if any.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in a new <see cref="IGraphQLGitLabClient"/>.</returns>
		public static async ValueTask<IGraphQLGitLabClient> CreateClient(string? bearerToken = null)
		{
			var serviceCollection = new ServiceCollection();

			var clientBuilder = serviceCollection
				.AddGraphQLClient();
			var graphQLEndpoint = new Uri("https://gitlab.com/api/graphql");

			clientBuilder.ConfigureHttpClient(
				client =>
				{
					client.BaseAddress = new Uri("https://gitlab.com/api/graphql");
					if (bearerToken != null)
					{
						client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
					}
				});

			var serviceProvider = serviceCollection.BuildServiceProvider();
			try
			{
				return new GraphQLGitLabClient(serviceProvider);
			}
			catch
			{
				await serviceProvider.DisposeAsync();
				throw;
			}
		}
	}
}
