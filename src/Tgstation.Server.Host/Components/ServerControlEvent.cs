using System;

namespace Tgstation.Server.Host.Components
{
	/// <summary>
	/// Represents a DreamDaemon control request or notification
	/// </summary>
	sealed class ServerControlEvent : EventArgs
	{
		/// <summary>
		/// The <see cref="ServerControlEventType"/>
		/// </summary>
		public ServerControlEventType EventType { get; set; }

		/// <summary>
		/// If the message was sent from the primary server
		/// </summary>
		public bool FromPrimaryServer { get; set; }
	}
}