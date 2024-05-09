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
		HealthCheck,

		/// <summary>
		/// Notify the server of a reattach and potentially new version.
		/// </summary>
		ServerRestarted,

		/// <summary>
		/// Part of a larger topic.
		/// </summary>
		SendChunk,

		/// <summary>
		/// Receive additional data for a previous response.
		/// </summary>
		ReceiveChunk,

		/// <summary>
		/// Sending a broadcast message.
		/// </summary>
		Broadcast,

		/// <summary>
		/// Notifying about the completion of a custom event.
		/// </summary>
		CompleteEvent,
	}
}
