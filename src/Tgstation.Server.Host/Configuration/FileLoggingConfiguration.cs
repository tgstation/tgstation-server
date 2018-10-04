using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Tgstation.Server.Host.Configuration
{
	/// <summary>
	/// File logging configuration options
	/// </summary>
	sealed class FileLoggingConfiguration
	{
		/// <summary>
		/// The key for the <see cref="Microsoft.Extensions.Configuration.IConfigurationSection"/> the <see cref="FileLoggingConfiguration"/> resides in
		/// </summary>
		public const string Section = "FileLogging";

		/// <summary>
		/// Where log files are stored
		/// </summary>
		public string Directory { get; set; }
		
		/// <summary>
		/// If file logging is disabled
		/// </summary>
		public bool Disable { get; set; }

		/// <summary>
		/// The <see cref="string"/>ified minimum <see cref="Microsoft.Extensions.Logging.LogLevel"/> to display in logs
		/// </summary>
		[JsonConverter(typeof(StringEnumConverter))]
		public LogLevel LogLevel { get; set; }


		/// <summary>
		/// The <see cref="string"/>ified minimum <see cref="Microsoft.Extensions.Logging.LogLevel"/> to display in logs for Microsoft library sources
		/// </summary>
		[JsonConverter(typeof(StringEnumConverter))]
		public LogLevel MicrosoftLogLevel { get; set; }
	}
}
