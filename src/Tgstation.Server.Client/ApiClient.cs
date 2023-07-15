using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

using Microsoft.Net.Http.Headers;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

using Tgstation.Server.Api;
using Tgstation.Server.Api.Models;
using Tgstation.Server.Api.Models.Response;
using Tgstation.Server.Common.Http;

namespace Tgstation.Server.Client
{
	/// <inheritdoc />
	class ApiClient : IApiClient
	{
		/// <summary>
		/// PATCH <see cref="HttpMethod"/>.
		/// </summary>
		/// <remarks>HOW IS THIS NOT INCLUDED IN THE FRAMEWORK??!?!?</remarks>
		static readonly HttpMethod HttpPatch = new ("PATCH");

		/// <inheritdoc />
		public Uri Url { get; }

		/// <inheritdoc />
		public ApiHeaders Headers
		{
			get => headers;
			set => headers = value ?? throw new InvalidOperationException("Cannot set null headers!");
		}

		/// <inheritdoc />
		public TimeSpan Timeout
		{
			get => httpClient.Timeout;
			set => httpClient.Timeout = value;
		}

		/// <summary>
		/// The <see cref="IHttpClient"/> for the <see cref="ApiClient"/>.
		/// </summary>
		readonly IHttpClient httpClient;

		/// <summary>
		/// The <see cref="IRequestLogger"/>s used by the <see cref="ApiClient"/>.
		/// </summary>
		readonly List<IRequestLogger> requestLoggers;

		/// <summary>
		/// Backing field for <see cref="Headers"/>.
		/// </summary>
		readonly ApiHeaders? tokenRefreshHeaders;

		/// <summary>
		/// The <see cref="SemaphoreSlim"/> for <see cref="TokenResponse"/> refreshes.
		/// </summary>
		readonly SemaphoreSlim semaphoreSlim;

		/// <summary>
		/// If the authentication header should be stripped from requests.
		/// </summary>
		readonly bool authless;

		/// <summary>
		/// Backing field for <see cref="Headers"/>.
		/// </summary>
		ApiHeaders headers;

		/// <summary>
		/// Get the <see cref="JsonSerializerSettings"/> to use.
		/// </summary>
		/// <returns>A new <see cref="JsonSerializerSettings"/> instance.</returns>
		static JsonSerializerSettings GetSerializerSettings() => new ()
		{
			ContractResolver = new CamelCasePropertyNamesContractResolver(),
			Converters = new[] { new VersionConverter() },
		};

