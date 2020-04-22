using System.Collections.Generic;

namespace Tgstation.Server.Host.Components.Interop.Topic
{
	sealed class TopicResponse
	{
		public string ErrorMessage { get; set; }

		public string CommandResponseMessage { get; set; }

		public ICollection<ChatMessage> ChatResponses { get; set; }
	}
}
