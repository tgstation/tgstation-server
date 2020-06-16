using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Tgstation.Server.Api.Models.Internal;

namespace Tgstation.Server.Host.Configuration
{
	/// <summary>
	/// General configuration options
	/// </summary>
	public sealed class GeneralConfiguration : ServerInformation
	{
		/// <summary>
		/// The key for the <see cref="Microsoft.Extensions.Configuration.IConfigurationSection"/> the <see cref="GeneralConfiguration"/> resides in
		/// </summary>
		public const string Section = "General";

		/// <summary>
		/// The default value of <see cref="ApiPort"/>.
		/// </summary>
		public const ushort DefaultApiPort = 5000;

		/// <summary>
		/// The default value for <see cref="ServerInformation.MinimumPasswordLength"/>.
		/// </summary>
		const uint DefaultMinimumPasswordLength = 15;

		/// <summary>
		/// The default value for <see cref="ServerInformation.InstanceLimit"/>.
		/// </summary>
		const uint DefaultInstanceLimit = 10;

		/// <summary>
		/// The default value for <see cref="ServerInformation.UserLimit"/>.
		/// </summary>
		const uint DefaultUserLimit = 100;

		/// <summary>
		/// The default value for <see cref="ByondTopicTimeout"/>
		/// </summary>
		const uint DefaultByondTopicTimeout = 5000;

		/// <summary>
		/// The default value for <see cref="RestartTimeout"/>
		/// </summary>
		const uint DefaultRestartTimeout = 60000;

		/// <summary>
		/// The port the TGS API listens on.
		/// </summary>
		public ushort ApiPort { get; set; }

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
		public uint ByondTopicTimeout { get; set; } = DefaultByondTopicTimeout;

		/// <summary>
		/// The timeout milliseconds for restarting the server
		/// </summary>
		public uint RestartTimeout { get; set; } = DefaultRestartTimeout;

		/// <summary>
		/// If the <see cref="Components.Watchdog.ExperimentalWatchdog"/> should be used.
		/// </summary>
		public bool UseExperimentalWatchdog { get; set; }

		/// <summary>
		/// If the <see cref="Components.Watchdog.WindowsWatchdog"/> should not be used if it is available.
		/// </summary>
		public bool UseBasicWatchdogOnWindows { get; set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="GeneralConfiguration"/> <see langword="class"/>.
		/// </summary>
		public GeneralConfiguration()
		{
			MinimumPasswordLength = DefaultMinimumPasswordLength;
			InstanceLimit = DefaultInstanceLimit;
			UserLimit = DefaultUserLimit;
		}
	}
}
