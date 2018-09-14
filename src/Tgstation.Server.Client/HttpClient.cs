using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Tgstation.Server.Client
{
	/// <inheritdoc />
	sealed class HttpClient : IHttpClient
	{
		/// <inheritdoc />
		public TimeSpan Timeout
		{
			get => httpClient.Timeout;
			set => httpClient.Timeout = value;
		}

		/// <summary>
		/// The real <see cref="System.Net.Http.HttpClient"/>
		/// </summary>
		readonly System.Net.Http.HttpClient httpClient;

		/// <summary>
		/// Construct an <see cref="HttpClient"/>
		/// </summary>
		public HttpClient()
		{
			httpClient = new System.Net.Http.HttpClient();
		}

		/// <inheritdoc />
		public void Dispose() => httpClient.Dispose();

		/// <inheritdoc />
		public Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => httpClient.SendAsync(request, cancellationToken);
	}
}
