using System;
using System.Net.Http;

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
		IApiClient CreateApiClient(
			Uri url,
			ApiHeaders apiHeaders,
			ApiHeaders? tokenRefreshHeaders,
			bool authless);

		/// <summary>
		/// Create an <see cref="IApiClient"/>.
		/// </summary>
		/// <param name="url">The base <see cref="Uri"/>.</param>
		/// <param name="apiHeaders">The <see cref="ApiHeaders"/> for the <see cref="IApiClient"/>.</param>
		/// <param name="tokenRefreshHeaders">The <see cref="ApiHeaders"/> to use to generate a new <see cref="Api.Models.Response.TokenResponse"/>.</param>
		/// <param name="handler">The <see cref="HttpMessageHandler"/> to use with the internal <see cref="HttpClient"/>.</param>
		/// <param name="disposeHandler">If <paramref name="handler"/> should be disposed with the created <see cref="IApiClient"/>.</param>
		/// <param name="authless">If there should be no authentication performed.</param>
		/// <returns>A new <see cref="IApiClient"/>.</returns>
		public IApiClient CreateApiClient(
			Uri url,
			ApiHeaders apiHeaders,
			ApiHeaders? tokenRefreshHeaders,
			HttpMessageHandler handler,
			bool disposeHandler,
			bool authless);
	}
}
