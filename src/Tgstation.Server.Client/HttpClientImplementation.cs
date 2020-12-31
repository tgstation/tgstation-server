using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Tgstation.Server.Client
{
	/// <inheritdoc />
	sealed class HttpClientImplementation : IHttpClient
	{
		/// <inheritdoc />
		public TimeSpan Timeout
		{
			get => httpClient.Timeout;
			set => httpClient.Timeout = value;
		}

		/// <summary>
		/// The real <see cref="HttpClient"/>
		/// </summary>
		readonly HttpClient httpClient;

		/// <summary>
		/// Construct an <see cref="HttpClientImplementation"/>
		/// </summary>
		public HttpClientImplementation()
		{
			httpClient = new HttpClient();
		}

		/// <inheritdoc />
		public void Dispose() => httpClient.Dispose();

		/// <inheritdoc />
		public Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => httpClient.SendAsync(request, cancellationToken);
	}
}
