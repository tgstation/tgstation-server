using System;
using System.Net.Http;

using Microsoft.Extensions.Logging;

using Tgstation.Server.Api.Models;
using Tgstation.Server.Host.Configuration;
using Tgstation.Server.Host.System;

namespace Tgstation.Server.Host.Security.OAuth
{
	/// <summary>
	/// OAuth validator for Discord.
	/// </summary>
	sealed class InvisionCommunityOAuthValidator : GenericOAuthValidator
		{
		/// <inheritdoc />
		public override OAuthProvider Provider => OAuthProvider.InvisionCommunity;

		/// <inheritdoc />
		protected override Uri TokenUrl => new Uri($"{BaseProtocolPath}/oauth/token/");

		/// <inheritdoc />
		protected override Uri UserInformationUrl => new Uri($"{BaseProtocolPath}/api/core/me");

		/// <summary>
		/// Base path to the server's OAuth endpoint.
		/// </summary>
		string BaseProtocolPath => $"{OAuthConfiguration.ServerUrl}";

		/// <summary>
		/// Initializes a new instance of the <see cref="InvisionCommunityOAuthValidator"/> class.
		/// </summary>
		/// <param name="httpClientFactory">The <see cref="IHttpClientFactory"/> for the <see cref="GenericOAuthValidator"/>.</param>
		/// <param name="assemblyInformationProvider">The <see cref="IAssemblyInformationProvider"/> for the <see cref="GenericOAuthValidator"/>.</param>
		/// <param name="logger">The <see cref="ILogger"/> for the <see cref="GenericOAuthValidator"/>.</param>
		/// <param name="oAuthConfiguration">The <see cref="OAuthConfiguration"/> for the <see cref="GenericOAuthValidator"/>.</param>
		public InvisionCommunityOAuthValidator(
			IHttpClientFactory httpClientFactory,
			IAssemblyInformationProvider assemblyInformationProvider,
			ILogger<InvisionCommunityOAuthValidator> logger,
			OAuthConfiguration oAuthConfiguration)
			: base(httpClientFactory, assemblyInformationProvider, logger, oAuthConfiguration) {
		}

		/// <inheritdoc />
		protected override OAuthTokenRequest CreateTokenRequest(string code) => new OAuthTokenRequest(OAuthConfiguration, code, "profile");

		/// <inheritdoc />
		protected override string DecodeTokenPayload(dynamic responseJson) => responseJson.access_token;

		/// <inheritdoc />
		protected override string DecodeUserInformationPayload(dynamic responseJson) => responseJson.id;
	}
}
