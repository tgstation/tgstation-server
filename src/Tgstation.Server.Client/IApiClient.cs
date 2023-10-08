using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Tgstation.Server.Api;
using Tgstation.Server.Api.Models;
using Tgstation.Server.Api.Models.Response;

namespace Tgstation.Server.Client
{
	/// <summary>
	/// Web interface for the API.
	/// </summary>
	interface IApiClient : IDisposable
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

		/// <summary>
		/// Downloads a file <see cref="Stream"/> for a given <paramref name="ticket"/>.
		/// </summary>
		/// <param name="ticket">The <see cref="FileTicketResponse"/> to download.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in the downloaded <see cref="Stream"/>.</returns>
		ValueTask<Stream> Download(FileTicketResponse ticket, CancellationToken cancellationToken);

		/// <summary>
		/// Uploads a given <paramref name="uploadStream"/> for a given <paramref name="ticket"/>.
		/// </summary>
		/// <param name="ticket">The <see cref="FileTicketResponse"/> to download.</param>
		/// <param name="uploadStream">The <see cref="Stream"/> to upload. <see langword="null"/> represents an empty file.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask"/> representing the running operation.</returns>
		ValueTask Upload(FileTicketResponse ticket, Stream? uploadStream, CancellationToken cancellationToken);
	}
}
