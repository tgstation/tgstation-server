using System;
using System.Runtime.Versioning;

namespace Tgstation.Server.Host.System
{
	/// <inheritdoc />
	sealed class PlatformIdentifier : IPlatformIdentifier
	{
		/// <inheritdoc />
		[SupportedOSPlatformGuard("windows")]
		public bool IsWindows { get; }

		/// <inheritdoc />
		public string ScriptFileExtension { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="PlatformIdentifier"/> class.
		/// </summary>
		public PlatformIdentifier()
		{
			IsWindows = OperatingSystem.IsWindows();
			ScriptFileExtension = IsWindows ? "bat" : "sh";
		}
	}
}
