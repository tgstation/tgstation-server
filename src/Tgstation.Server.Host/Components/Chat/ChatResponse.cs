using System.Collections.Generic;

namespace Tgstation.Server.Host.Components.Chat
{
	sealed class ChatResponse
	{
		public string Message { get; set; }
		public List<long> ChannelIds { get; set; }
	}
}
