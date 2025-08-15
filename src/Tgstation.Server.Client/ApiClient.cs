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

using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

using Tgstation.Server.Api;
using Tgstation.Server.Api.Models;
using Tgstation.Server.Api.Models.Response;
using Tgstation.Server.Client.Extensions;
using Tgstation.Server.Common.Extensions;
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
		static readonly HttpMethod HttpPatch = new("PATCH");

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
		/// The <see cref="JsonSerializerSettings"/> to use.
		/// </summary>
		static readonly JsonSerializerSettings SerializerSettings = new()
		{
			ContractResolver = new CamelCasePropertyNamesContractResolver(),
			Converters = new[]
			{
				new VersionConverter(),
			},
		};

		/// <summary>
		/// The <see cref="HttpClient"/> for the <see cref="ApiClient"/>.
		/// </summary>
		readonly HttpClient httpClient;

		/// <summary>
		/// The <see cref="IRequestLogger"/>s used by the <see cref="ApiClient"/>.
		/// </summary>
		readonly List<IRequestLogger> requestLoggers;

		/// <summary>
		/// List of <see cref="HubConnection"/>s created by the <see cref="ApiClient"/>.
		/// </summary>
		readonly List<HubConnection> hubConnections;

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
		/// If the <see cref="ApiClient"/> is disposed.
		/// </summary>
		bool disposed;

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
				errorMessage = JsonConvert.DeserializeObject<ErrorMessageResponse>(json, SerializerSettings);
			}
			catch (JsonException)
			{
			}

#pragma warning disable IDE0010 // Add missing cases
			switch (response.StatusCode)