		/// <summary>
		/// Handle a bad HTTP <paramref name="response"/>.
		/// </summary>
		/// <param name="response">The <see cref="HttpResponseMessage"/>.</param>
		/// <param name="json">The JSON <see cref="string"/> if any.</param>
		static void HandleBadResponse(HttpResponseMessage response, string json)
		{
			ErrorMessageResponse? errorMessage = null;
			try
			{
				// check if json serializes to an error message
				errorMessage = JsonConvert.DeserializeObject<ErrorMessageResponse>(json, GetSerializerSettings());
			}
			catch (JsonException)
			{
			}

#pragma warning disable IDE0010 // Add missing cases
#pragma warning disable IDE0066 // Convert switch statement to expression
			switch (response.StatusCode)
#pragma warning restore IDE0066 // Convert switch statement to expression
#pragma warning restore IDE0010 // Add missing cases
			{
				case HttpStatusCode.UpgradeRequired:
					throw new VersionMismatchException(errorMessage, response);
				case HttpStatusCode.Unauthorized:
					throw new UnauthorizedException(errorMessage, response);
				case HttpStatusCode.InternalServerError:
					throw new ServerErrorException(errorMessage, response);
				case HttpStatusCode.NotImplemented:
				// unprocessable entity
				case (HttpStatusCode)422:
					throw new MethodNotSupportedException(errorMessage, response);
				case HttpStatusCode.NotFound:
				case HttpStatusCode.Gone:
				case HttpStatusCode.Conflict:
					throw new ConflictException(errorMessage, response);
				case HttpStatusCode.Forbidden:
					throw new InsufficientPermissionsException(response);
				case HttpStatusCode.ServiceUnavailable:
					throw new ServiceUnavailableException(response);
				case HttpStatusCode.RequestTimeout:
					throw new RequestTimeoutException(response);
				case (HttpStatusCode)429:
					throw new RateLimitException(errorMessage, response);
				default:
					throw new ApiConflictException(errorMessage, response);
			}
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ApiClient"/> class.
		/// </summary>
		/// <param name="httpClient">The value of <see cref="httpClient"/>.</param>
		/// <param name="url">The value of <see cref="Url"/>.</param>
		/// <param name="apiHeaders">The value of <see cref="Headers"/>.</param>
		/// <param name="tokenRefreshHeaders">The value of <see cref="tokenRefreshHeaders"/>.</param>
		/// <param name="authless">The value of <see cref="authless"/>.</param>
		public ApiClient(IHttpClient httpClient, Uri url, ApiHeaders apiHeaders, ApiHeaders? tokenRefreshHeaders, bool authless)
		{
			this.httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
			Url = url ?? throw new ArgumentNullException(nameof(url));
			headers = apiHeaders ?? throw new ArgumentNullException(nameof(apiHeaders));
			this.tokenRefreshHeaders = tokenRefreshHeaders;
			this.authless = authless;

			requestLoggers = new List<IRequestLogger>();
			semaphoreSlim = new SemaphoreSlim(1);
		}

		/// <inheritdoc />
		public void Dispose()
		{
			httpClient.Dispose();
			semaphoreSlim.Dispose();
		}

		/// <inheritdoc />
		public Task<TResult> Create<TResult>(string route, CancellationToken cancellationToken)
			=> RunRequest<object, TResult>(route, new object(), HttpMethod.Put, null, false, cancellationToken);

		/// <inheritdoc />
		public Task<TResult> Read<TResult>(string route, CancellationToken cancellationToken)
			=> RunRequest<object, TResult>(route, null, HttpMethod.Get, null, false, cancellationToken);

		/// <inheritdoc />
		public Task<TResult> Update<TResult>(string route, CancellationToken cancellationToken)
			=> RunRequest<object, TResult>(route, new object(), HttpMethod.Post, null, false, cancellationToken);

		/// <inheritdoc />
		public Task<TResult> Update<TBody, TResult>(string route, TBody body, CancellationToken cancellationToken)
			where TBody : class
			=> RunRequest<TBody, TResult>(route, body, HttpMethod.Post, null, false, cancellationToken);

		/// <inheritdoc />
		public Task Patch(string route, CancellationToken cancellationToken) => RunRequest<object>(route, null, HttpPatch, null, false, cancellationToken);

		/// <inheritdoc />
		public Task Update<TBody>(string route, TBody body, CancellationToken cancellationToken)
			where TBody : class
			=> RunRequest<TBody, object>(route, body, HttpMethod.Post, null, false, cancellationToken);

		/// <inheritdoc />
		public Task<TResult> Create<TBody, TResult>(string route, TBody body, CancellationToken cancellationToken)
			where TBody : class
			=> RunRequest<TBody, TResult>(route, body, HttpMethod.Put, null, false, cancellationToken);

		/// <inheritdoc />
		public Task Delete(string route, CancellationToken cancellationToken)
			=> RunRequest<object>(route, null, HttpMethod.Delete, null, false, cancellationToken);

		/// <inheritdoc />
		public Task<TResult> Create<TBody, TResult>(string route, TBody body, long instanceId, CancellationToken cancellationToken)
			where TBody : class
			=> RunRequest<TBody, TResult>(route, body, HttpMethod.Put, instanceId, false, cancellationToken);

		/// <inheritdoc />
		public Task<TResult> Read<TResult>(string route, long instanceId, CancellationToken cancellationToken)
			=> RunRequest<TResult>(route, null, HttpMethod.Get, instanceId, false, cancellationToken);

		/// <inheritdoc />
		public Task<TResult> Update<TBody, TResult>(string route, TBody body, long instanceId, CancellationToken cancellationToken)
			where TBody : class
			=> RunRequest<TBody, TResult>(route, body, HttpMethod.Post, instanceId, false, cancellationToken);

		/// <inheritdoc />
		public Task Delete(string route, long instanceId, CancellationToken cancellationToken)
			=> RunRequest<object>(route, null, HttpMethod.Delete, instanceId, false, cancellationToken);

		/// <inheritdoc />
		public Task Delete<TBody>(string route, TBody body, long instanceId, CancellationToken cancellationToken)
			where TBody : class
			=> RunRequest<TBody, object>(route, body, HttpMethod.Delete, instanceId, false, cancellationToken);

		/// <inheritdoc />
		public Task<TResult> Delete<TResult>(string route, long instanceId, CancellationToken cancellationToken)
			=> RunRequest<TResult>(route, null, HttpMethod.Delete, instanceId, false, cancellationToken);

		/// <inheritdoc />
		public Task<TResult> Delete<TBody, TResult>(string route, TBody body, long instanceId, CancellationToken cancellationToken)
			where TBody : class
			=> RunRequest<TBody, TResult>(route, body, HttpMethod.Delete, instanceId, false, cancellationToken);

		/// <inheritdoc />
		public Task<TResult> Create<TResult>(string route, long instanceId, CancellationToken cancellationToken)
			=> RunRequest<object, TResult>(route, new object(), HttpMethod.Put, instanceId, false, cancellationToken);

		/// <inheritdoc />
		public Task<TResult> Patch<TResult>(string route, long instanceId, CancellationToken cancellationToken)
			=> RunRequest<object, TResult>(route, new object(), HttpPatch, instanceId, false, cancellationToken);

		/// <inheritdoc />
		public void AddRequestLogger(IRequestLogger requestLogger) => requestLoggers.Add(requestLogger ?? throw new ArgumentNullException(nameof(requestLogger)));

		/// <inheritdoc />
		public Task<Stream> Download(FileTicketResponse ticket, CancellationToken cancellationToken)
		{
			if (ticket == null)
				throw new ArgumentNullException(nameof(ticket));

			return RunRequest<Stream>(
				$"{Routes.Transfer}?ticket={HttpUtility.UrlEncode(ticket.FileTicket)}",
				null,
				HttpMethod.Get,
				null,
				false,
				cancellationToken);
		}

		/// <inheritdoc />
		public async Task Upload(FileTicketResponse ticket, Stream? uploadStream, CancellationToken cancellationToken)
		{
			if (ticket == null)
				throw new ArgumentNullException(nameof(ticket));

			MemoryStream? memoryStream = null;
			if (uploadStream == null)
				memoryStream = new MemoryStream();

			using (memoryStream)
			{
#pragma warning disable CA2000 // Dispose objects before losing scope
				var streamContent = new StreamContent(uploadStream ?? memoryStream);
#pragma warning restore CA2000 // Dispose objects before losing scope
				try
				{
					await RunRequest<object>(
						$"{Routes.Transfer}?ticket={HttpUtility.UrlEncode(ticket.FileTicket)}",
						streamContent,
						HttpMethod.Put,
						null,
						false,
						cancellationToken)
						.ConfigureAwait(false);
					streamContent = null;
				}
				finally
				{
					streamContent?.Dispose();
				}
			}
		}

		/// <summary>
		/// Main request method.
		/// </summary>
		/// <typeparam name="TResult">The resulting POCO type.</typeparam>
		/// <param name="route">The route to run.</param>
		/// <param name="content">The <see cref="HttpContent"/> of the request if any.</param>
		/// <param name="method">The method of the request.</param>
		/// <param name="instanceId">The optional instance <see cref="EntityId.Id"/> for the request.</param>
		/// <param name="tokenRefresh">If this is a token refresh operation.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the response on success.</returns>
		protected virtual async Task<TResult> RunRequest<TResult>(
			string route,
			HttpContent? content,
			HttpMethod method,
			long? instanceId,
			bool tokenRefresh,
			CancellationToken cancellationToken)
		{
			if (route == null)
				throw new ArgumentNullException(nameof(route));
			if (method == null)
				throw new ArgumentNullException(nameof(method));
			if (content == null && (method == HttpMethod.Post || method == HttpMethod.Put))
				throw new InvalidOperationException("content cannot be null for POST or PUT!");

			HttpResponseMessage response;
			var fullUri = new Uri(Url, route);
			var serializerSettings = GetSerializerSettings();
			var fileDownload = typeof(TResult) == typeof(Stream);
			using (var request = new HttpRequestMessage(method, fullUri))
			{
				if (content != null)
					request.Content = content;

				try
				{
					var headersToUse = tokenRefresh ? tokenRefreshHeaders! : headers;
					headersToUse.SetRequestHeaders(request.Headers, instanceId);

					if (authless)
						request.Headers.Remove(HeaderNames.Authorization);

					if (fileDownload)
						request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(MediaTypeNames.Application.Octet));

					await Task.WhenAll(requestLoggers.Select(x => x.LogRequest(request, cancellationToken))).ConfigureAwait(false);

					response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
				}
				finally
				{
					// prevent content param from getting disposed
					request.Content = null;
				}
			}

			try
			{
				await Task.WhenAll(requestLoggers.Select(x => x.LogResponse(response, cancellationToken))).ConfigureAwait(false);

				// just stream
				if (fileDownload && response.IsSuccessStatusCode)
					return (TResult)(object)await CachedResponseStream.Create(response).ConfigureAwait(false);
			}
			catch
			{
				response.Dispose();
				throw;
			}

			using (response)
			{
				var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

				if (!response.IsSuccessStatusCode)
				{
					if (!tokenRefresh
						&& response.StatusCode == HttpStatusCode.Unauthorized
						&& await RefreshToken(cancellationToken).ConfigureAwait(false))
						return await RunRequest<TResult>(route, content, method, instanceId, false, cancellationToken).ConfigureAwait(false);
					HandleBadResponse(response, json);
				}

				if (String.IsNullOrWhiteSpace(json))
					json = JsonConvert.SerializeObject(new object());

				try
				{
					var result = JsonConvert.DeserializeObject<TResult>(json, serializerSettings);
					return result!;
				}
				catch (JsonException)
				{
					throw new UnrecognizedResponseException(response);
				}
			}
		}

