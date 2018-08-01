namespace Tgstation.Server.Host.Configuration
{
	/// <summary>
	/// General configuration options
	/// </summary>
	sealed class GeneralConfiguration
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
		/// If file logging is disabled
		/// </summary>
		public bool DisableFileLogging { get; set; }
	}
}
