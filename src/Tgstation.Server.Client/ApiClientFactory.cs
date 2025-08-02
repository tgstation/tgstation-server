using System;
using System.Net.Http;

using Tgstation.Server.Api;

namespace Tgstation.Server.Client
{
	/// <inheritdoc />
	sealed class ApiClientFactory : IApiClientFactory
	{
		/// <inheritdoc />
		public IApiClient CreateApiClient(
			Uri url,
			ApiHeaders apiHeaders,
			ApiHeaders? tokenRefreshHeaders,
			bool authless) => new ApiClient(
				new HttpClient(),
				url,
				apiHeaders,
				tokenRefreshHeaders,
				authless);

		/// <inheritdoc />
		public IApiClient CreateApiClient(
			Uri url,
			ApiHeaders apiHeaders,
			ApiHeaders? tokenRefreshHeaders,
			HttpMessageHandler handler,
			bool disposeHandler,
			bool authless) => new ApiClient(
				new HttpClient(handler, disposeHandler),
				url,
				apiHeaders,
				tokenRefreshHeaders,
				authless);
	}
}
