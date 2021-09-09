using System.Runtime.Versioning;

namespace Tgstation.Server.Host.System
{
	/// <inheritdoc />
	[UnsupportedOSPlatform("windows")]
	sealed class PosixNetworkPromptReaper : INetworkPromptReaper
	{
		/// <inheritdoc />
		public void RegisterProcess(IProcess process)
		{
			// POSIX BYOND doesn't prompt you when you change the port
		}
	}
}
