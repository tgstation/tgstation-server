using System;

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
				new HttpClientImplementation(),
				url,
				apiHeaders,
				tokenRefreshHeaders,
				authless);
	}
}
