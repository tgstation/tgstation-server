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
		/// Where log files are stored
		/// </summary>
		public string LogFileDirectory { get; set; }

		/// <summary>
		/// The stringified <see cref="Microsoft.Extensions.Logging.LogLevel"/> for file logging
		/// </summary>
		public string LogFileLevel { get; set; }

		/// <summary>
		/// If file logging is disabled
		/// </summary>
		public bool DisableFileLogging { get; set; }

		/// <summary>
		/// Minimum length of database user passwords
		/// </summary>
		public uint MinimumPasswordLength { get; set; }

		/// <summary>
		/// A GitHub personal access token to use for bypassing rate limits on requests. Requires no scopes
		/// </summary>
		public string GitHubAccessToken { get; set; }
	}
}
