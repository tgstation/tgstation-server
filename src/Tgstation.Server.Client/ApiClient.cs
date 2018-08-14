using Newtonsoft.Json;
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
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
		/// The <see cref="HttpClient"/> for the <see cref="IApiClient"/>
		/// </summary>
		readonly HttpClient httpClient;

		/// <summary>
		/// Construct an <see cref="ApiClient"/>
		/// </summary>
		/// <param name="url">The value of <see cref="Url"/></param>
		/// <param name="apiHeaders">The value of <see cref="ApiHeaders"/></param>
		public ApiClient(Uri url, ApiHeaders apiHeaders)
		{
			Url = url ?? throw new ArgumentNullException(nameof(url));
			Headers = apiHeaders ?? throw new ArgumentNullException(nameof(apiHeaders));

			httpClient = new HttpClient();
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

			HttpContent content = null;
			if (body != null)
				content = new StringContent(JsonConvert.SerializeObject(body));

			Task<HttpResponseMessage> task;
			lock (this)
			{
				httpClient.DefaultRequestHeaders.Clear();
				Headers.SetRequestHeaders(httpClient.DefaultRequestHeaders, instanceId);

				if (method == HttpMethod.Get)
					task = httpClient.GetAsync(route);
				else if (method == HttpMethod.Put)
					task = httpClient.PutAsync(fullUri, content, cancellationToken);
				else if (method == HttpMethod.Post)
					task = httpClient.PostAsync(fullUri, content, cancellationToken);
				else if (method == HttpMethod.Delete)
					task = httpClient.DeleteAsync(fullUri, cancellationToken);
				else
					throw new NotSupportedException();
			}

			var response = await task.ConfigureAwait(false);

			var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

			if (!response.IsSuccessStatusCode) {
				ErrorMessage errorMessage = null;
				try
				{
					//check if json serializes to an error message
					errorMessage = JsonConvert.DeserializeObject<ErrorMessage>(json);
				}
				catch (JsonSerializationException) { }

				switch (response.StatusCode)
				{
					case HttpStatusCode.BadRequest:
						//validate our api version is compatible
						if(errorMessage != null && ApiHeaders.CheckCompatibility(errorMessage.SeverApiVersion))
							throw new ApiMismatchException(errorMessage);
						goto default;
					case HttpStatusCode.Unauthorized:
						throw new UnauthorizedException();
					case HttpStatusCode.RequestTimeout:
						throw new RequestTimeoutException();
					case HttpStatusCode.Forbidden:
						throw new InsufficientPermissionsException();
					case HttpStatusCode.Gone:
					case HttpStatusCode.NotFound:
					case HttpStatusCode.Conflict:
						throw new ConflictException(errorMessage, response.StatusCode);
					case HttpStatusCode.NotImplemented:
						throw new MethodNotSupportedException();
					case HttpStatusCode.InternalServerError:
						//response
						throw new ServerErrorException(json);	//json is html
					case (HttpStatusCode)429:   //rate limited
						response.Headers.TryGetValues("Retry-After", out var values);
						throw new RateLimitException(values?.FirstOrDefault());
					default:
						throw new ApiConflictException(errorMessage, response.StatusCode);
				}
			}

			if (String.IsNullOrWhiteSpace(json))
				json = JsonConvert.SerializeObject(new object());

			return JsonConvert.DeserializeObject<TResult>(json);
		}

		/// <inheritdoc />
		public Task<T> Create<T>(string route, CancellationToken cancellationToken)
		{
			throw new NotImplementedException();
		}

		/// <inheritdoc />
		public Task<T> Read<T>(string route, CancellationToken cancellationToken)
		{
			throw new NotImplementedException();
		}

		/// <inheritdoc />
		public Task<TResult> Update<TResult>(string route, CancellationToken cancellationToken) => Update<object, TResult>(route, new object(), cancellationToken);

		/// <inheritdoc />
		public Task<TResult> Update<TBody, TResult>(string route, TBody body, CancellationToken cancellationToken)
		{
			throw new NotImplementedException();
		}

		/// <inheritdoc />
		public Task Update<TBody>(string route, TBody body, CancellationToken cancellationToken)
		{
			throw new NotImplementedException();
		}

		/// <inheritdoc />
		public Task<TResult> Create<TBody, TResult>(string route, TBody body, CancellationToken cancellationToken)
		{
			throw new NotImplementedException();
		}

		/// <inheritdoc />
		public Task Delete(string route, CancellationToken cancellationToken)
		{
			throw new NotImplementedException();
		}

		/// <inheritdoc />
		public Task<TResult> Create<TBody, TResult>(string route, TBody body, long instanceId, CancellationToken cancellationToken)
		{
			throw new NotImplementedException();
		}

		/// <inheritdoc />
		public Task<TResult> Read<TResult>(string route, long instanceId, CancellationToken cancellationToken)
		{
			throw new NotImplementedException();
		}

		/// <inheritdoc />
		public Task<TResult> Update<TBody, TResult>(string route, TBody body, long instanceId, CancellationToken cancellationToken)
		{
			throw new NotImplementedException();
		}

		/// <inheritdoc />
		public Task<TResult> Update<TResult>(string route, long instanceId, CancellationToken cancellationToken)
		{
			throw new NotImplementedException();
		}

		/// <inheritdoc />
		public Task Update<TBody>(string route, TBody body, long instanceId, CancellationToken cancellationToken)
		{
			throw new NotImplementedException();
		}

		/// <inheritdoc />
		public Task Delete(string route, long instanceId, CancellationToken cancellationToken)
		{
			throw new NotImplementedException();
		}

		/// <inheritdoc />
		public Task<TResult> Create<TResult>(string route, long instanceId, CancellationToken cancellationToken)
		{
			throw new NotImplementedException();
		}
	}
}