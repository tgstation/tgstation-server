using System;

using Microsoft.IdentityModel.JsonWebTokens;

namespace Tgstation.Server.Host.Configuration
{
	/// <summary>
	/// Configuration for an OpenID Connect provider.
	/// </summary>
	public sealed class OidcConfiguration
	{
		/// <summary>
		/// The default value of <see cref="GroupIdClaim"/>.
		/// </summary>
		private const string DefaultGroupClaimName = "tgstation-server-group-id";

		/// <summary>
		/// The <see cref="Uri"/> containing the .well-known endpoint for the provider.
		/// </summary>
		public Uri? Authority { get; set; }

		/// <summary>
		/// The OIDC client ID.
		/// </summary>
		public string? ClientId { get; set; }

		/// <summary>
		/// The OIDC client secret.
		/// </summary>
		public string? ClientSecret { get; set; }

		/// <summary>
		/// The provider's name as it should be displayed to the user.
		/// </summary>
		public string? FriendlyName { get; set; }

		/// <summary>
		/// The path to return to once OIDC authentication is complete. On success the "code" and "state" query strings will be set, containing a TGS token and "oidc.(scheme key)" strings respectively. On failure the "error" query string will be set with a user readable error message.
		/// </summary>
		public string? ReturnPath { get; set; } = "/app";

		/// <summary>
		/// Colour that should be used to theme this OIDC provider.
		/// </summary>
		public string? ThemeColour { get; set; }

		/// <summary>
		/// Image URL that should be used to theme this OIDC provider.
		/// </summary>
		public Uri? ThemeIconUrl { get; set; }

		/// <summary>
		/// The name of the claim used to set the user's name.
		/// </summary>
		public string UsernameClaim { get; set; } = JwtRegisteredClaimNames.PreferredUsername;

		/// <summary>
		/// Claim name used to set user groups in OIDC strict mode.
		/// </summary>
		public string GroupIdClaim { get; set; } = DefaultGroupClaimName;
	}
}
