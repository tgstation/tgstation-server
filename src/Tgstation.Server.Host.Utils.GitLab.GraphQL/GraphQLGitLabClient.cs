using System;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;

namespace Tgstation.Server.Host.Utils.GitLab.GraphQL
{
	/// <inheritdoc />
	sealed class GraphQLGitLabClient : IGraphQLGitLabClient
	{
		/// <inheritdoc />
		public IGraphQLClient GraphQL { get; }

		/// <summary>
		/// The <see cref="ServiceProvider"/> containing the <see cref="GraphQL"/> client.
		/// </summary>
		readonly ServiceProvider serviceProvider;

		/// <summary>
		/// Initializes a new instance of the <see cref="GraphQLGitLabClient"/> class.
		/// </summary>
		/// <param name="serviceProvider">The value of <see cref="serviceProvider"/>.</param>
		public GraphQLGitLabClient(ServiceProvider serviceProvider)
		{
			this.serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
			GraphQL = serviceProvider.GetService<IGraphQLClient>() ?? throw new ArgumentException($"Expected an {nameof(IGraphQLClient)} service in the provider!", nameof(serviceProvider));
		}

		/// <inheritdoc />
		public ValueTask DisposeAsync()
			=> serviceProvider.DisposeAsync();
	}
}
