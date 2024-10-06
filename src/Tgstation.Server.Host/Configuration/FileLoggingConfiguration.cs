using System;

using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

using Tgstation.Server.Host.IO;
using Tgstation.Server.Host.System;

namespace Tgstation.Server.Host.Configuration
{
	/// <summary>
	/// File logging configuration options.
	/// </summary>
	public sealed class FileLoggingConfiguration
	{
		/// <summary>
		/// The key for the <see cref="Microsoft.Extensions.Configuration.IConfigurationSection"/> the <see cref="FileLoggingConfiguration"/> resides in.
		/// </summary>
		public const string Section = "FileLogging";

		/// <summary>
		/// Where log files are stored.
		/// </summary>
		public string? Directory { get; set; }

		/// <summary>
		/// If file logging is disabled.
		/// </summary>
		public bool Disable { get; set; }

		/// <summary>
		/// If Chat Providers should log their network traffic. Normally disabled because it is too noisy.
		/// </summary>
		public bool ProviderNetworkDebug { get; set; }

		/// <summary>
		/// The minimum <see cref="Microsoft.Extensions.Logging.LogLevel"/> to display in logs.
		/// </summary>
		[JsonConverter(typeof(StringEnumConverter))]
		public LogLevel LogLevel { get; set; } = LogLevel.Debug; // Not a `const` b/c of https://github.com/coverlet-coverage/coverlet/issues/1507

		/// <summary>
		/// The minimum <see cref="Microsoft.Extensions.Logging.LogLevel"/> to display in logs for Microsoft library sources.
		/// </summary>
		[JsonConverter(typeof(StringEnumConverter))]
		public LogLevel MicrosoftLogLevel { get; set; } = LogLevel.Warning; // Not a `const` b/c of https://github.com/coverlet-coverage/coverlet/issues/1507

		/// <summary>
		/// Gets the evaluated log <see cref="Directory"/>.
		/// </summary>
		/// <param name="ioManager">The <see cref="IIOManager"/> to use.</param>
		/// <param name="assemblyInformationProvider">The <see cref="IAssemblyInformationProvider"/> to use.</param>
		/// <param name="platformIdentifier">The <see cref="IPlatformIdentifier"/> to use.</param>
		/// <returns>The evaluated log <see cref="Directory"/>.</returns>
		public string GetFullLogDirectory(
			IIOManager ioManager,
			IAssemblyInformationProvider assemblyInformationProvider,
			IPlatformIdentifier platformIdentifier)
		{
			ArgumentNullException.ThrowIfNull(ioManager);
			ArgumentNullException.ThrowIfNull(assemblyInformationProvider);
			ArgumentNullException.ThrowIfNull(platformIdentifier);

			if (!String.IsNullOrEmpty(Directory))
				return Directory;

			return platformIdentifier.IsWindows
				? ioManager.ConcatPath(
					Environment.GetFolderPath(
						Environment.SpecialFolder.CommonApplicationData,
						Environment.SpecialFolderOption.DoNotVerify),
					assemblyInformationProvider.VersionPrefix,
					"logs")
				: ioManager.ConcatPath(
					"/var/log",
					assemblyInformationProvider.VersionPrefix);
		}
	}
}
