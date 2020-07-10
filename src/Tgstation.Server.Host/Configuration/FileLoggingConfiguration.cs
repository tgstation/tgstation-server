using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using Tgstation.Server.Host.IO;
using Tgstation.Server.Host.System;

namespace Tgstation.Server.Host.Configuration
{
	/// <summary>
	/// File logging configuration options
	/// </summary>
	public sealed class FileLoggingConfiguration
	{
		/// <summary>
		/// The key for the <see cref="Microsoft.Extensions.Configuration.IConfigurationSection"/> the <see cref="FileLoggingConfiguration"/> resides in
		/// </summary>
		public const string Section = "FileLogging";

		/// <summary>
		/// Default value for <see cref="LogLevel"/>
		/// </summary>
		const LogLevel DefaultLogLevel = LogLevel.Debug;

		/// <summary>
		/// Default value for <see cref="MicrosoftLogLevel"/>
		/// </summary>
		const LogLevel DefaultMicrosoftLogLevel = LogLevel.Warning;

		/// <summary>
		/// Where log files are stored
		/// </summary>
		public string Directory { get; set; }

		/// <summary>
		/// If file logging is disabled
		/// </summary>
		public bool Disable { get; set; }

		/// <summary>
		/// The minimum <see cref="Microsoft.Extensions.Logging.LogLevel"/> to display in logs
		/// </summary>
		[JsonConverter(typeof(StringEnumConverter))]
		public LogLevel LogLevel { get; set; } = DefaultLogLevel;

		/// <summary>
		/// The minimum <see cref="Microsoft.Extensions.Logging.LogLevel"/> to display in logs for Microsoft library sources
		/// </summary>
		[JsonConverter(typeof(StringEnumConverter))]
		public LogLevel MicrosoftLogLevel { get; set; } = DefaultMicrosoftLogLevel;

		/// <summary>
		/// Gets the evaluated log <see cref="Directory"/>.
		/// </summary>
		/// <param name="ioManager">The <see cref="IIOManager"/> to use.</param>
		/// <param name="assemblyInformationProvider">The <see cref="IAssemblyInformationProvider"/> to use.</param>
		/// <param name="platformIdentifier">The <see cref="IPlatformIdentifier"/> to use</param>
		/// <returns>The evaluated log <see cref="Directory"/>.</returns>
		public string GetFullLogDirectory(
			IIOManager ioManager,
			IAssemblyInformationProvider assemblyInformationProvider,
			IPlatformIdentifier platformIdentifier)
		{
			if (ioManager == null)
				throw new ArgumentNullException(nameof(ioManager));
			if (assemblyInformationProvider == null)
				throw new ArgumentNullException(nameof(assemblyInformationProvider));
			if (platformIdentifier == null)
				throw new ArgumentNullException(nameof(platformIdentifier));

			var directoryToUse = platformIdentifier.IsWindows
				? Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) // C:/ProgramData
				: "/var/log"; // :pain:

			return !String.IsNullOrEmpty(Directory)
				? Directory
				: ioManager.ConcatPath(
					directoryToUse,
					assemblyInformationProvider.VersionPrefix);
		}
	}
}
