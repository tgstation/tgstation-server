using System;

using Tgstation.Server.Api;

namespace Tgstation.Server.Client
{
	/// <summary>
	/// For creating <see cref="IApiClient"/>s.
	/// </summary>
	interface IApiClientFactory
	{
		/// <summary>
		/// Create an <see cref="IApiClient"/>.
		/// </summary>
		/// <param name="url">The base <see cref="Uri"/>.</param>
		/// <param name="apiHeaders">The <see cref="ApiHeaders"/> for the <see cref="IApiClient"/>.</param>
		/// <param name="tokenRefreshHeaders">The <see cref="ApiHeaders"/> to use to generate a new <see cref="Api.Models.Response.TokenResponse"/>.</param>
		/// <param name="authless">If there should be no authentication performed.</param>
		/// <returns>A new <see cref="IApiClient"/>.</returns>
		IApiClient CreateApiClient(Uri url, ApiHeaders apiHeaders, ApiHeaders? tokenRefreshHeaders, bool authless);
	}
}
