using System;

using Tgstation.Server.Api;
using Tgstation.Server.Common;

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
	}
}
