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

		/// <summary>
		/// Subcribes to the GraphQL subscription indicated by <paramref name="operationExecutor"/>.
		/// </summary>
		/// <typeparam name="TResultData">The <see cref="Type"/> of the <see cref="IOperationResult{TResultData}"/>'s <see cref="IOperationResult{TResultData}.Data"/>.</typeparam>
		/// <param name="operationExecutor">A <see cref="Func{T, TResult}"/> which initiates a single subscription on a given <see cref="IGraphQLClient"/> and returns a <see cref="ValueTask{TResult}"/> resulting in the <typeparamref name="TResultData"/> <see cref="IOperationResult{TResultData}"/> <see cref="IObservable{T}"/>.</param>
		/// <param name="observer">The <see cref="IObserver{T}"/> for <typeparamref name="TResultData"/> <see cref="IOperationResult{TResultData}"/>s.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in the <see cref="IDisposable"/> representing the lifetime of the subscription.</returns>
		ValueTask<IDisposable> Subscribe<TResultData>(Func<IGraphQLClient, IObservable<IOperationResult<TResultData>>> operationExecutor, IObserver<IOperationResult<TResultData>> observer, CancellationToken cancellationToken)
			where TResultData : class;
	}
}
