using System;

using Tgstation.Server.Api;
using Tgstation.Server.Api.Models.Response;
using Tgstation.Server.Client;
using Tgstation.Server.Common.Http;

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
	}
}
