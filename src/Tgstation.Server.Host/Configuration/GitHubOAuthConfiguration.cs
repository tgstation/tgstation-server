namespace Tgstation.Server.Host.Configuration
{
	/// <summary>
	/// OAuth options for GitHub
	/// </summary>
	sealed class GitHubOAuthConfiguration
	{
		/// <summary>
		/// The GitHub client ID.
		/// </summary>
		public string ClientId { get; set; }

		/// <summary>
		/// The GitHub client secret.
		/// </summary>
		public string ClientSecret { get; set; }
	}
}
