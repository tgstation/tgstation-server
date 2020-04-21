using System;
using Tgstation.Server.Host.Components.Watchdog;

namespace Tgstation.Server.Host.Components.Interop.Topic
{
	sealed class TopicParameters : DMApiParameters
	{
		public TopicCommandType CommandType { get; }

		public ChatCommand ChatCommand { get; }

		public EventNotification EventNotification { get; }

		public ushort? NewPort { get; }

		public RebootState? NewRebootState { get; }
		public string NewInstanceName { get; }

		private TopicParameters(TopicCommandType commandType)
		{
			CommandType = commandType;
		}

		public TopicParameters(ChatCommand chatCommand)
			: this(TopicCommandType.ChatCommand)
		{
			ChatCommand = chatCommand ?? throw new ArgumentNullException(nameof(chatCommand));
		}

		public TopicParameters(EventNotification eventNotification)
			: this(TopicCommandType.Event)
		{
			EventNotification = eventNotification ?? throw new ArgumentNullException(nameof(eventNotification));
		}

		public TopicParameters(ushort newPort)
			: this(TopicCommandType.ChangePort)
		{
			NewPort = newPort;
		}

		public TopicParameters(RebootState newRebootState)
			: this(TopicCommandType.ChangeRebootState)
		{
			NewRebootState = newRebootState;
		}

		public TopicParameters(string newInstanceName)
			: this(TopicCommandType.InstanceRenamed)
		{
			NewInstanceName = newInstanceName ?? throw new ArgumentNullException(nameof(newInstanceName));
		}
	}
}
