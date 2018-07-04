using System;

namespace Tgstation.Server.Host.Components.Watchdog
{
	/// <summary>
	/// Parameters necessary for duplicating a <see cref="ISessionController"/> session
	/// </summary>
	sealed class ReattachInformation
	{
		/// <summary>
		/// Used to identify and authenticate the DreamDaemon instance
		/// </summary>
		public string AccessIdentifier { get; set; }

		/// <summary>
		/// The system process ID
		/// </summary>
		public int ProcessId { get; set; }

		/// <summary>
		/// If the <see cref="IDmbProvider.PrimaryDirectory"/> of <see cref="Dmb"/> is being used
		/// </summary>
		public bool IsPrimary { get; set; }

		/// <summary>
		/// The port DreamDaemon was last listening on
		/// </summary>
		public ushort Port { get; set; }

		/// <summary>
		/// The current DreamDaemon reboot state
		/// </summary>
		public RebootState RebootState { get; set; }

		/// <summary>
		/// The <see cref="IDmbProvider"/> used by DreamDaemon
		/// </summary>
		public IDmbProvider Dmb { get; set; }
		
		/// <summary>
		/// Path to the chat commands json file
		/// </summary>
		public string ChatCommandsJson { get; set; }

		/// <summary>
		/// Path to the chat channels json file
		/// </summary>
		public string ChatChannelsJson { get; set; }
	}
}