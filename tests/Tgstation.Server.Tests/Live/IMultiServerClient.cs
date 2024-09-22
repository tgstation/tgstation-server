using StrawberryShake;
using System.Threading.Tasks;
using System.Threading;
using System;
using Tgstation.Server.Client.GraphQL;
using Tgstation.Server.Client;

namespace Tgstation.Server.Tests.Live
{
	interface IMultiServerClient
	{
		ValueTask Execute(
			Func<IRestServerClient, ValueTask> restAction,
			Func<IGraphQLServerClient, ValueTask> graphQLAction);

		ValueTask<(TRestResult, TGraphQLResult)> ExecuteReadOnlyConfirmEquivalence<TRestResult, TGraphQLResult>(
			Func<IRestServerClient, ValueTask<TRestResult>> restAction,
			Func<IGraphQLClient, Task<IOperationResult<TGraphQLResult>>> graphQLAction,
			Func<TRestResult, TGraphQLResult, bool> comparison,
			CancellationToken cancellationToken)
			where TGraphQLResult : class;
	}
}
