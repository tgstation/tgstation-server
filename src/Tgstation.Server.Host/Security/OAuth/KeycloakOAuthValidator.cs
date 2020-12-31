using Microsoft.Extensions.Logging;
using System;
using System.Net.Http;
using Tgstation.Server.Api.Models;
using Tgstation.Server.Host.Configuration;
using Tgstation.Server.Host.System;

namespace Tgstation.Server.Host.Security.OAuth
{
	/// <summary>
	/// OAuth validator for Keycloak.
	/// </summary>
	sealed class KeycloakOAuthValidator : GenericOAuthValidator
	{
		/// <inheritdoc />
		public override OAuthProvider Provider => OAuthProvider.Keycloak;

		/// <inheritdoc />
		protected override Uri TokenUrl => new Uri($"{BaseProtocolPath}/token");

		/// <inheritdoc />
		protected override Uri UserInformationUrl => new Uri($"{BaseProtocolPath}/userinfo");

		/// <summary>
		/// Base path to the server's OAuth endpoint.
		/// </summary>
		string BaseProtocolPath => $"{OAuthConfiguration.ServerUrl}/protocol/openid-connect";

		/// <summary>
		/// Initializes a new instance of the <see cref="KeycloakOAuthValidator"/> <see langword="class"/>.
		/// </summary>
		/// <param name="httpClientFactory">The <see cref="IHttpClientFactory"/> for the <see cref="GenericOAuthValidator"/>.</param>
		/// <param name="assemblyInformationProvider">The <see cref="IAssemblyInformationProvider"/> for the <see cref="GenericOAuthValidator"/>.</param>
		/// <param name="logger">The <see cref="ILogger"/> for the <see cref="GenericOAuthValidator"/>.</param>
		/// <param name="oAuthConfiguration">The <see cref="OAuthConfiguration"/> for the <see cref="GenericOAuthValidator"/>.</param>
		public KeycloakOAuthValidator(
			IHttpClientFactory httpClientFactory,
			IAssemblyInformationProvider assemblyInformationProvider,
			ILogger<KeycloakOAuthValidator> logger,
			OAuthConfiguration oAuthConfiguration)
			: base(httpClientFactory, assemblyInformationProvider, logger, oAuthConfiguration)
		{
		}

		/// <inheritdoc />
		protected override OAuthTokenRequest CreateTokenRequest(string code) => new OAuthTokenRequest(OAuthConfiguration, code, "openid");

		/// <inheritdoc />
		protected override string DecodeTokenPayload(dynamic responseJson) => responseJson.access_token;

		/// <inheritdoc />
		protected override string DecodeUserInformationPayload(dynamic responseJson) => responseJson.sub;
	}
}