		/// <summary>
		/// Attempt to refresh the bearer token in the <see cref="headers"/>.
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in <see langword="true"/> if the refresh was successful, <see langword="false"/> otherwise.</returns>
		async Task<bool> RefreshToken(CancellationToken cancellationToken)
		{
			if (tokenRefreshHeaders == null)
				return false;

			var startingToken = headers.Token;
			await semaphoreSlim.WaitAsync(cancellationToken).ConfigureAwait(false);
			try
			{
				if (startingToken != headers.Token)
					return true;

				var token = await RunRequest<object, TokenResponse>(Routes.Root, new object(), HttpMethod.Post, null, true, cancellationToken).ConfigureAwait(false);
				headers = new ApiHeaders(headers.UserAgent!, token.Bearer!);
			}
			catch (ClientException)
			{
				return false;
			}
			finally
			{
				semaphoreSlim.Release();
			}

			return true;
		}

		/// <summary>
		/// Main request method.
		/// </summary>
		/// <typeparam name="TBody">The body <see cref="Type"/>.</typeparam>
		/// <typeparam name="TResult">The resulting POCO type.</typeparam>
		/// <param name="route">The route to run.</param>
		/// <param name="body">The body of the request.</param>
		/// <param name="method">The method of the request.</param>
		/// <param name="instanceId">The optional instance <see cref="EntityId.Id"/> for the request.</param>
		/// <param name="tokenRefresh">If this is a token refresh operation.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the response on success.</returns>
		async Task<TResult> RunRequest<TBody, TResult>(
			string route,
			TBody? body,
			HttpMethod method,
			long? instanceId,
			bool tokenRefresh,
			CancellationToken cancellationToken)
			where TBody : class
		{
			HttpContent? content = null;
			if (body != null)
				content = new StringContent(
					JsonConvert.SerializeObject(body, typeof(TBody), Formatting.None, GetSerializerSettings()),
					Encoding.UTF8,
					ApiHeaders.ApplicationJsonMime);

			using (content)
				return await RunRequest<TResult>(
					route,
					content,
					method,
					instanceId,
					tokenRefresh,
					cancellationToken)
					.ConfigureAwait(false);
		}
	}
}
