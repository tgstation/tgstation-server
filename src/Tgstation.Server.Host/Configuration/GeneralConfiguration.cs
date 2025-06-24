using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

using Tgstation.Server.Api.Models.Internal;
using Tgstation.Server.Host.Extensions;
using Tgstation.Server.Host.Properties;
using Tgstation.Server.Host.Setup;

using YamlDotNet.Serialization;

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
		/// The default value of <see cref="ApiPort"/>.
		/// </summary>
		public const ushort DefaultApiPort = 5000;

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
		/// The default value for <see cref="RestartTimeoutMinutes"/>.
		/// </summary>
		const uint DefaultRestartTimeoutMinutes = 1;

		/// <summary>
		/// The default value for <see cref="ShutdownTimeoutMinutes"/>.
		/// </summary>
		const uint DefaultShutdownTimeoutMinutes = 300;

		/// <summary>
		/// The default value for <see cref="OpenDreamGitUrl"/>.
		/// </summary>
		const string DefaultOpenDreamGitUrl = "https://github.com/OpenDreamProject/OpenDream";

		/// <summary>
		/// The default value for <see cref="OpenDreamGitTagPrefix"/>.
		/// </summary>
		const string DefaultOpenDreamGitTagPrefix = "v";

		/// <summary>
		/// The current <see cref="ConfigVersion"/>.
		/// </summary>
		public static readonly Version CurrentConfigVersion = Version.Parse(MasterVersionsAttribute.Instance.RawConfigurationVersion);

		/// <summary>
		/// The <see cref="Version"/> the file says it is.
		/// </summary>
		public Version? ConfigVersion { get; set; }

		/// <summary>
		/// The port the TGS API listens on.
		/// </summary>
		public ushort ApiPort { get; set; }

		/// <summary>
		/// The port Prometheus metrics are published on, if any.
		/// </summary>
		public ushort? PrometheusPort { get; set; }

		/// <summary>
		/// A classic GitHub personal access token to use for bypassing rate limits on requests. Requires no scopes.
		/// </summary>
		public string? GitHubAccessToken { get; set; }

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
		/// The timeout minutes for restarting the server.
		/// </summary>
		public uint RestartTimeoutMinutes { get; set; } = DefaultRestartTimeoutMinutes;

		/// <summary>
		/// The timeout minutes for gracefully stopping the server.
		/// </summary>
		public uint ShutdownTimeoutMinutes { get; set; } = DefaultShutdownTimeoutMinutes;

		/// <summary>
		/// If the <see cref="Components.Watchdog.BasicWatchdog"/> should be preferred.
		/// </summary>
		public bool UseBasicWatchdog { get; set; }

		/// <summary>
		/// If the swagger documentation and UI should be made avaiable.
		/// </summary>
		public bool HostApiDocumentation { get; set; }

		/// <summary>
		/// If the netsh.exe execution to exempt DreamDaemon from Windows firewall should be skipped.
		/// </summary>
		public bool SkipAddingByondFirewallException { get; set; }

		/// <summary>
		/// A limit on the amount of tasks used for asynchronous I/O when copying directories during the deployment process as a multiplier to the machine's <see cref="Environment.ProcessorCount"/>. Too few can significantly increase deployment times, too many can make TGS unresponsive and slowdown other I/O operations on the machine.
		/// </summary>
		public uint? DeploymentDirectoryCopyTasksPerCore { get; set; }

		/// <summary>
		/// Location of a publically accessible OpenDream repository.
		/// </summary>
		[YamlMember(SerializeAs = typeof(string))]
		public Uri OpenDreamGitUrl { get; set; } = new Uri(DefaultOpenDreamGitUrl);

		/// <summary>
		/// The formatter used to download official byond zip files for a given version
		/// - ${Major} is substituted with the major version number
		/// - ${Minor} is substituted with the minor version number
		/// - ${Linux:xxx}, where xxx is any string, will be substituted with xxx if running under Linux.
		/// - ${Windows:xxx}, where xxx is any string, will be substituted with xxx if running under Windows.
		/// - $$ will evaluate to a literal $ and not be used for substitutions.
		/// - Any inapplicable ${xxx} string will be removed.
		/// </summary>
		public string ByondZipDownloadTemplate { get; set; } = "https://www.byond.com/download/build/${Major}/${Major}.{Minor}_byond${Linux:_linux}.zip";

		/// <summary>
		/// The prefix to the OpenDream semver as tags appear in the git repository.
		/// </summary>
		public string OpenDreamGitTagPrefix { get; set; } = DefaultOpenDreamGitTagPrefix;

		/// <summary>
		/// If the dotnet output of creating an OpenDream installation should be suppressed. Known to cause issues in CI.
		/// </summary>
		public bool OpenDreamSuppressInstallOutput { get; set; }

		/// <summary>
		/// List of directories that have their contents merged with instance EventScripts directories when executing scripts.
		/// </summary>
		public List<string>? AdditionalEventScriptsDirectories { get; set; }

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
			ArgumentNullException.ThrowIfNull(logger);

			if (ConfigVersion == null)
				logger.LogCritical(
					"No `ConfigVersion` specified, your configuration may be out of date! The current version is \"{currentVersion}\"",
					CurrentConfigVersion);
			else if (ConfigVersion != CurrentConfigVersion)
				if (ConfigVersion.Major != CurrentConfigVersion.Major)
					logger.LogCritical(
						"Your `ConfigVersion` is majorly out-of-date and may potentially cause issues running the server. Please follow migration instructions from the TGS release notes. The current config version is v{currentConfigVersion}.",
						CurrentConfigVersion);
				else
					logger.LogWarning("Your `ConfigVersion` is out-of-date. Please follow migration instructions from the TGS release notes.");

			if (DeploymentDirectoryCopyTasksPerCore == 0)
				throw new InvalidOperationException(
					$"{nameof(DeploymentDirectoryCopyTasksPerCore)} must be at least 1!");
			else if (this.GetCopyDirectoryTaskThrottle() < 1)
				throw new InvalidOperationException(
					$"{nameof(DeploymentDirectoryCopyTasksPerCore)} is too large for the CPU core count of {Environment.ProcessorCount} and overflows a 32-bit signed integer. Please lower the value!");

			if (ByondTopicTimeout <= 1000)
				logger.LogWarning("The timeout for sending BYOND topics is very low ({ms}ms). Topic calls may fail to complete at all!", ByondTopicTimeout);

			if (AdditionalEventScriptsDirectories?.Any(path => !Path.IsPathRooted(path)) == true)
				logger.LogWarning($"Config option \"{nameof(AdditionalEventScriptsDirectories)}\" contains non-rooted paths. These will be evaluated relative to each instances \"Configuration\" directory!");
		}
	}
}
