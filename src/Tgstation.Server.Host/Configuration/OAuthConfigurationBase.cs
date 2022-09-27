using System;

namespace Tgstation.Server.Host.Configuration
{
	/// <summary>
	/// Base OAuth options.
	/// </summary>
	public abstract class OAuthConfigurationBase
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
		/// Initializes a new instance of the <see cref="OAuthConfigurationBase"/> class.
		/// </summary>
		public OAuthConfigurationBase()
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="OAuthConfigurationBase"/> class.
		/// </summary>
		/// <param name="oAuthConfiguration">The <see cref="OAuthConfigurationBase"/> to copy settings from.</param>
		public OAuthConfigurationBase(OAuthConfigurationBase oAuthConfiguration)
		{
			if (oAuthConfiguration == null)
				throw new ArgumentNullException(nameof(oAuthConfiguration));
			ClientId = oAuthConfiguration.ClientId;
			ClientSecret = oAuthConfiguration.ClientSecret;
		}
	}
}
