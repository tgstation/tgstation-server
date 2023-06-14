using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace Tgstation.Server.Common.Http
{
	/// <inheritdoc />
	public sealed class HttpClient : IHttpClient
	{
		/// <inheritdoc />
		public TimeSpan Timeout
		{
			get => httpClient.Timeout;
			set => httpClient.Timeout = value;
		}

		/// <inheritdoc />
		public HttpRequestHeaders DefaultRequestHeaders => httpClient.DefaultRequestHeaders;

		/// <summary>
		/// The real <see cref="System.Net.Http.HttpClient"/>.
		/// </summary>
		readonly System.Net.Http.HttpClient httpClient;

		/// <summary>
		/// Initializes a new instance of the <see cref="HttpClient"/> class.
		/// </summary>
		/// <param name="implementation">The <see cref="System.Net.Http.HttpClient"/> to wrap.</param>
		public HttpClient(System.Net.Http.HttpClient implementation)
		{
			httpClient = implementation ?? throw new ArgumentNullException(nameof(implementation));
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="HttpClient"/> class.
		/// </summary>
		public HttpClient()
			: this(new System.Net.Http.HttpClient())
		{
		}

		/// <inheritdoc />
		public void Dispose() => httpClient.Dispose();

		/// <inheritdoc />
		public Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, HttpCompletionOption completionOption, CancellationToken cancellationToken)
			=> httpClient.SendAsync(request, completionOption, cancellationToken);
	}
}
