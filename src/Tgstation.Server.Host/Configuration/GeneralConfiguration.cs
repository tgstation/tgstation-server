using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

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
		/// The default value for <see cref="MinimumPasswordLength"/>
		/// </summary>
		const uint DefaultMinimumPasswordLength = 15;

		/// <summary>
		/// The default value for <see cref="ByondTopicTimeout"/>
		/// </summary>
		const int DefaultByondTopicTimeout = 5000;

		/// <summary>
		/// The default value for <see cref="RestartTimeout"/>
		/// </summary>
		const int DefaultRestartTimeout = 10000;

		/// <summary>
		/// Minimum length of database user passwords
		/// </summary>
		public uint MinimumPasswordLength { get; set; } = DefaultMinimumPasswordLength;

		/// <summary>
		/// A GitHub personal access token to use for bypassing rate limits on requests. Requires no scopes
		/// </summary>
		public string GitHubAccessToken { get; set; }

		/// <summary>
		/// The <see cref="SetupWizardMode"/>
		/// </summary>
		[JsonConverter(typeof(StringEnumConverter))]
		public SetupWizardMode SetupWizardMode { get; set; }

		/// <summary>
		/// The timeout in milliseconds for sending and receiving topics to/from DreamDaemon. Note that a single topic exchange can take up to twice this value
		/// </summary>
		public int ByondTopicTimeout { get; set; } = DefaultByondTopicTimeout;

		/// <summary>
		/// The timeout milliseconds for restarting the server
		/// </summary>
		public int RestartTimeout { get; set; } = DefaultRestartTimeout;

		/// <summary>
		/// If the <see cref="Components.Watchdog.ExperimentalWatchdog"/> should be used.
		/// </summary>
		public bool UseExperimentalWatchdog { get; set; }
	}
}
