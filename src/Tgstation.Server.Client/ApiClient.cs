using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Api;
using Tgstation.Server.Api.Models;

namespace Tgstation.Server.Client
{
	/// <inheritdoc />
	sealed class ApiClient : IApiClient
	{
		/// <inheritdoc />
		public Uri Url { get; }

		/// <inheritdoc />
		public ApiHeaders Headers { get; }

		/// <inheritdoc />
		public TimeSpan Timeout
		{
			get => httpClient.Timeout;
			set => httpClient.Timeout = value;
		}

		/// <summary>
		/// The <see cref="HttpClient"/> for the <see cref="ApiClient"/>
		/// </summary>
		readonly IHttpClient httpClient;

		/// <summary>
		/// The <see cref="IRequestLogger"/>s used by the <see cref="ApiClient"/>
		/// </summary>
		readonly List<IRequestLogger> requestLoggers;

		/// <summary>
		/// Construct an <see cref="ApiClient"/>
		/// </summary>
		/// <param name="httpClient">The value of <see cref="httpClient"/></param>
		/// <param name="url">The value of <see cref="Url"/></param>
		/// <param name="apiHeaders">The value of <see cref="ApiHeaders"/></param>
		public ApiClient(IHttpClient httpClient, Uri url, ApiHeaders apiHeaders)
		{
			this.httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
			Url = url ?? throw new ArgumentNullException(nameof(url));
			Headers = apiHeaders ?? throw new ArgumentNullException(nameof(apiHeaders));
			
			requestLoggers = new List<IRequestLogger>();
		}

		/// <inheritdoc />
		public void Dispose() => httpClient.Dispose();

		/// <summary>
		/// Main request method
		/// </summary>
		/// <param name="route">The route to run</param>
		/// <param name="body">The body of the request</param>
		/// <param name="method">The method of the request</param>
		/// <param name="instanceId">The optional <see cref="Api.Models.Instance.Id"/> for the request</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the response on success</returns>
		async Task<TResult> RunRequest<TResult>(string route, object body, HttpMethod method, long? instanceId, CancellationToken cancellationToken)
		{
			if (route == null)
				throw new ArgumentNullException(nameof(route));
			if (method == null)
				throw new ArgumentNullException(nameof(method));
			if (body == null && (method == HttpMethod.Post || method == HttpMethod.Put))
				throw new InvalidOperationException("Body cannot be null for POST or PUT!");

			var fullUri = new Uri(Url, route);

			var message = new HttpRequestMessage(method, fullUri);

			var serializerSettings = new JsonSerializerSettings
			{
				ContractResolver = new CamelCasePropertyNamesContractResolver()
			};

			if (body != null)
				message.Content = new StringContent(JsonConvert.SerializeObject(body, serializerSettings), Encoding.UTF8, ApiHeaders.ApplicationJson);

			Headers.SetRequestHeaders(message.Headers, instanceId);

			await Task.WhenAll(requestLoggers.Select(x => x.LogRequest(message, cancellationToken))).ConfigureAwait(false);

			var response = await httpClient.SendAsync(message, cancellationToken).ConfigureAwait(false);

			await Task.WhenAll(requestLoggers.Select(x => x.LogResponse(response, cancellationToken))).ConfigureAwait(false);

			var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

			if (!response.IsSuccessStatusCode)
			{
				ErrorMessage errorMessage = null;
				try
				{
					//check if json serializes to an error message
					errorMessage = JsonConvert.DeserializeObject<ErrorMessage>(json, serializerSettings);
				}
				catch (JsonException) { }

				const string BadSpecExtension = " This is not part of TGS4 communication specification and should be reported if it was returned from a TGS4 server!";

				switch (response.StatusCode)
				{
					case HttpStatusCode.UpgradeRequired:
						throw new ApiMismatchException(errorMessage ?? new ErrorMessage
						{
							Message = "API Mismatch but no current API version provided!" + BadSpecExtension,
							SeverApiVersion = null
						});
					case HttpStatusCode.Unauthorized:
						throw new UnauthorizedException();
					case HttpStatusCode.RequestTimeout:
						throw new RequestTimeoutException();
					case HttpStatusCode.Forbidden:
						throw new InsufficientPermissionsException();
					case HttpStatusCode.ServiceUnavailable:
						throw new ServiceUnavailableException();
					case HttpStatusCode.Gone:
						errorMessage = errorMessage ?? new ErrorMessage
						{
							Message = "The requested resource could not be found!",
							SeverApiVersion = null
						};
						goto case HttpStatusCode.Conflict;
					case HttpStatusCode.NotFound:	//our fault somehow
						errorMessage = errorMessage ?? new ErrorMessage
						{
							Message = "This is not a valid route!" + BadSpecExtension,
							SeverApiVersion = null
						};
						goto case HttpStatusCode.Conflict;
					case HttpStatusCode.Conflict:
						throw new ConflictException(errorMessage ?? new ErrorMessage
						{
							Message = "An undescribed conflict occurred!" + BadSpecExtension,
							SeverApiVersion = null
						}, response.StatusCode);
					case HttpStatusCode.NotImplemented:
					case (HttpStatusCode)422:   //unprocessable entity
						throw new MethodNotSupportedException();
					case HttpStatusCode.InternalServerError:
						//response
						throw new ServerErrorException(json);   //json is html
					case (HttpStatusCode)429:   //rate limited
						response.Headers.TryGetValues("Retry-After", out var values);
						throw new RateLimitException(values?.FirstOrDefault());
					default:
						throw new ApiConflictException(errorMessage, response.StatusCode);
				}
			}

			if (String.IsNullOrWhiteSpace(json))
				json = JsonConvert.SerializeObject(new object());

			return JsonConvert.DeserializeObject<TResult>(json, serializerSettings);
		}

