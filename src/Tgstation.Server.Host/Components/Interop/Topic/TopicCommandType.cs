using System;

namespace Tgstation.Server.Host.Components.Interop.Topic
{
	/// <summary>
	/// The type of topic command being sent.
	/// </summary>
	enum TopicCommandType
	{
		/// <summary>
		/// Invoking a custom chat command.
		/// </summary>
		ChatCommand,

		/// <summary>
		/// Notification of a TGS event.
		/// </summary>
		EventNotification,

		/// <summary>
		/// DreamDaemon port change request.
		/// </summary>
		ChangePort,

		/// <summary>
		/// Reboot state change request.
		/// </summary>
		ChangeRebootState,

		/// <summary>
		/// The owning instance was renamed.
		/// </summary>
		InstanceRenamed,

		/// <summary>
		/// Chat channels were changed.
		/// </summary>
		ChatChannelsUpdate,

		/// <summary>
		/// The server's port was possibly changed.
		/// </summary>
		[Obsolete("Deprecated", true)]
		ServerPortUpdate,

		/// <summary>
		/// Ping to ensure the server is running.
		/// </summary>
		Heartbeat,

		/// <summary>
		/// Notify the server of a reattach and potentially new version.
		/// </summary>
		ServerRestarted
	}
}