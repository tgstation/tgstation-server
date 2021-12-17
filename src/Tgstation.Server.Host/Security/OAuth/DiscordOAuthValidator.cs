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
	sealed class DiscordOAuthValidator : GenericOAuthValidator
	{
		/// <inheritdoc />
		public override OAuthProvider Provider => OAuthProvider.Discord;

		/// <inheritdoc />
		protected override Uri TokenUrl => new Uri("https://discord.com/api/oauth2/token");

		/// <inheritdoc />
		protected override Uri UserInformationUrl => new Uri("https://discord.com/api/users/@me");

		/// <summary>
		/// Initializes a new instance of the <see cref="DiscordOAuthValidator"/> class.
		/// </summary>
		/// <param name="httpClientFactory">The <see cref="IHttpClientFactory"/> for the <see cref="GenericOAuthValidator"/>.</param>
		/// <param name="assemblyInformationProvider">The <see cref="IAssemblyInformationProvider"/> for the <see cref="GenericOAuthValidator"/>.</param>
		/// <param name="logger">The <see cref="ILogger"/> for the <see cref="GenericOAuthValidator"/>.</param>
		/// <param name="oAuthConfiguration">The <see cref="OAuthConfiguration"/> for the <see cref="GenericOAuthValidator"/>.</param>
		public DiscordOAuthValidator(
			IHttpClientFactory httpClientFactory,
			IAssemblyInformationProvider assemblyInformationProvider,
			ILogger<DiscordOAuthValidator> logger,
			OAuthConfiguration oAuthConfiguration)
			: base(httpClientFactory, assemblyInformationProvider, logger, oAuthConfiguration)
		{
		}

		/// <inheritdoc />
		protected override OAuthTokenRequest CreateTokenRequest(string code) => new OAuthTokenRequest(OAuthConfiguration, code, "identify");

		/// <inheritdoc />
		protected override string DecodeTokenPayload(dynamic responseJson) => responseJson.access_token;

		/// <inheritdoc />
		protected override string DecodeUserInformationPayload(dynamic responseJson) => responseJson.id;
	}
}
