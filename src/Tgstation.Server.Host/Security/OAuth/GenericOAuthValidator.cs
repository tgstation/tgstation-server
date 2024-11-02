using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

using Tgstation.Server.Api;
using Tgstation.Server.Api.Models;
using Tgstation.Server.Common.Http;
using Tgstation.Server.Host.Configuration;
using Tgstation.Server.Host.Extensions;

namespace Tgstation.Server.Host.Security.OAuth
{
	/// <summary>
	/// <see cref="IOAuthValidator"/> for generic OAuth2 endpoints.
	/// </summary>
	abstract class GenericOAuthValidator : IOAuthValidator
	{
		/// <inheritdoc />
		public abstract OAuthProvider Provider { get; }

		/// <inheritdoc />
		public OAuthGatewayStatus GatewayStatus => OAuthConfiguration.Gateway ?? OAuthGatewayStatus.Disabled;

		/// <summary>
		/// The <see cref="ILogger"/> for the <see cref="GenericOAuthValidator"/>.
		/// </summary>
		protected ILogger<GenericOAuthValidator> Logger { get; }

		/// <summary>
		/// The <see cref="OAuthConfiguration"/> for the <see cref="GenericOAuthValidator"/>.
		/// </summary>
		protected OAuthConfiguration OAuthConfiguration { get; }

		/// <summary>
		/// <see cref="Uri"/> to <see cref="HttpMethod.Post"/> to to get the access token.
		/// </summary>
		protected abstract Uri TokenUrl { get; }

		/// <summary>
		/// <see cref="Uri"/> to <see cref="HttpMethod.Get"/> the user information payload from.
		/// </summary>
		protected abstract Uri UserInformationUrl { get; }

		/// <summary>
		/// The <see cref="IHttpClientFactory"/> for the <see cref="GenericOAuthValidator"/>.
		/// </summary>
		readonly IAbstractHttpClientFactory httpClientFactory;

		/// <summary>
		/// Gets <see cref="JsonSerializerSettings"/> that should be used.
		/// </summary>
		/// <returns>A new <see cref="JsonSerializerSettings"/> <see cref="object"/>.</returns>
		protected static JsonSerializerSettings SerializerSettings() => new()
		{
			ContractResolver = new DefaultContractResolver
			{
				NamingStrategy = new SnakeCaseNamingStrategy(),
			},
		};

		/// <summary>
		/// Initializes a new instance of the <see cref="GenericOAuthValidator"/> class.
		/// </summary>
		/// <param name="httpClientFactory">The value of <see cref="httpClientFactory"/>.</param>
		/// <param name="logger">The value of <see cref="Logger"/>.</param>
		/// <param name="oAuthConfiguration">The value of <see cref="OAuthConfiguration"/>.</param>
		public GenericOAuthValidator(
			IAbstractHttpClientFactory httpClientFactory,
			ILogger<GenericOAuthValidator> logger,
			OAuthConfiguration oAuthConfiguration)
		{
			this.httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
			Logger = logger ?? throw new ArgumentNullException(nameof(logger));
			OAuthConfiguration = oAuthConfiguration ?? throw new ArgumentNullException(nameof(oAuthConfiguration));
		}

		/// <inheritdoc />
		public async ValueTask<(string? UserID, string AccessCode)?> ValidateResponseCode(string code, bool requireUserID, CancellationToken cancellationToken)
		{
			using var httpClient = CreateHttpClient();
			string? tokenResponsePayload = null;
			string? userInformationPayload = null;
			try
			{
				Logger.LogTrace("Validating response code...");
				using var tokenRequest = new HttpRequestMessage(HttpMethod.Post, TokenUrl);

				var tokenRequestPayload = CreateTokenRequest(code);

				// roundabout but it works
				var tokenRequestJson = JsonConvert.SerializeObject(
					tokenRequestPayload,
					SerializerSettings());

				var tokenRequestDictionary = JsonConvert.DeserializeObject<Dictionary<string, string>>(tokenRequestJson)!;
				tokenRequest.Content = new FormUrlEncodedContent(tokenRequestDictionary);

				using var tokenResponse = await httpClient.SendAsync(tokenRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
				tokenResponse.EnsureSuccessStatusCode();
				tokenResponsePayload = await tokenResponse.Content.ReadAsStringAsync(cancellationToken);
				var tokenResponseJson = JObject.Parse(tokenResponsePayload);

				var accessToken = DecodeTokenPayload(tokenResponseJson);
				if (accessToken == null)
				{
					Logger.LogTrace("No token from DecodeTokenPayload!");
					return null;
				}

				if (!requireUserID)
					return (null, AccessCode: accessToken);

				Logger.LogTrace("Getting user details...");

				var userInfoUrl = OAuthConfiguration?.UserInformationUrlOverride ?? UserInformationUrl;
				using var userInformationRequest = new HttpRequestMessage(HttpMethod.Get, userInfoUrl);
				userInformationRequest.Headers.Authorization = new AuthenticationHeaderValue(
					ApiHeaders.BearerAuthenticationScheme,
					accessToken);

				using var userInformationResponse = await httpClient.SendAsync(userInformationRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
				userInformationResponse.EnsureSuccessStatusCode();
				userInformationPayload = await userInformationResponse.Content.ReadAsStringAsync(cancellationToken);

				var userInformationJson = JObject.Parse(userInformationPayload);

				return (DecodeUserInformationPayload(userInformationJson), AccessCode: accessToken);
			}
			catch (Exception ex)
			{
				Logger.LogWarning(
					ex,
					"Error while completing OAuth handshake! Payload:{newLine}{responsePayload}",
					Environment.NewLine,
					userInformationPayload ?? tokenResponsePayload);
				return null;
			}
		}

		/// <inheritdoc />
		public OAuthProviderInfo GetProviderInfo()
			=> new()
			{
				ClientId = OAuthConfiguration.ClientId,
				RedirectUri = OAuthConfiguration.RedirectUrl,
				ServerUrl = OAuthConfiguration.ServerUrl,
				GatewayOnly = GatewayStatus.ToBoolean(),
			};

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
		/// Create the <see cref="OAuthTokenRequest"/> for a given <paramref name="code"/>.
		/// </summary>
		/// <param name="code">The OAuth code from the browser.</param>
		/// <returns>The <see cref="OAuthTokenRequest"/> to send to <see cref="TokenUrl"/>.</returns>
		protected abstract OAuthTokenRequest CreateTokenRequest(string code);

		/// <summary>
		/// Create a new configured <see cref="IHttpClient"/>.
		/// </summary>
		/// <returns>A new configured <see cref="IHttpClient"/>.</returns>
		IHttpClient CreateHttpClient()
		{
			var httpClient = httpClientFactory.CreateClient();
			try
			{
				httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(MediaTypeNames.Application.Json));
				return httpClient;
			}
			catch
			{
				httpClient.Dispose();
				throw;
			}
		}
	}
}
