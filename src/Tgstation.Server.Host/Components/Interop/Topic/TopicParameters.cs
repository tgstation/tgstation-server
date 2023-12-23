using System;

using Newtonsoft.Json;

using Tgstation.Server.Host.Components.Session;

namespace Tgstation.Server.Host.Components.Interop.Topic
{
	/// <summary>
	/// Parameters for a topic request.
	/// </summary>
	class TopicParameters : DMApiParameters
	{
		/// <summary>
		/// The <see cref="TopicCommandType"/>.
		/// </summary>
		/// <remarks>This is <see cref="Nullable"/> but always set to work around a serialization issue.</remarks>
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
		/// The message to broadcast for <see cref="TopicCommandType.Broadcast"/> requests.
		/// </summary>
		public string? BroadcastMessage { get; }

		/// <summary>
		/// The <see cref="Interop.ChatUpdate"/> for <see cref="TopicCommandType.ChatChannelsUpdate"/> requests.
		/// </summary>
		public ChatUpdate? ChatUpdate { get; }

		/// <summary>
		/// The new server <see cref="Version"/> after a reattach.
		/// </summary>
		public Version? NewServerVersion { get; }

		/// <summary>
		/// The <see cref="ChunkData"/> for a partial request.
		/// </summary>
		public ChunkData? Chunk { get; }

		/// <summary>
		/// Whether or not the <see cref="TopicParameters"/> constitute a priority request.
		/// </summary>
		[JsonIgnore]
		public bool IsPriority => CommandType switch
		{
			TopicCommandType.EventNotification
			or TopicCommandType.ChangePort
			or TopicCommandType.ChangeRebootState
			or TopicCommandType.InstanceRenamed
			or TopicCommandType.ChatChannelsUpdate
			or TopicCommandType.Broadcast
			or TopicCommandType.ServerRestarted => true,
			TopicCommandType.ChatCommand
			or TopicCommandType.HealthCheck
			or TopicCommandType.ReceiveChunk => false,
			TopicCommandType.SendChunk => throw new InvalidOperationException("SendChunk topic priority should be based on the original TopicParameters!"),
			_ => throw new InvalidOperationException($"Invalid value for {nameof(CommandType)}: {CommandType}"),
		};

		/// <summary>
		/// Initializes a new instance of the <see cref="TopicParameters"/> class.
		/// </summary>
		/// <param name="newInstanceName">The value of <see cref="NewInstanceName"/>.</param>
		/// <returns>The created <see cref="TopicParameters"/>.</returns>
		public static TopicParameters CreateInstanceRenamedTopicParameters(string newInstanceName)
			=> new(
				newInstanceName ?? throw new ArgumentNullException(nameof(newInstanceName)),
				TopicCommandType.InstanceRenamed);

		/// <summary>
		/// Initializes a new instance of the <see cref="TopicParameters"/> class.
		/// </summary>
		/// <param name="broadcastMessage">The value of <see cref="BroadcastMessage"/>.</param>
		/// <returns>The created <see cref="TopicParameters"/>.</returns>
		public static TopicParameters CreateBroadcastParameters(string broadcastMessage)
			=> new(
				broadcastMessage ?? throw new ArgumentNullException(nameof(broadcastMessage)),
				TopicCommandType.Broadcast);

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
		/// <param name="chunk">The value of <see cref="Chunk"/>.</param>
		public TopicParameters(ChunkData chunk)
			: this(TopicCommandType.SendChunk)
		{
			Chunk = chunk ?? throw new ArgumentNullException(nameof(chunk));
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="TopicParameters"/> class.
		/// </summary>
		/// <remarks>Constructor for <see cref="TopicCommandType.HealthCheck"/>s.</remarks>
		public TopicParameters()
			: this(TopicCommandType.HealthCheck)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="TopicParameters"/> class.
		/// </summary>
		/// <param name="commandType">The value of <see cref="CommandType"/>.</param>
		protected TopicParameters(TopicCommandType commandType)
		{
			CommandType = commandType;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="TopicParameters"/> class.
		/// </summary>
		/// <param name="stringCommand">The <see cref="string"/> parameter for the property designated by <paramref name="stringCommandType"/>.</param>
		/// <param name="stringCommandType">The value of <see cref="CommandType"/>.</param>
		TopicParameters(string stringCommand, TopicCommandType stringCommandType)
			: this(stringCommandType)
		{
#pragma warning disable IDE0010 // Add missing cases
			switch (stringCommandType)
			{
				case TopicCommandType.InstanceRenamed:
					NewInstanceName = stringCommand;
					break;
				case TopicCommandType.Broadcast:
					BroadcastMessage = stringCommand;
					break;
				default:
					throw new InvalidOperationException($"Invalid string TopicCommandType: {stringCommandType}");
			}
#pragma warning restore IDE0010 // Add missing cases
		}
	}
}
