using System;

using Tgstation.Server.Api;
using Tgstation.Server.Client;
using Tgstation.Server.Common;

namespace Tgstation.Server.Tests.Live
{
	sealed class RateLimitRetryingApiClientFactory : IApiClientFactory
	{
		public IApiClient CreateApiClient(Uri url, ApiHeaders apiHeaders, ApiHeaders tokenRefreshHeaders, bool authless)
			=> new RateLimitRetryingApiClient(
				new HttpClient(),
				url,
				apiHeaders,
				tokenRefreshHeaders,
				authless);
	}
}
