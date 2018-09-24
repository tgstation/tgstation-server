using Tgstation.Server.Host.Core;

namespace Tgstation.Server.Host.Components.Watchdog
{
	//POSIX BYOND doesn't prompt you when you change the port
	/// <inheritdoc />
	sealed class PosixNetworkPromptReaper : INetworkPromptReaper
	{
		/// <inheritdoc />
		public void RegisterProcess(IProcess process) { }
	}
}
