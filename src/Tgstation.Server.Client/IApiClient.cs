using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;

using Tgstation.Server.Api;
using Tgstation.Server.Api.Models;

namespace Tgstation.Server.Client
{
	/// <summary>
	/// Web interface for the API.
	/// </summary>
	interface IApiClient : ITransferClient, IAsyncDisposable
	{
		/// <summary>
		/// The <see cref="ApiHeaders"/> the <see cref="IApiClient"/> uses.
		/// </summary>
		ApiHeaders Headers { get; set; }

		/// <summary>
		/// The <see cref="Uri"/> pointing the tgstation-server.
		/// </summary>
		Uri Url { get; }

		/// <summary>
		/// The request timeout.
		/// </summary>
		TimeSpan Timeout { get; set; }

		/// <summary>
		/// Adds a <paramref name="requestLogger"/> to the request pipeline.
		/// </summary>
		/// <param name="requestLogger">The <see cref="IRequestLogger"/> to add.</param>
		void AddRequestLogger(IRequestLogger requestLogger);

		/// <summary>
		/// Subscribe to all job updates available to the <see cref="IRestServerClient"/>.
		/// </summary>
		/// <typeparam name="THubImplementation">The <see cref="Type"/> of the hub being implemented.</typeparam>
		/// <param name="hubImplementation">The <typeparamref name="THubImplementation"/> to use for proxying the methods of the hub connection.</param>
		/// <param name="retryPolicy">The optional <see cref="IRetryPolicy"/> to use for the backing connection. The default retry policy waits for 1, 2, 4, 8, and 16 seconds, then 30s repeatedly.</param>
		/// <param name="loggingConfigureAction">The optional <see cref="Action{T1}"/> used to configure a <see cref="ILoggingBuilder"/>.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>An <see cref="IAsyncDisposable"/> representing the lifetime of the subscription.</returns>
		ValueTask<IAsyncDisposable> CreateHubConnection<THubImplementation>(
			THubImplementation hubImplementation,
			IRetryPolicy? retryPolicy,
			Action<ILoggingBuilder>? loggingConfigureAction,
			CancellationToken cancellationToken)
			where THubImplementation : class;

		/// <summary>
		/// Run an HTTP PUT request.
		/// </summary>
		/// <typeparam name="TBody">The type to of the request body.</typeparam>
		/// <typeparam name="TResult">The type of the response body.</typeparam>
		/// <param name="route">The server route to make the request to.</param>
		/// <param name="body">The request body.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in the response body as a <typeparamref name="TResult"/>.</returns>
		ValueTask<TResult> Create<TBody, TResult>(string route, TBody body, CancellationToken cancellationToken)
			where TBody : class;

		/// <summary>
		/// Run an HTTP PUT request.
		/// </summary>
		/// <typeparam name="TResult">The type of the response body.</typeparam>
		/// <param name="route">The server route to make the request to.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in the response body as a <typeparamref name="TResult"/>.</returns>
		ValueTask<TResult> Create<TResult>(string route, CancellationToken cancellationToken);

		/// <summary>
		/// Run an HTTP GET request.
		/// </summary>
		/// <typeparam name="TResult">The type of the response body.</typeparam>
		/// <param name="route">The server route to make the request to.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in the response body as a <typeparamref name="TResult"/>.</returns>
		ValueTask<TResult> Read<TResult>(string route, CancellationToken cancellationToken);

		/// <summary>
		/// Run an HTTP POST request.
		/// </summary>
		/// <typeparam name="TBody">The type to of the request body.</typeparam>
		/// <typeparam name="TResult">The type of the response body.</typeparam>
		/// <param name="route">The server route to make the request to.</param>
		/// <param name="body">The request body.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in the response body as a <typeparamref name="TResult"/>.</returns>
		ValueTask<TResult> Update<TBody, TResult>(string route, TBody body, CancellationToken cancellationToken)
			where TBody : class;

		/// <summary>
		/// Run an HTTP POST request.
		/// </summary>
		/// <typeparam name="TResult">The type of the response body.</typeparam>
		/// <param name="route">The server route to make the request to.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in the response body as a <typeparamref name="TResult"/>.</returns>
		ValueTask<TResult> Update<TResult>(string route, CancellationToken cancellationToken);

		/// <summary>
		/// Run an HTTP POST request.
		/// </summary>
		/// <typeparam name="TBody">The type to of the request body.</typeparam>
		/// <param name="route">The server route to make the request to.</param>
		/// <param name="body">The request body.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask"/> representing the running operation.</returns>
		ValueTask Update<TBody>(string route, TBody body, CancellationToken cancellationToken)
			where TBody : class;

		/// <summary>
		/// Run an HTTP PATCH request.
		/// </summary>
		/// <param name="route">The server route to make the request to.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask"/> representing the running operation.</returns>
		ValueTask Patch(string route, CancellationToken cancellationToken);

