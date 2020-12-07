using Tgstation.Server.Host.Configuration;

namespace Tgstation.Server.Host.Security.OAuth
{
	/// <summary>
	/// <see cref="OAuthTokenRequest"/> for Discord.
	/// </summary>
	/// <remarks>See https://discord.com/developers/docs/topics/oauth2</remarks>
	sealed class DiscordTokenRequest : OAuthTokenRequest
	{
		/// <summary>
		/// The 'grant_type' field.
		/// </summary>
		public string GrantType { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="DiscordTokenRequest"/> <see langword="class"/>.
		/// </summary>
		/// <param name="oAuthConfiguration">The <see cref="OAuthConfigurationBase"/> for the <see cref="OAuthTokenRequest"/>.</param>
		/// <param name="code">The OAuth code for the <see cref="OAuthTokenRequest"/>.</param>
		public DiscordTokenRequest(OAuthConfigurationBase oAuthConfiguration, string code)
			: base(oAuthConfiguration, code, "identify")
		{
			GrantType = "authorization_code";
		}
	}
}
