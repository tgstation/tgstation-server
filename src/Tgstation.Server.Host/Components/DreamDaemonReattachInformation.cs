using System;

namespace Tgstation.Server.Host.Components
{
	/// <summary>
	/// Parameters necessary for duplicating a <see cref="IDreamDaemonControl"/> session
	/// </summary>
	sealed class DreamDaemonReattachInformation
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
		public DreamDaemonRebootState RebootState { get; set; }

		/// <summary>
		/// The <see cref="IDmbProvider"/> used by DreamDaemon
		/// </summary>
		public IDmbProvider Dmb { get; set; }
	}
}