		/// <summary>
		/// Run an HTTP DELETE request.
		/// </summary>
		/// <param name="route">The server route to make the request to.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask"/> representing the running operation.</returns>
		ValueTask Delete(string route, CancellationToken cancellationToken);

		/// <summary>
		/// Run an HTTP PUT request.
		/// </summary>
		/// <typeparam name="TBody">The type to of the request body.</typeparam>
		/// <typeparam name="TResult">The type of the response body.</typeparam>
		/// <param name="route">The server route to make the request to.</param>
		/// <param name="body">The request body.</param>
		/// <param name="instanceId">The instance <see cref="EntityId.Id"/> to make the request to.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in the response body as a <typeparamref name="TResult"/>.</returns>
		ValueTask<TResult> Create<TBody, TResult>(
			string route,
			TBody body,
			long instanceId,
			CancellationToken cancellationToken)
			where TBody : class;

		/// <summary>
		/// Run an HTTP PUT request.
		/// </summary>
		/// <typeparam name="TResult">The type of the response body.</typeparam>
		/// <param name="route">The server route to make the request to.</param>
		/// <param name="instanceId">The instance <see cref="EntityId.Id"/> to make the request to.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in the response body as a <typeparamref name="TResult"/>.</returns>
		ValueTask<TResult> Create<TResult>(string route, long instanceId, CancellationToken cancellationToken);

		/// <summary>
		/// Run an HTTP PATCH request.
		/// </summary>
		/// <typeparam name="TResult">The type of the response body.</typeparam>
		/// <param name="route">The server route to make the request to.</param>
		/// <param name="instanceId">The instance <see cref="EntityId.Id"/> to make the request to.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in the response body as a <typeparamref name="TResult"/>.</returns>
		ValueTask<TResult> Patch<TResult>(string route, long instanceId, CancellationToken cancellationToken);

		/// <summary>
		/// Run an HTTP GET request.
		/// </summary>
		/// <typeparam name="TResult">The type of the response body.</typeparam>
		/// <param name="route">The server route to make the request to.</param>
		/// <param name="instanceId">The instance <see cref="EntityId.Id"/> to make the request to.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in the response body as a <typeparamref name="TResult"/>.</returns>
		ValueTask<TResult> Read<TResult>(string route, long instanceId, CancellationToken cancellationToken);

		/// <summary>
		/// Run an HTTP POST request.
		/// </summary>
		/// <typeparam name="TBody">The type to of the request body.</typeparam>
		/// <typeparam name="TResult">The type of the response body.</typeparam>
		/// <param name="route">The server route to make the request to.</param>
		/// <param name="body">The request body.</param>
		/// <param name="instanceId">The instance <see cref="EntityId.Id"/> to make the request to.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in the response body as a <typeparamref name="TResult"/>.</returns>
		ValueTask<TResult> Update<TBody, TResult>(
			string route,
			TBody body,
			long instanceId,
			CancellationToken cancellationToken)
			where TBody : class;

		/// <summary>
		/// Run an HTTP DELETE request.
		/// </summary>
		/// <param name="route">The server route to make the request to.</param>
		/// <param name="instanceId">The instance <see cref="EntityId.Id"/> to make the request to.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask"/> representing the running operation.</returns>
		ValueTask Delete(string route, long instanceId, CancellationToken cancellationToken);

		/// <summary>
		/// Run an HTTP DELETE request.
		/// </summary>
		/// <typeparam name="TBody">The type to of the request body.</typeparam>
		/// <param name="route">The server route to make the request to.</param>
		/// <param name="body">The request body.</param>
		/// <param name="instanceId">The instance <see cref="EntityId.Id"/> to make the request to.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask"/> representing the running operation.</returns>
		ValueTask Delete<TBody>(string route, TBody body, long instanceId, CancellationToken cancellationToken)
			where TBody : class;

		/// <summary>
		/// Run an HTTP DELETE request.
		/// </summary>
		/// <typeparam name="TResult">The type of the response body.</typeparam>
		/// <param name="route">The server route to make the request to.</param>
		/// <param name="instanceId">The instance <see cref="EntityId.Id"/> to make the request to.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in the response body as a <typeparamref name="TResult"/>.</returns>
		ValueTask<TResult> Delete<TResult>(string route, long instanceId, CancellationToken cancellationToken);

		/// <summary>
		/// Run an HTTP DELETE request.
		/// </summary>
		/// <typeparam name="TBody">The type to of the request body.</typeparam>
		/// <typeparam name="TResult">The type of the response body.</typeparam>
		/// <param name="route">The server route to make the request to.</param>
		/// <param name="body">The request body.</param>
		/// <param name="instanceId">The instance <see cref="EntityId.Id"/> to make the request to.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask"/> representing the running operation.</returns>
		ValueTask<TResult> Delete<TBody, TResult>(string route, TBody body, long instanceId, CancellationToken cancellationToken)
			where TBody : class;
	}
}
