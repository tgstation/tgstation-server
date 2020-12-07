using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Api;
using Tgstation.Server.Api.Models;
using Tgstation.Server.Host.Configuration;
using Tgstation.Server.Host.System;

namespace Tgstation.Server.Host.Security.OAuth
{
	/// <summary>
	/// <see cref="IOAuthValidator"/> for generic OAuth2 endpoints.
	/// </summary>
	abstract class GenericOAuthValidator : BaseOAuthValidator
	{
		/// <summary>
		/// <see cref="Uri"/> to <see cref="HttpMethod.Post"/> to to get the access token.
		/// </summary>
		protected abstract Uri TokenUrl { get; }

		/// <summary>
		/// <see cref="Uri"/> to <see cref="HttpMethod.Get"/> the user information payload from.
		/// </summary>
		protected abstract Uri UserInformationUrl { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="GenericOAuthValidator"/> <see langword="class"/>.
		/// </summary>
		/// <param name="httpClientFactory">The <see cref="IHttpClientFactory"/> for the <see cref="BaseOAuthValidator"/></param>
		/// <param name="assemblyInformationProvider">The <see cref="IAssemblyInformationProvider"/> for the <see cref="BaseOAuthValidator"/></param>
		/// <param name="logger">The <see cref="ILogger"/> for the <see cref="BaseOAuthValidator"/></param>
		/// <param name="oAuthConfiguration">The <see cref="OAuthConfiguration"/> for the <see cref="BaseOAuthValidator"/>.</param>
		public GenericOAuthValidator(
			IHttpClientFactory httpClientFactory,
			IAssemblyInformationProvider assemblyInformationProvider,
			ILogger<GenericOAuthValidator> logger,
			OAuthConfiguration oAuthConfiguration)
			: base(
				 httpClientFactory,
				 assemblyInformationProvider,
				 logger,
				 oAuthConfiguration)
		{
		}

		/// <summary>
		/// Decode the token payload <paramref name="responseJson"/>.
		/// </summary>
		/// <param name="responseJson">The token payload <see cref="JObject"/>.</param>
		/// <returns>The OAuth2 bearer access token on success, <see langword="null"/> otherwise.</returns>
		protected abstract string DecodeTokenPayload(dynamic responseJson);

		/// <summary>
		/// Decode the user information payload <paramref name="responseJson"/>.
		/// </summary>
		/// <param name="responseJson">The user information payload <see cref="JObject"/>.</param>
		/// <returns>The user ID on success, <see langword="null"/> otherwise.</returns>
		protected abstract string DecodeUserInformationPayload(dynamic responseJson);

		/// <summary>
		/// Create the <see cref="OAuthTokenRequest"/> for a given <paramref name="code"/>
		/// </summary>
		/// <param name="code">The OAuth code from the browser.</param>
		/// <returns>The <see cref="OAuthTokenRequest"/> to send to <see cref="TokenUrl"/>.</returns>
		protected abstract OAuthTokenRequest CreateTokenRequest(string code);

		/// <inheritdoc />
		public override async Task<string> ValidateResponseCode(string code, CancellationToken cancellationToken)
		{
			using var httpClient = CreateHttpClient();
			try
			{
				Logger.LogTrace("Validating response code...");
				using var tokenRequest = new HttpRequestMessage(HttpMethod.Post, TokenUrl);

				var tokenRequestPayload = CreateTokenRequest(code);

				// roundabout but it works
				var tokenRequestJson = JsonConvert.SerializeObject(
					tokenRequestPayload,
					SerializerSettings());

				var tokenRequestDictionary = JsonConvert.DeserializeObject<Dictionary<string, string>>(tokenRequestJson);
				tokenRequest.Content = new FormUrlEncodedContent(tokenRequestDictionary);

				var tokenResponse = await httpClient.SendAsync(tokenRequest, cancellationToken).ConfigureAwait(false);
				tokenResponse.EnsureSuccessStatusCode();
				var tokenResponsePayload = await tokenResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
				var tokenResponseJson = JObject.Parse(tokenResponsePayload);

				var accessToken = DecodeTokenPayload(tokenResponseJson);
				if (accessToken == null)
				{
					Logger.LogTrace("No token from DecodeTokenPayload!");
					return null;
				}

				Logger.LogTrace("Getting user details...");
				using var userInformationRequest = new HttpRequestMessage(HttpMethod.Get, UserInformationUrl);
				userInformationRequest.Headers.Authorization = new AuthenticationHeaderValue(
					ApiHeaders.BearerAuthenticationScheme,
					accessToken);

				var userInformationResponse = await httpClient.SendAsync(userInformationRequest, cancellationToken).ConfigureAwait(false);
				userInformationResponse.EnsureSuccessStatusCode();

				var userInformationPayload = await userInformationResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
				var userInformationJson = JObject.Parse(userInformationPayload);

				return DecodeUserInformationPayload(userInformationJson);
			}
			catch (Exception ex)
			{
				Logger.LogWarning(ex, "Error while completing OAuth handshake!");
				return null;
			}
		}

		/// <inheritdoc />
		public override Task<OAuthProviderInfo> GetProviderInfo(CancellationToken cancellationToken) => Task.FromResult(
			new OAuthProviderInfo
			{
				ClientId = OAuthConfiguration.ClientId,
				RedirectUri = OAuthConfiguration.RedirectUrl
			});
	}
}
