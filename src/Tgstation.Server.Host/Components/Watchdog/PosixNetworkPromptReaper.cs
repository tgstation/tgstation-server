using Tgstation.Server.Host.System;

namespace Tgstation.Server.Host.Components.Watchdog
{
	/// <inheritdoc />
	sealed class PosixNetworkPromptReaper : INetworkPromptReaper
	{
		/// <inheritdoc />
		public void RegisterProcess(IProcess process)
		{
			// POSIX BYOND doesn't prompt you when you change the port
		}
	}
}
