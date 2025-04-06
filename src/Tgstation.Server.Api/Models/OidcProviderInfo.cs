using System;

namespace Tgstation.Server.Api.Models
{
	/// <summary>
	/// Represents a configured OIDC provider.
	/// </summary>
	public sealed class OidcProviderInfo
	{
		/// <summary>
		/// The scheme key used to reference the <see cref="OidcProviderInfo"/>. Unique amongst providers.
		/// </summary>
		public string? SchemeKey { get; set; }

		/// <summary>
		/// The provider's name as it should be displayed to the user.
		/// </summary>
		public string? FriendlyName { get; set; }

		/// <summary>
		/// Colour that should be used to theme this OIDC provider.
		/// </summary>
		public string? ThemeColour { get; set; }

		/// <summary>
		/// Image URL that should be used to theme this OIDC provider.
		/// </summary>
		public Uri? ThemeIconUrl { get; set; }
	}
}
