using System;

using Microsoft.Extensions.Logging;

using Tgstation.Server.Api.Models;
using Tgstation.Server.Common.Http;
using Tgstation.Server.Host.Configuration;

namespace Tgstation.Server.Host.Security.OAuth
{
	/// <summary>
	/// OAuth validator for Keycloak.
	/// </summary>
	sealed class KeycloakOAuthValidator : GenericOAuthValidator
	{
		/// <inheritdoc />
#pragma warning disable CS0618 // Type or member is obsolete
		public override OAuthProvider Provider => OAuthProvider.Keycloak;
#pragma warning restore CS0618 // Type or member is obsolete

		/// <inheritdoc />
		protected override Uri TokenUrl => new($"{BaseProtocolPath}/token");

		/// <inheritdoc />
		protected override Uri UserInformationUrl => new($"{BaseProtocolPath}/userinfo");

		/// <summary>
		/// Base path to the server's OAuth endpoint.
		/// </summary>
		string BaseProtocolPath => $"{OAuthConfiguration.ServerUrl}/protocol/openid-connect";

		/// <summary>
		/// Initializes a new instance of the <see cref="KeycloakOAuthValidator"/> class.
		/// </summary>
		/// <param name="httpClientFactory">The <see cref="IAbstractHttpClientFactory"/> for the <see cref="GenericOAuthValidator"/>.</param>
		/// <param name="logger">The <see cref="ILogger"/> for the <see cref="GenericOAuthValidator"/>.</param>
		/// <param name="oAuthConfiguration">The <see cref="OAuthConfiguration"/> for the <see cref="GenericOAuthValidator"/>.</param>
		public KeycloakOAuthValidator(
			IAbstractHttpClientFactory httpClientFactory,
			ILogger<KeycloakOAuthValidator> logger,
			OAuthConfiguration oAuthConfiguration)
			: base(httpClientFactory, logger, oAuthConfiguration)
		{
		}

		/// <inheritdoc />
		protected override OAuthTokenRequest CreateTokenRequest(string code) => new(OAuthConfiguration, code, "openid");

		/// <inheritdoc />
		protected override string DecodeTokenPayload(dynamic responseJson) => responseJson.access_token;

		/// <inheritdoc />
		protected override string DecodeUserInformationPayload(dynamic responseJson) => responseJson.sub;
	}
}
