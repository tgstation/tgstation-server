using System;

using Tgstation.Server.Host.Components.Session;

namespace Tgstation.Server.Host.Components.Interop.Topic
{
	/// <summary>
	/// Parameters for a topic request.
	/// </summary>
	sealed class TopicParameters : DMApiParameters
	{
		/// <summary>
		/// The <see cref="TopicCommandType"/>.
		/// </summary>
		/// <remarks>This is not actually <see cref="Nullable"/> but always set to work around serializing it causing a default entry.</remarks>
		public TopicCommandType? CommandType { get; }

		/// <summary>
		/// The <see cref="Topic.ChatCommand"/> for <see cref="TopicCommandType.ChatCommand"/> requests.
		/// </summary>
		public ChatCommand? ChatCommand { get; }

		/// <summary>
		/// The <see cref="Topic.EventNotification"/> for <see cref="TopicCommandType.EventNotification"/> requests.
		/// </summary>
		public EventNotification? EventNotification { get; }

		/// <summary>
		/// The new port for <see cref="TopicCommandType.ChangePort"/> or <see cref="TopicCommandType.ServerPortUpdate"/> requests.
		/// </summary>
		public ushort? NewPort { get; }

		/// <summary>
		/// The <see cref="RebootState"/> for <see cref="TopicCommandType.ChangeRebootState"/> requests.
		/// </summary>
		public RebootState? NewRebootState { get; }

		/// <summary>
		/// The new <see cref="Api.Models.NamedEntity.Name"/> for <see cref="TopicCommandType.InstanceRenamed"/> requests.
		/// </summary>
		public string? NewInstanceName { get; }

		/// <summary>
		/// The <see cref="Interop.ChatUpdate"/> for <see cref="TopicCommandType.ChatChannelsUpdate"/> requests.
		/// </summary>
		public ChatUpdate? ChatUpdate { get; }

		/// <summary>
		/// The new server <see cref="Version"/> after a reattach.
		/// </summary>
		public Version? NewServerVersion { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="TopicParameters"/> class.
		/// </summary>
		/// <param name="chatCommand">The value of <see cref="ChatCommand"/>.</param>
		public TopicParameters(ChatCommand chatCommand)
			: this(TopicCommandType.ChatCommand)
		{
			ChatCommand = chatCommand ?? throw new ArgumentNullException(nameof(chatCommand));
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="TopicParameters"/> class.
		/// </summary>
		/// <param name="eventNotification">The value of <see cref="EventNotification"/>.</param>
		public TopicParameters(EventNotification eventNotification)
			: this(TopicCommandType.EventNotification)
		{
			EventNotification = eventNotification ?? throw new ArgumentNullException(nameof(eventNotification));
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="TopicParameters"/> class.
		/// </summary>
		/// <param name="newPort">The value of <see cref="NewPort"/>.</param>
		public TopicParameters(ushort newPort)
			: this(TopicCommandType.ChangePort)
		{
			NewPort = newPort;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="TopicParameters"/> class.
		/// </summary>
		/// <param name="newRebootState">The value of <see cref="NewRebootState"/>.</param>
		public TopicParameters(RebootState newRebootState)
			: this(TopicCommandType.ChangeRebootState)
		{
			NewRebootState = newRebootState;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="TopicParameters"/> class.
		/// </summary>
		/// <param name="newInstanceName">The value of <see cref="NewInstanceName"/>.</param>
		public TopicParameters(string newInstanceName)
			: this(TopicCommandType.InstanceRenamed)
		{
			NewInstanceName = newInstanceName ?? throw new ArgumentNullException(nameof(newInstanceName));
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="TopicParameters"/> class.
		/// </summary>
		/// <param name="channelsUpdate">The value of <see cref="ChatUpdate"/>.</param>
		public TopicParameters(ChatUpdate channelsUpdate)
			: this(TopicCommandType.ChatChannelsUpdate)
		{
			ChatUpdate = channelsUpdate ?? throw new ArgumentNullException(nameof(channelsUpdate));
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="TopicParameters"/> class.
		/// </summary>
		/// <param name="newServerVersion">The value of <see cref="NewServerVersion"/>.</param>
		/// <param name="serverPort">TGS's new API port.</param>
		public TopicParameters(Version newServerVersion, ushort serverPort)
			: this(TopicCommandType.ServerRestarted)
		{
			NewServerVersion = newServerVersion ?? throw new ArgumentNullException(nameof(newServerVersion));
			NewPort = serverPort;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="TopicParameters"/> class.
		/// </summary>
		/// <remarks>Constructor for <see cref="TopicCommandType.Heartbeat"/>s.</remarks>
		public TopicParameters()
			: this(TopicCommandType.Heartbeat)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="TopicParameters"/> class.
		/// </summary>
		/// <param name="commandType">The value of <see cref="CommandType"/>.</param>
		TopicParameters(TopicCommandType commandType)
			: base(String.Empty) // access identifier set later
		{
			CommandType = commandType;
		}
	}
}
