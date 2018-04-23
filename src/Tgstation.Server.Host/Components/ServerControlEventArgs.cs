using System;

namespace Tgstation.Server.Host.Components
{
	/// <summary>
	/// Represents a DreamDaemon control request or notification
	/// </summary>
	sealed class ServerControlEventArgs : EventArgs
	{
		/// <summary>
		/// If <see langword="true"/> the event will result in a new run of the world
		/// </summary>
		bool ServerReboot { get; set; }

		/// <summary>
		/// If <see langword="true"/> the event will result in a restart of the process. If both this and <see cref="ServerReboot"/> are <see langword="false"/>, the server is being terminated
		/// </summary>
		bool ProcessRestart { get; set; }
		
		/// <summary>
		/// If the action to be taken is graceful
		/// </summary>
		bool Graceful { get; set; }
	}
}