		/// <inheritdoc />
		public Task<TResult> Create<TResult>(string route, CancellationToken cancellationToken) => RunRequest<TResult>(route, new object(), HttpMethod.Put, null, cancellationToken);

		/// <inheritdoc />
		public Task<TResult> Read<TResult>(string route, CancellationToken cancellationToken) => RunRequest<TResult>(route, null, HttpMethod.Get, null, cancellationToken);

		/// <inheritdoc />
		public Task<TResult> Update<TResult>(string route, CancellationToken cancellationToken) => RunRequest<TResult>(route, new object(), HttpMethod.Post, null, cancellationToken);

		/// <inheritdoc />
		public Task<TResult> Update<TBody, TResult>(string route, TBody body, CancellationToken cancellationToken) => RunRequest<TResult>(route, body, HttpMethod.Post, null, cancellationToken);

		/// <inheritdoc />
		public Task Update<TBody>(string route, TBody body, CancellationToken cancellationToken) => RunRequest<object>(route, body, HttpMethod.Post, null, cancellationToken);

		/// <inheritdoc />
		public Task<TResult> Create<TBody, TResult>(string route, TBody body, CancellationToken cancellationToken) => RunRequest<TResult>(route, body, HttpMethod.Put, null, cancellationToken);

		/// <inheritdoc />
		public Task Delete(string route, CancellationToken cancellationToken) => RunRequest<object>(route, null, HttpMethod.Delete, null, cancellationToken);

		/// <inheritdoc />
		public Task<TResult> Create<TBody, TResult>(string route, TBody body, long instanceId, CancellationToken cancellationToken) => RunRequest<TResult>(route, body, HttpMethod.Put, instanceId, cancellationToken);

		/// <inheritdoc />
		public Task<TResult> Read<TResult>(string route, long instanceId, CancellationToken cancellationToken) => RunRequest<TResult>(route, null, HttpMethod.Get, instanceId, cancellationToken);

		/// <inheritdoc />
		public Task<TResult> Update<TBody, TResult>(string route, TBody body, long instanceId, CancellationToken cancellationToken) => RunRequest<TResult>(route, body, HttpMethod.Post, instanceId, cancellationToken);

		/// <inheritdoc />
		public Task Delete(string route, long instanceId, CancellationToken cancellationToken) => RunRequest<object>(route, null, HttpMethod.Delete, instanceId, cancellationToken);

		/// <inheritdoc />
		public Task Delete<TBody>(string route, TBody body, long instanceId, CancellationToken cancellationToken) => RunRequest<object>(route, body, HttpMethod.Delete, instanceId, cancellationToken);

		/// <inheritdoc />
		public Task<TResult> Delete<TResult>(string route, long instanceId, CancellationToken cancellationToken) => RunRequest<TResult>(route, null, HttpMethod.Delete, instanceId, cancellationToken);

		/// <inheritdoc />
		public Task<TResult> Create<TResult>(string route, long instanceId, CancellationToken cancellationToken) => RunRequest<TResult>(route, new object(), HttpMethod.Put, instanceId, cancellationToken);

		/// <inheritdoc />
		public void AddRequestLogger(IRequestLogger requestLogger) => requestLoggers.Add(requestLogger ?? throw new ArgumentNullException(nameof(requestLogger)));
	}
}