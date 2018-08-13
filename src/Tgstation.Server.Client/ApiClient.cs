using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Api;

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