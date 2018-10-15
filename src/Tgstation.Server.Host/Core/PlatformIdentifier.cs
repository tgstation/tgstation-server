using System.Runtime.InteropServices;

namespace Tgstation.Server.Host.Core
{
	/// <inheritdoc />
	sealed class PlatformIdentifier : IPlatformIdentifier
	{
		/// <inheritdoc />
		public bool IsWindows { get; }

		/// <summary>
		/// Construct a <see cref="PlatformIdentifier"/>
		/// </summary>
		public PlatformIdentifier()
		{
			IsWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
		}
	}
}
