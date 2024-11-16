#if NETSTANDARD2_0_OR_GREATER
using System;
using System.Net.Http.Headers;

namespace Tgstation.Server.Common.Http
{
	/// <summary>
	/// <see cref="IAbstractHttpClientFactory"/> that creates <see cref="HttpClient"/>s.
	/// </summary>
	public sealed class HttpClientFactory : IAbstractHttpClientFactory
	{
		/// <inheritdoc />
		public IHttpClient CreateClient()
		{
			var client = new HttpClient();
			try
			{
				client.DefaultRequestHeaders.UserAgent.Add(userAgent);
				return client;
			}
			catch
			{
				client.Dispose();
				throw;
			}
		}

		/// <summary>
		/// The <see cref="ProductInfoHeaderValue"/> used as created client's User-Agent header on request.
		/// </summary>
		readonly ProductInfoHeaderValue userAgent;

		/// <summary>
		/// Initializes a new instance of the <see cref="HttpClientFactory"/> class.
		/// </summary>
		/// <param name="userAgent">The value of <see cref="userAgent"/>.</param>
		public HttpClientFactory(ProductInfoHeaderValue userAgent)
		{
			this.userAgent = userAgent ?? throw new ArgumentNullException(nameof(userAgent));
		}
	}
}
#endif
