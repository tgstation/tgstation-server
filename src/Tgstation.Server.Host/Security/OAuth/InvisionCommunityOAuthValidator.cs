using System;
using System.Net.Http;

using Microsoft.Extensions.Logging;

using Tgstation.Server.Api.Models;
using Tgstation.Server.Host.Configuration;

namespace Tgstation.Server.Host.Security.OAuth
{
	/// <summary>
	/// OAuth validator for Invision Community (selfhosted).
	/// </summary>
	sealed class InvisionCommunityOAuthValidator : GenericOAuthValidator
	{
		/// <inheritdoc />
#pragma warning disable CS0618 // Type or member is obsolete
		public override OAuthProvider Provider => OAuthProvider.InvisionCommunity;
#pragma warning restore CS0618 // Type or member is obsolete

		/// <inheritdoc />
		protected override Uri TokenUrl => new($"{OAuthConfiguration.ServerUrl}/oauth/token/"); // This needs the trailing slash or it doesnt get the token. Do not remove.

		/// <inheritdoc />
		protected override Uri UserInformationUrl => new($"{OAuthConfiguration.ServerUrl}/api/core/me");

		/// <summary>
		/// Initializes a new instance of the <see cref="InvisionCommunityOAuthValidator"/> class.
		/// </summary>
		/// <param name="httpClientFactory">The <see cref="IHttpClientFactory"/> for the <see cref="GenericOAuthValidator"/>.</param>
		/// <param name="logger">The <see cref="ILogger"/> for the <see cref="GenericOAuthValidator"/>.</param>
		/// <param name="oAuthConfiguration">The <see cref="OAuthConfiguration"/> for the <see cref="GenericOAuthValidator"/>.</param>
		public InvisionCommunityOAuthValidator(
			IHttpClientFactory httpClientFactory,
			ILogger<InvisionCommunityOAuthValidator> logger,
			OAuthConfiguration oAuthConfiguration)
			: base(httpClientFactory, logger, oAuthConfiguration)
		{
		}

		/// <inheritdoc />
		protected override OAuthTokenRequest CreateTokenRequest(string code) => new(OAuthConfiguration, code, "profile");

		/// <inheritdoc />
		protected override string DecodeTokenPayload(dynamic responseJson) => responseJson.access_token;

		/// <inheritdoc />
		protected override string DecodeUserInformationPayload(dynamic responseJson) => responseJson.id;
	}
}
