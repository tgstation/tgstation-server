using System;
using System.Net.Http;

using Tgstation.Server.Api;
using Tgstation.Server.Client;

namespace Tgstation.Server.Tests.Live
{
	sealed class RateLimitRetryingApiClientFactory : IApiClientFactory
	{
		public IApiClient CreateApiClient(
			Uri url,
			ApiHeaders apiHeaders,
			ApiHeaders tokenRefreshHeaders,
			bool authless)
			=> new RateLimitRetryingApiClient(
				new HttpClient(),
				url,
				apiHeaders,
				tokenRefreshHeaders,
				authless);

		/// <inheritdoc />
		public IApiClient CreateApiClient(
			Uri url,
			ApiHeaders apiHeaders,
			ApiHeaders tokenRefreshHeaders,
			HttpMessageHandler handler,
			bool disposeHandler,
			bool authless) => new RateLimitRetryingApiClient(
				new HttpClient(handler, disposeHandler),
				url,
				apiHeaders,
				tokenRefreshHeaders,
				authless);
	}
}
