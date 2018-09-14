using System;
using Tgstation.Server.Api;

namespace Tgstation.Server.Client
{
	/// <inheritdoc />
	sealed class ApiClientFactory : IApiClientFactory
	{
		/// <inheritdoc />
		public IApiClient CreateApiClient(Uri url, ApiHeaders apiHeaders) => new ApiClient(new HttpClient(), url, apiHeaders);
	}
}