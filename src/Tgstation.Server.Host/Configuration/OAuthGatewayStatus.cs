namespace Tgstation.Server.Host
{
	/// <summary>
	/// Status of the OAuth gateway for a provider.
	/// </summary>
	public enum OAuthGatewayStatus
	{
		/// <summary>
		/// The OAuth Gateway is disabled.
		/// </summary>
		Disabled,

		/// <summary>
		/// The OAuth Gateway is enabled.
		/// </summary>
		Enabled,

		/// <summary>
		/// The provider may ONLY be used as an OAuth Gateway.
		/// </summary>
		Only,
	}
}
