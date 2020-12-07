namespace Tgstation.Server.Host.Configuration
{
	/// <summary>
	/// OAuth configuration options.
	/// </summary>
	sealed class OAuthConfiguration : OAuthConfigurationBase
	{
		/// <summary>
		/// The redirect or server URL. Not used by all providers.
		/// </summary>
		public string Url { get; set; }
	}
}
