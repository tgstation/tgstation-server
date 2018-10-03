namespace Tgstation.Server.Host.Configuration
{
	/// <summary>
	/// General configuration options
	/// </summary>
	public sealed class GeneralConfiguration
	{
		/// <summary>
		/// The key for the <see cref="Microsoft.Extensions.Configuration.IConfigurationSection"/> the <see cref="GeneralConfiguration"/> resides in
		/// </summary>
		public const string Section = "General";

		/// <summary>
		/// Minimum length of database user passwords
		/// </summary>
		public uint MinimumPasswordLength { get; set; }

		/// <summary>
		/// A GitHub personal access token to use for bypassing rate limits on requests. Requires no scopes
		/// </summary>
		public string GitHubAccessToken { get; set; }

		/// <summary>
		/// If the <see cref="Core.Application"/> should just check if the configuration wizard needs to be run and then exit
		/// </summary>
		public bool ConfigCheckOnly { get; set; }
	}
}
