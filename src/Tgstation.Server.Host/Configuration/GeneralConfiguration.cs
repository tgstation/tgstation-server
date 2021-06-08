using System;

using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

using Tgstation.Server.Api.Models.Internal;
using Tgstation.Server.Host.Properties;
using Tgstation.Server.Host.Setup;

namespace Tgstation.Server.Host.Configuration
{
	/// <summary>
	/// General configuration options.
	/// </summary>
	public sealed class GeneralConfiguration : ServerInformationBase
	{
		/// <summary>
		/// The key for the <see cref="Microsoft.Extensions.Configuration.IConfigurationSection"/> the <see cref="GeneralConfiguration"/> resides in.
		/// </summary>
		public const string Section = "General";

		/// <summary>
		/// The default value of <see cref="BindAddress"/>.
		/// </summary>
		public const string DefaultBindAddress = "127.0.0.0:5000";

		/// <summary>
		/// The default value for <see cref="ServerInformationBase.MinimumPasswordLength"/>.
		/// </summary>
		const uint DefaultMinimumPasswordLength = 15;

		/// <summary>
		/// The default value for <see cref="ServerInformationBase.InstanceLimit"/>.
		/// </summary>
		const uint DefaultInstanceLimit = 10;

		/// <summary>
		/// The default value for <see cref="ServerInformationBase.UserLimit"/>.
		/// </summary>
		const uint DefaultUserLimit = 100;

		/// <summary>
		/// The default value for <see cref="ServerInformationBase.UserGroupLimit"/>.
		/// </summary>
		const uint DefaultUserGroupLimit = 25;

		/// <summary>
		/// The default value for <see cref="ByondTopicTimeout"/>.
		/// </summary>
		const uint DefaultByondTopicTimeout = 5000;

		/// <summary>
		/// The default value for <see cref="RestartTimeout"/>.
		/// </summary>
		const uint DefaultRestartTimeout = 60000;

		/// <summary>
		/// The current <see cref="ConfigVersion"/>.
		/// </summary>
		public static readonly Version CurrentConfigVersion = Version.Parse(MasterVersionsAttribute.Instance.RawConfigurationVersion);

		/// <summary>
		/// The <see cref="Version"/> the file says it is.
		/// </summary>
		public Version ConfigVersion { get; set; }

		/// <summary>
		/// The bind address the TGS API listens on, takes the form of the ip address to bind to, followed by the port.
		/// e.g 127.0.0.1:5000.
		/// </summary>
		public Uri BindAddress { get; set; }

		/// <summary>
		/// A GitHub personal access token to use for bypassing rate limits on requests. Requires no scopes.
		/// </summary>
		public string GitHubAccessToken { get; set; }

		/// <summary>
		/// The <see cref="SetupWizardMode"/>.
		/// </summary>
		[JsonConverter(typeof(StringEnumConverter))]
		public SetupWizardMode SetupWizardMode { get; set; }

		/// <summary>
		/// The timeout in milliseconds for sending and receiving topics to/from DreamDaemon. Note that a single topic exchange can take up to twice this value.
		/// </summary>
		public uint ByondTopicTimeout { get; set; } = DefaultByondTopicTimeout;

		/// <summary>
		/// The timeout milliseconds for restarting the server.
		/// </summary>
		public uint RestartTimeout { get; set; } = DefaultRestartTimeout;

		/// <summary>
		/// If the <see cref="Components.Watchdog.BasicWatchdog"/> should be preferred.
		/// </summary>
		public bool UseBasicWatchdog { get; set; }

		/// <summary>
		/// If the swagger UI should be made avaiable.
		/// </summary>
		public bool HostApiDocumentation { get; set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="GeneralConfiguration"/> class.
		/// </summary>
		public GeneralConfiguration()
		{
			MinimumPasswordLength = DefaultMinimumPasswordLength;
			InstanceLimit = DefaultInstanceLimit;
			UserLimit = DefaultUserLimit;
			UserGroupLimit = DefaultUserGroupLimit;
		}

		/// <summary>
		/// Validates the current <see cref="ConfigVersion"/>'s compatibility and provides migration instructions.
		/// </summary>
		/// <param name="logger">The <see cref="ILogger"/> to use.</param>
		public void CheckCompatibility(ILogger logger)
		{
			if (logger == null)
				throw new ArgumentNullException(nameof(logger));

			if (ConfigVersion == null)
				logger.LogCritical(
					"No `ConfigVersion` specified, your configuration may be out of date! The current version is \"{0}\"",
					CurrentConfigVersion);
			else if (ConfigVersion != CurrentConfigVersion)
				if (ConfigVersion.Major != CurrentConfigVersion.Major)
					logger.LogCritical(
						"Your `ConfigVersion` is majorly out-of-date and may potentially cause issues running the server. Please follow migration instructions from the TGS release notes.",
						CurrentConfigVersion);
				else
					logger.LogWarning("Your `ConfigVersion` is out-of-date. Please follow migration instructions from the TGS release notes.");
		}
	}
}
