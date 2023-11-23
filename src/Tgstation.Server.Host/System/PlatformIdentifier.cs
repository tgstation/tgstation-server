﻿using System.Runtime.InteropServices;
using System.Runtime.Versioning;

#nullable disable

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
			IsWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
			ScriptFileExtension = IsWindows ? "bat" : "sh";
		}
	}
}
