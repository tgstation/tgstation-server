using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using StrawberryShake;

using Tgstation.Server.Client;
using Tgstation.Server.Client.GraphQL;
using Tgstation.Server.Common.Extensions;

namespace Tgstation.Server.Tests.Live
{
	sealed class MultiServerClient(IRestServerClient restServerClient, IGraphQLServerClient graphQLServerClient) : IAsyncDisposable
	{
		public IRestServerClient RestClient { get; } = restServerClient ?? throw new ArgumentNullException(nameof(restServerClient));
		public IGraphQLServerClient GraphQLClient { get; } = graphQLServerClient ?? throw new ArgumentNullException(nameof(graphQLServerClient));

		public static bool UseGraphQL => Boolean.TryParse(Environment.GetEnvironmentVariable("TGS_TEST_GRAPHQL"), out var result) && result;

		public ValueTask DisposeAsync()
			=> ValueTaskExtensions.WhenAll(
				RestClient.DisposeAsync(),
				GraphQLClient.DisposeAsync());

		public ValueTask Execute(
			Func<IRestServerClient, ValueTask> restAction,
			Func<IGraphQLServerClient, ValueTask> graphQLAction)
		{
			if (UseGraphQL)
				return graphQLAction(GraphQLClient);

			return restAction(RestClient);
		}

		public async ValueTask ExecuteReadOnlyConfirmEquivalence<TRestResult, TGraphQLResult>(
			Func<IRestServerClient, ValueTask<TRestResult>> restAction,
			Func<IGraphQLClient, Task<IOperationResult<TGraphQLResult>>> graphQLAction,
			Func<TRestResult, TGraphQLResult, bool> comparison,
			CancellationToken cancellationToken)
			where TGraphQLResult : class
		{
			var restTask = restAction(RestClient);
			var graphQLResult = await GraphQLClient.RunOperation(graphQLAction, cancellationToken);

			var restResult = await restTask;
			Assert.IsTrue(comparison(restResult, graphQLResult.Data), "REST/GraphQL results differ!");
		}
	}
}
