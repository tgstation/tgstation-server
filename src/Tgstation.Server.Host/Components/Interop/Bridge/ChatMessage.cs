using System.Collections.Generic;

namespace Tgstation.Server.Host.Components.Interop.Bridge
{
	public sealed class ChatMessage
	{
		public string Text { get; set; }

		public ICollection<ulong> ChannelIds { get; set; }
	}
}