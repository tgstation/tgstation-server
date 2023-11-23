using System;

#nullable disable

namespace Tgstation.Server.Host.Configuration
{
	/// <summary>
	/// OAuth configuration options.
	/// </summary>
	public sealed class OAuthConfiguration : OAuthConfigurationBase
	{
		/// <summary>
		/// The client redirect URL. Not used by all providers.
		/// </summary>
		public Uri ServerUrl { get; set; }

		/// <summary>
		/// The authentication server URL. Not used by all providers.
		/// </summary>
		public Uri RedirectUrl { get; set; }

		/// <summary>
		/// User information URL override. Not supported by the <see cref="Api.Models.OAuthProvider.GitHub"/> provider.
		/// </summary>
		public Uri UserInformationUrlOverride { get; set; }
	}
}
