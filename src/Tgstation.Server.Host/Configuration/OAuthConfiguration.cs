using System;

namespace Tgstation.Server.Host.Configuration
{
	/// <summary>
	/// OAuth options.
	/// </summary>
	class OAuthConfiguration
	{
		/// <summary>
		/// The client ID.
		/// </summary>
		public string ClientId { get; set; }

		/// <summary>
		/// The client secret.
		/// </summary>
		public string ClientSecret { get; set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="OAuthConfiguration"/> <see langword="class"/>.
		/// </summary>
		public OAuthConfiguration() { }

		/// <summary>
		/// Initializes a new instance of the <see cref="OAuthConfiguration"/> <see langword="class"/>.
		/// </summary>
		/// <param name="oAuthConfiguration">The <see cref="OAuthConfiguration"/> to copy settings from.</param>
		public OAuthConfiguration(OAuthConfiguration oAuthConfiguration)
		{
			if (oAuthConfiguration == null)
				throw new ArgumentNullException(nameof(oAuthConfiguration));
			ClientId = oAuthConfiguration.ClientId;
			ClientSecret = oAuthConfiguration.ClientSecret;
		}
	}
}
