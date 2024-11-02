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
		public string? ClientId { get; set; }

		/// <summary>
		/// The client secret.
		/// </summary>
		public string? ClientSecret { get; set; }

		/// <summary>
		/// If the OAuth setup is only to be used for passing the user's OAuth token to clients.
		/// </summary>
		public OAuthGatewayStatus? Gateway { get; set; }

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
			ArgumentNullException.ThrowIfNull(oAuthConfiguration);
			ClientId = oAuthConfiguration.ClientId;
			ClientSecret = oAuthConfiguration.ClientSecret;
			Gateway = oAuthConfiguration.Gateway;
		}
	}
}
