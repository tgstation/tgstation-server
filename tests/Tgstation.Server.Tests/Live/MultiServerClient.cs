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
	sealed class MultiServerClient : IMultiServerClient, IAsyncDisposable
	{
		public IRestServerClient RestClient { get; }
		public IGraphQLServerClient GraphQLClient { get; }

		public MultiServerClient(IRestServerClient restServerClient, IGraphQLServerClient graphQLServerClient)
		{
			this.RestClient = restServerClient;
			this.GraphQLClient = graphQLServerClient;
		}

		public static bool UseGraphQL => Boolean.TryParse(Environment.GetEnvironmentVariable("TGS_TEST_GRAPHQL"), out var result) && result;

		public ValueTask DisposeAsync()
			=> ValueTaskExtensions.WhenAll(
				RestClient.DisposeAsync(),
				GraphQLClient?.DisposeAsync() ?? ValueTask.CompletedTask);

		public ValueTask Execute(
			Func<IRestServerClient, ValueTask> restAction,
			Func<IGraphQLServerClient, ValueTask> graphQLAction)
		{
			if (UseGraphQL)
				return graphQLAction(GraphQLClient);

			return restAction(RestClient);
		}

		public async ValueTask<(TRestResult, TGraphQLResult)> ExecuteReadOnlyConfirmEquivalence<TRestResult, TGraphQLResult>(
			Func<IRestServerClient, ValueTask<TRestResult>> restAction,
			Func<IGraphQLClient, Task<IOperationResult<TGraphQLResult>>> graphQLAction,
			Func<TRestResult, TGraphQLResult, bool> comparison,
			CancellationToken cancellationToken)
			where TGraphQLResult : class
		{
			var restTask = restAction(RestClient);
			if (!UseGraphQL)
			{
				return (await restTask, null);
			}

			var graphQLResult = await GraphQLClient.RunOperation(graphQLAction, cancellationToken);

			graphQLResult.EnsureNoErrors();

			var restResult = await restTask;
			var comparisonResult = comparison(restResult, graphQLResult.Data);
			Assert.IsTrue(comparisonResult, "REST/GraphQL results differ!");

			return (restResult, graphQLResult.Data);
		}

		public ValueTask<IDisposable> Subscribe<TResultData>(Func<IGraphQLClient, IObservable<IOperationResult<TResultData>>> operationExecutor, IObserver<IOperationResult<TResultData>> observer, CancellationToken cancellationToken) where TResultData : class
			=> GraphQLClient?.Subscribe(operationExecutor, observer, cancellationToken) ?? ValueTask.FromResult<IDisposable>(new CancellationTokenSource());
	}
}
