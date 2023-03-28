using System.Collections.Generic;

namespace Tgstation.Server.Host.Components.Interop
{
	/// <summary>
	/// Represents a message to send to one or more <see cref="Chat.ChannelRepresentation"/>s.
	/// </summary>
	public sealed class ChatMessage : MessageContent
	{
		/// <summary>
		/// The <see cref="ICollection{T}"/> of <see cref="Chat.ChannelRepresentation.Id"/>s to sent the <see cref="MessageContent"/> to. Must be safe to parse as <see cref="ulong"/>s.
		/// </summary>
		public ICollection<string> ChannelIds { get; set; }
	}
}