#pragma warning restore IDE0010 // Add missing cases
			{
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
					if (errorMessage?.ErrorCode == ErrorCode.ApiMismatch)
						throw new VersionMismatchException(errorMessage, response);

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
		public ApiClient(
			HttpClient httpClient,
			Uri url,
			ApiHeaders apiHeaders,
			ApiHeaders? tokenRefreshHeaders,
			bool authless)
		{
			this.httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
			Url = url ?? throw new ArgumentNullException(nameof(url));
			headers = apiHeaders ?? throw new ArgumentNullException(nameof(apiHeaders));
			this.tokenRefreshHeaders = tokenRefreshHeaders;
			this.authless = authless;

			requestLoggers = new List<IRequestLogger>();
			hubConnections = new List<HubConnection>();
			semaphoreSlim = new SemaphoreSlim(1);
		}

		/// <inheritdoc />
		public async ValueTask DisposeAsync()
		{
			List<HubConnection> localHubConnections;
			lock (hubConnections)
			{
				if (disposed)
					return;

				disposed = true;

				localHubConnections = [.. hubConnections];
				hubConnections.Clear();
			}

			await ValueTaskExtensions.WhenAll(hubConnections.Select(connection => connection.DisposeAsync()));

			httpClient.Dispose();
			semaphoreSlim.Dispose();
		}

		/// <inheritdoc />
		public ValueTask<TResult> Create<TResult>(string route, CancellationToken cancellationToken)
			=> RunRequest<object, TResult>(route, new object(), HttpMethod.Post, null, false, cancellationToken);

		/// <inheritdoc />
		public ValueTask<TResult> Read<TResult>(string route, CancellationToken cancellationToken)
			=> RunRequest<object, TResult>(route, null, HttpMethod.Get, null, false, cancellationToken);

		/// <inheritdoc />
		public ValueTask<TResult> Update<TResult>(string route, CancellationToken cancellationToken)
			=> RunRequest<object, TResult>(route, new object(), HttpMethod.Post, null, false, cancellationToken);

		/// <inheritdoc />
		public ValueTask<TResult> Update<TBody, TResult>(string route, TBody body, CancellationToken cancellationToken)
			where TBody : class
			=> RunRequest<TBody, TResult>(route, body, HttpMethod.Post, null, false, cancellationToken);

		/// <inheritdoc />
		public ValueTask Patch(string route, CancellationToken cancellationToken) => RunRequest(route, HttpPatch, null, false, cancellationToken);

		/// <inheritdoc />
		public ValueTask Update<TBody>(string route, TBody body, CancellationToken cancellationToken)
			where TBody : class
			=> RunResultlessRequest(route, body, HttpMethod.Post, null, false, cancellationToken);

		/// <inheritdoc />
		public ValueTask<TResult> Create<TBody, TResult>(string route, TBody body, CancellationToken cancellationToken)
			where TBody : class
			=> RunRequest<TBody, TResult>(route, body, HttpMethod.Post, null, false, cancellationToken);

		/// <inheritdoc />
		public ValueTask Delete(string route, CancellationToken cancellationToken)
			=> RunRequest(route, HttpMethod.Delete, null, false, cancellationToken);

		/// <inheritdoc />
		public ValueTask<TResult> Create<TBody, TResult>(string route, TBody body, long instanceId, CancellationToken cancellationToken)
			where TBody : class
			=> RunRequest<TBody, TResult>(route, body, HttpMethod.Post, instanceId, false, cancellationToken);

		/// <inheritdoc />
		public ValueTask<TResult> Read<TResult>(string route, long instanceId, CancellationToken cancellationToken)
			=> RunRequest<TResult>(route, null, HttpMethod.Get, instanceId, false, cancellationToken);

		/// <inheritdoc />
		public ValueTask<TResult> Update<TBody, TResult>(string route, TBody body, long instanceId, CancellationToken cancellationToken)
			where TBody : class
			=> RunRequest<TBody, TResult>(route, body, HttpMethod.Post, instanceId, false, cancellationToken);

		/// <inheritdoc />
		public ValueTask Delete(string route, long instanceId, CancellationToken cancellationToken)
			=> RunRequest(route, HttpMethod.Delete, instanceId, false, cancellationToken);

		/// <inheritdoc />
		public ValueTask Delete<TBody>(string route, TBody body, long instanceId, CancellationToken cancellationToken)
			where TBody : class
			=> RunResultlessRequest(route, body, HttpMethod.Delete, instanceId, false, cancellationToken);

		/// <inheritdoc />
		public ValueTask<TResult> Delete<TResult>(string route, long instanceId, CancellationToken cancellationToken)
			=> RunRequest<TResult>(route, null, HttpMethod.Delete, instanceId, false, cancellationToken);

		/// <inheritdoc />
		public ValueTask<TResult> Delete<TBody, TResult>(string route, TBody body, long instanceId, CancellationToken cancellationToken)
			where TBody : class
			=> RunRequest<TBody, TResult>(route, body, HttpMethod.Delete, instanceId, false, cancellationToken);

		/// <inheritdoc />
		public ValueTask<TResult> Create<TResult>(string route, long instanceId, CancellationToken cancellationToken)
			=> RunRequest<object, TResult>(route, new object(), HttpMethod.Post, instanceId, false, cancellationToken);

		/// <inheritdoc />
		public ValueTask<TResult> Patch<TResult>(string route, long instanceId, CancellationToken cancellationToken)
			=> RunRequest<object, TResult>(route, new object(), HttpPatch, instanceId, false, cancellationToken);

		/// <inheritdoc />
		public void AddRequestLogger(IRequestLogger requestLogger) => requestLoggers.Add(requestLogger ?? throw new ArgumentNullException(nameof(requestLogger)));

		/// <inheritdoc />
		public ValueTask<Stream> Download(FileTicketResponse ticket, CancellationToken cancellationToken)
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
		public async ValueTask Upload(FileTicketResponse ticket, Stream? uploadStream, CancellationToken cancellationToken)
		{
			if (ticket == null)
				throw new ArgumentNullException(nameof(ticket));

			MemoryStream? memoryStream = null;
			if (uploadStream == null)
				memoryStream = new MemoryStream();

			using (memoryStream)
			{
				var streamContent = new StreamContent(uploadStream ?? memoryStream);
				try
				{
					await RunRequest<object>(
						$"{Routes.Transfer}?ticket={HttpUtility.UrlEncode(ticket.FileTicket)}",
						streamContent,
						HttpMethod.Post,
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
		/// Attempt to refresh the stored Bearer token in <see cref="Headers"/>.
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in <see langword="true"/> if the refresh was successful, <see langword="false"/> if a refresh is unable to be performed.</returns>
		public async ValueTask<bool> RefreshToken(CancellationToken cancellationToken)
		{
			if (tokenRefreshHeaders == null)
				return false;

			var startingToken = headers.Token;
			await semaphoreSlim.WaitAsync(cancellationToken).ConfigureAwait(false);
			try
			{
				if (startingToken != headers.Token)
					return true;

				var token = await RunRequest<object, TokenResponse>(Routes.ApiRoot, new object(), HttpMethod.Post, null, true, cancellationToken).ConfigureAwait(false);
				headers = new ApiHeaders(headers.UserAgent!, token);
			}
			finally
			{
				semaphoreSlim.Release();
			}

			return true;
		}

		/// <inheritdoc />
		public async ValueTask<IAsyncDisposable> CreateHubConnection<THubImplementation>(
			THubImplementation hubImplementation,
			IRetryPolicy? retryPolicy,
			Action<ILoggingBuilder>? loggingConfigureAction,
			CancellationToken cancellationToken)
			where THubImplementation : class
		{
			if (hubImplementation == null)
				throw new ArgumentNullException(nameof(hubImplementation));

			retryPolicy ??= new InfiniteThirtySecondMaxRetryPolicy();

			var wrappedPolicy = new ApiClientTokenRefreshRetryPolicy(this, retryPolicy);

			HubConnection? hubConnection = null;
			var hubConnectionBuilder = new HubConnectionBuilder()
				.AddNewtonsoftJsonProtocol(options =>
				{
					options.PayloadSerializerSettings = SerializerSettings;
				})
				.WithAutomaticReconnect(wrappedPolicy)
				.WithUrl(
					new Uri(Url, Routes.JobsHub),
					HttpTransportType.ServerSentEvents,
					options =>
					{
						options.AccessTokenProvider = async () =>
						{
							// DCT: None available.
							if (Headers.Token == null
								|| (Headers.Token.ParseJwt().ValidTo <= DateTime.UtcNow
								&& !await RefreshToken(CancellationToken.None)))
							{
								_ = hubConnection!.StopAsync(); // DCT: None available.
								return null;
							}

							return Headers.Token.Bearer;
						};

						options.CloseTimeout = Timeout;

						Headers.SetHubConnectionHeaders(options.Headers);
					});

			if (loggingConfigureAction != null)
				hubConnectionBuilder.ConfigureLogging(loggingConfigureAction);

			async ValueTask<HubConnection> AttemptConnect()
			{
				hubConnection = hubConnectionBuilder.Build();
				try
				{
					hubConnection.Closed += async (error) =>
					{
						if (error is HttpRequestException httpRequestException)
						{
							// .StatusCode isn't in netstandard but fuck the police
							var property = error.GetType().GetProperty("StatusCode");
							if (property != null)
							{
								var statusCode = (HttpStatusCode?)property.GetValue(error);
								if (statusCode == HttpStatusCode.Unauthorized
									&& !await RefreshToken(CancellationToken.None))
									_ = hubConnection!.StopAsync();
							}
						}
					};

					hubConnection.ProxyOn(hubImplementation);

					Task startTask;
					lock (hubConnections)
					{
						if (disposed)
							throw new ObjectDisposedException(nameof(ApiClient));

						hubConnections.Add(hubConnection);
						startTask = hubConnection.StartAsync(cancellationToken);
					}

					await startTask;

					return hubConnection;
				}
				catch
				{
					bool needsDispose;
					lock (hubConnections)
						needsDispose = hubConnections.Remove(hubConnection);

					if (needsDispose)
						await hubConnection.DisposeAsync();
					throw;
				}
			}

			return await WrapHubInitialConnectAuthRefresh(AttemptConnect, cancellationToken);
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
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in the response on success.</returns>
#pragma warning disable CA1506 // TODO: Decomplexify
		protected virtual async ValueTask<TResult> RunRequest<TResult>(
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

			if (disposed)
				throw new ObjectDisposedException(nameof(ApiClient));

			HttpResponseMessage response;
			var fullUri = new Uri(Url, route);
			var serializerSettings = SerializerSettings;
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
					else
					{
						var bearer = headersToUse.Token?.Bearer;
						if (bearer != null)
						{
							var parsed = headersToUse.Token!.ParseJwt();
							var nbf = parsed.ValidFrom;
							var now = DateTime.UtcNow;
							if (nbf >= now)
							{
								var delay = (nbf - now).Add(TimeSpan.FromMilliseconds(1));
								await Task.Delay(delay, cancellationToken);
							}
						}
					}

					if (fileDownload)
						request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(MediaTypeNames.Application.Octet));

					if (method == HttpMethod.Put)
						request.Headers.Warning.Add(new WarningHeaderValue(299, "-", "HttpMethod.Put is depreciated and will be replaced with HttpMethod.Post in next major release."));

					await ValueTaskExtensions.WhenAll(requestLoggers.Select(x => x.LogRequest(request, cancellationToken))).ConfigureAwait(false);

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
				await ValueTaskExtensions.WhenAll(requestLoggers.Select(x => x.LogResponse(response, cancellationToken))).ConfigureAwait(false);

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
#pragma warning restore CA1506

		/// <summary>
		/// Wrap a hub connection attempt via a <paramref name="connectFunc"/> with proper token refreshing.
		/// </summary>
		/// <param name="connectFunc">The <see cref="HubConnection"/> <see cref="Func{TResult}"/>.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in the connected <see cref="HubConnection"/>.</returns>
		async ValueTask<HubConnection> WrapHubInitialConnectAuthRefresh(Func<ValueTask<HubConnection>> connectFunc, CancellationToken cancellationToken)
		{
			try
			{
				return await connectFunc();
			}
			catch (HttpRequestException ex)
			{
				// status code is not in netstandard
				var propertyInfo = ex.GetType().GetProperty("StatusCode");
				if (propertyInfo != null)
				{
					var statusCode = (HttpStatusCode)propertyInfo.GetValue(ex);
					if (statusCode != HttpStatusCode.Unauthorized)
						throw;
				}

				await RefreshToken(cancellationToken);

				return await connectFunc();
			}
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
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in the response on success.</returns>
		async ValueTask<TResult> RunRequest<TBody, TResult>(
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
					JsonConvert.SerializeObject(body, typeof(TBody), Formatting.None, SerializerSettings),
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

		/// <summary>
		/// Main request method.
		/// </summary>
		/// <typeparam name="TBody">The body <see cref="Type"/>.</typeparam>
		/// <param name="route">The route to run.</param>
		/// <param name="body">The body of the request.</param>
		/// <param name="method">The method of the request.</param>
		/// <param name="instanceId">The optional instance <see cref="EntityId.Id"/> for the request.</param>
		/// <param name="tokenRefresh">If this is a token refresh operation.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in the response on success.</returns>
		async ValueTask RunResultlessRequest<TBody>(
			string route,
			TBody? body,
			HttpMethod method,
			long? instanceId,
			bool tokenRefresh,
			CancellationToken cancellationToken)
			where TBody : class
			=> await RunRequest<TBody, object>(
				route,
				body,
				method,
				instanceId,
				tokenRefresh,
				cancellationToken);

		/// <summary>
		/// Main request method.
		/// </summary>
		/// <param name="route">The route to run.</param>
		/// <param name="method">The method of the request.</param>
		/// <param name="instanceId">The optional instance <see cref="EntityId.Id"/> for the request.</param>
		/// <param name="tokenRefresh">If this is a token refresh operation.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in the response on success.</returns>
		ValueTask RunRequest(
			string route,
			HttpMethod method,
			long? instanceId,
			bool tokenRefresh,
			CancellationToken cancellationToken)
			=> RunResultlessRequest<object>(
				route,
				null,
				method,
				instanceId,
				tokenRefresh,
				cancellationToken);
	}
}
