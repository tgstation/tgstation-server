using System;
using System.Reflection;

using Tgstation.Server.Common.Extensions;

namespace Tgstation.Server.Host.Watchdog
{
	/// <summary>
	/// Settings for the bootstrapper feature.
	/// </summary>
	sealed class BootstrapSettings
	{
		/// <summary>
		/// The current supported major version of <see cref="FileVersion"/>.
		/// </summary>
		public const int FileMajorVersion = 1;

		/// <summary>
		/// The token used to substitute <see cref="ServerUpdatePackageUrlFormatter"/>.
		/// </summary>
		public const string VersionSubstitutionToken = "${version}";

		/// <summary>
		/// The version of the boostrapper file.
		/// </summary>
		public Version FileVersion { get; set; } = new Version(FileMajorVersion, 0, 0);

		/// <summary>
		/// The <see cref="Version"/> of TGS last launched in the lib/Default directory.
		/// </summary>
		public Version TgsVersion { get; set; } = Assembly.GetEntryAssembly()!.GetName().Version!.Semver();

		/// <summary>
		/// The URL to format with <see cref="TgsVersion"/> to get the download URL.
		/// </summary>
		public string ServerUpdatePackageUrlFormatter { get; set; } = $"https://github.com/tgstation/tgstation-server/releases/download/tgstation-server-v{VersionSubstitutionToken}/ServerUpdatePackage.zip";
	}
}
