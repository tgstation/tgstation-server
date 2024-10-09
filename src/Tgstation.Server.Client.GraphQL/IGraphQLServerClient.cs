using System;
using System.Threading;
using System.Threading.Tasks;

using StrawberryShake;

namespace Tgstation.Server.Client.GraphQL
{
	/// <summary>
	/// Wrapper for using a TGS <see cref="IGraphQLClient"/>.
	/// </summary>
	public interface IGraphQLServerClient : IAsyncDisposable
	{
		/// <summary>
		/// Runs a given <paramref name="operationExecutor"/>. It may be invoked multiple times depending on the behavior of the <see cref="IGraphQLServerClient"/> if reauthentication is required.
		/// </summary>
		/// <typeparam name="TResultData">The <see cref="Type"/> of the <see cref="IOperationResult{TResultData}"/>'s <see cref="IOperationResult{TResultData}.Data"/>.</typeparam>
		/// <param name="operationExecutor">A <see cref="Func{T, TResult}"/> which executes a single query on a given <see cref="IGraphQLClient"/> and returns a <see cref="ValueTask{TResult}"/> resulting in the <typeparamref name="TResultData"/> <see cref="IOperationResult{TResultData}"/>.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in the <see cref="IOperationResult{TResultData}"/>.</returns>
		/// <exception cref="AuthenticationException">Thrown when automatic reauthentication fails.</exception>
		ValueTask<IOperationResult<TResultData>> RunOperationAsync<TResultData>(Func<IGraphQLClient, ValueTask<IOperationResult<TResultData>>> operationExecutor, CancellationToken cancellationToken)
			where TResultData : class;

		/// <summary>
		/// Runs a given <paramref name="operationExecutor"/>. It may be invoked multiple times depending on the behavior of the <see cref="IGraphQLServerClient"/> if reauthentication is required.
		/// </summary>
		/// <typeparam name="TResultData">The <see cref="Type"/> of the <see cref="IOperationResult{TResultData}"/>'s <see cref="IOperationResult{TResultData}.Data"/>.</typeparam>
		/// <param name="operationExecutor">A <see cref="Func{T, TResult}"/> which executes a single query on a given <see cref="IGraphQLClient"/> and returns a <see cref="ValueTask{TResult}"/> resulting in the <typeparamref name="TResultData"/> <see cref="IOperationResult{TResultData}"/>.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in the <see cref="IOperationResult{TResultData}"/>.</returns>
		/// <exception cref="AuthenticationException">Thrown when automatic reauthentication fails.</exception>
		ValueTask<IOperationResult<TResultData>> RunOperation<TResultData>(Func<IGraphQLClient, Task<IOperationResult<TResultData>>> operationExecutor, CancellationToken cancellationToken)
			where TResultData : class;

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
