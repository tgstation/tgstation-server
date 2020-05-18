using System.Collections.Generic;
using Tgstation.Server.Host.Components.Chat.Commands;

namespace Tgstation.Server.Host.Components.Interop.Topic
{
	/// <summary>
	/// A response to a topic request.
	/// </summary>
	sealed class TopicResponse : DMApiResponse
	{
		/// <summary>
		/// The text to reply with as the result of a <see cref="TopicCommandType.ChatCommand"/> request, if any.
		/// </summary>
		public string CommandResponseMessage { get; set; }

		/// <summary>
		/// The <see cref="ChatMessage"/>s to send as the result of a <see cref="TopicCommandType.EventNotification"/> request, if any.
		/// </summary>
		public ICollection<ChatMessage> ChatResponses { get; set; }

		/// <summary>
		/// The DMAPI <see cref="CustomCommand"/>s for <see cref="TopicCommandType.ServerRestarted"/> requests.
		/// </summary>
		public ICollection<CustomCommand> CustomCommands { get; set; }
	}
}
