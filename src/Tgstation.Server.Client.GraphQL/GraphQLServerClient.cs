using System;
using System.Threading.Tasks;

namespace Tgstation.Server.Client.GraphQL
{
	/// <inheritdoc />
	class GraphQLServerClient : IGraphQLServerClient
	{
		/// <summary>
		/// The <see cref="IGraphQLClient"/> for the <see cref="GraphQLServerClient"/>.
		/// </summary>
		readonly IGraphQLClient graphQLClient;

		/// <summary>
		/// The <see cref="IAsyncDisposable"/> to be <see cref="DisposeAsync"/>'d with the <see cref="GraphQLServerClient"/>.
		/// </summary>
		readonly IAsyncDisposable serviceProvider;

		/// <summary>
		/// Initializes a new instance of the <see cref="GraphQLServerClient"/> class.
		/// </summary>
		/// <param name="graphQLClient">The value of <see cref="graphQLClient"/>.</param>
		/// <param name="serviceProvider">The value of <see cref="serviceProvider"/>.</param>
		public GraphQLServerClient(
			IGraphQLClient graphQLClient,
			IAsyncDisposable serviceProvider)
		{
			this.graphQLClient = graphQLClient ?? throw new ArgumentNullException(nameof(graphQLClient));
			this.serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
		}

		/// <inheritdoc />
		public ValueTask DisposeAsync() => serviceProvider.DisposeAsync();

		/// <inheritdoc />
		public virtual ValueTask RunQuery(Func<IGraphQLClient, ValueTask> queryExector)
		{
			ArgumentNullException.ThrowIfNull(queryExector);
			return queryExector(graphQLClient);
		}
	}
}
