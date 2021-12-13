using System;
using System.Net.Http;

using Microsoft.Extensions.Logging;

using Tgstation.Server.Api.Models;
using Tgstation.Server.Host.Configuration;

namespace Tgstation.Server.Host.Security.OAuth
{
	/// <summary>
	/// <see cref="IOAuthValidator"/> for /tg/ forums.
	/// </summary>
	sealed class TGForumsOAuthValidator : GenericOAuthValidator
	{
		/// <inheritdoc />
		public override OAuthProvider Provider => OAuthProvider.TGForums;

		/// <inheritdoc />
		protected override Uri TokenUrl => new Uri("https://tgstation13.org/phpBB/app.php/tgapi/oauth/token");

		/// <inheritdoc />
		protected override Uri UserInformationUrl => new Uri("https://tgstation13.org/phpBB/app.php/tgapi/user/me");

		/// <summary>
		/// Initializes a new instance of the <see cref="TGForumsOAuthValidator"/> class.
		/// </summary>
		/// <param name="httpClientFactory">The <see cref="IHttpClientFactory"/> for the <see cref="GenericOAuthValidator"/>.</param>
		/// <param name="assemblyInformationProvider">The <see cref="IAssemblyInformationProvider"/> for the <see cref="GenericOAuthValidator"/>.</param>
		/// <param name="logger">The <see cref="ILogger"/> for the <see cref="GenericOAuthValidator"/>.</param>
		/// <param name="oAuthConfiguration">The <see cref="OAuthConfiguration"/> for the <see cref="GenericOAuthValidator"/>.</param>
		public TGForumsOAuthValidator(
			IHttpClientFactory httpClientFactory,
			ILogger<TGForumsOAuthValidator> logger,
			OAuthConfiguration oAuthConfiguration)
			: base(
				 httpClientFactory,
				 logger,
				 oAuthConfiguration)
		{
		}

		/// <inheritdoc />
		protected override string DecodeTokenPayload(dynamic responseJson) => responseJson.access_token;

		/// <inheritdoc />
		protected override string DecodeUserInformationPayload(dynamic responseJson) => responseJson.phpbb_username;

		/// <inheritdoc />
		protected override OAuthTokenRequest CreateTokenRequest(string code) => new OAuthTokenRequest(OAuthConfiguration, code, "user");
	}
}
