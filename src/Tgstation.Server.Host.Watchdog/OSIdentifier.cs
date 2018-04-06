using System.Runtime.InteropServices;

namespace Tgstation.Server.Host.Watchdog
{
	/// <inheritdoc />
	sealed class OSIdentifier : IOSIdentifier
	{
		/// <inheritdoc />
		public bool IsWindows => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
	}
}