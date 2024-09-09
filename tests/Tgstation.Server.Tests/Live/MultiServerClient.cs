using System;
using System.Threading.Tasks;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Tgstation.Server.Client;
using Tgstation.Server.Client.GraphQL;

namespace Tgstation.Server.Tests.Live
{
	sealed class MultiServerClient
	{
		readonly IRestServerClient restServerClient;
		readonly IGraphQLServerClient graphQLServerClient;

		readonly bool useGraphQL;

		public MultiServerClient(IRestServerClient restServerClient, IGraphQLServerClient graphQLServerClient)
		{
			this.restServerClient = restServerClient ?? throw new ArgumentNullException(nameof(restServerClient));
			this.graphQLServerClient = graphQLServerClient ?? throw new ArgumentNullException(nameof(graphQLServerClient));
			this.useGraphQL = Boolean.TryParse(Environment.GetEnvironmentVariable("TGS_TEST_GRAPHQL"), out var result) && result;
		}

		public ValueTask Execute(
			Func<IRestServerClient, ValueTask> restAction,
			Func<IGraphQLClient, ValueTask> graphQLAction)
		{
			if (useGraphQL)
				return graphQLServerClient.RunQuery(graphQLAction);

			return restAction(restServerClient);
		}

		public async ValueTask ExecuteReadOnlyConfirmEquivalence<TRestResult, TGraphQLResult>(
			Func<IRestServerClient, ValueTask<TRestResult>> restAction,
			Func<IGraphQLClient, ValueTask<TGraphQLResult>> graphQLAction,
			Func<TRestResult, TGraphQLResult, bool> comparison)
		{
			var restTask = restAction(this.restServerClient);
			TGraphQLResult graphQLResult = default;
			await this.graphQLServerClient.RunQuery(async gqlClient => graphQLResult = await graphQLAction(gqlClient));

			var restResult = await restTask;
			Assert.IsTrue(comparison(restResult, graphQLResult), "REST/GraphQL results differ!");
		}
	}
}
