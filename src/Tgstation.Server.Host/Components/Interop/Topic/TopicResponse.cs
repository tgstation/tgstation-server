using System.Collections.Generic;

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
	}
}
