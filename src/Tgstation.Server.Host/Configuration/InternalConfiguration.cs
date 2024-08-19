using System;

namespace Tgstation.Server.Host.Configuration
{
	/// <summary>
	/// Unstable configuration options used internally by TGS.
	/// </summary>
	public sealed class InternalConfiguration
	{
		/// <summary>
		/// The key for the <see cref="Microsoft.Extensions.Configuration.IConfigurationSection"/> the <see cref="InternalConfiguration"/> resides in.
		/// </summary>
		public const string Section = "Internal";

		/// <summary>
		/// The name of the pipe opened by the host watchdog for sending commands, if any.
		/// </summary>
		public string? CommandPipe { get; set; }

		/// <summary>
		/// The name of the pipe opened by the host watchdog for receiving commands, if any.
		/// </summary>
		public string? ReadyPipe { get; set; }

		/// <summary>
		/// If the server is running under SystemD.
		/// </summary>
		public bool UsingSystemD { get; set; }

		/// <summary>
		/// If the server is running inside of a Docker container.
		/// </summary>
		public bool UsingDocker { get; set; }

		/// <summary>
		/// The base path for the app settings configuration files.
		/// </summary>
		public string AppSettingsBasePath { get; set; } = "UNINITIALIZED"; // this is set in a hacky way in ServerFactory

		/// <summary>
		/// Coerce the <see cref="Setup.SetupWizard"/> to select <see cref="DatabaseType.MariaDB"/>.
		/// </summary>
		public bool MariaDBSetup { get; set; }

		/// <summary>
		/// Generate default configuration using the given <see cref="DatabaseType.MariaDB"/> default password.
		/// </summary>
		public string? MariaDBDefaultRootPassword { get; set; }
	}
}
