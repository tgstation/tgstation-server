using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Tgstation.Server.Host.Components.Interop
{
	/// <summary>
	/// Represents a message to send to one or more <see cref="Chat.ChannelRepresentation"/>s.
	/// </summary>
	public sealed class ChatMessage
	{
		/// <summary>
		/// The message <see cref="string"/>.
		/// </summary>
		[Required]
		public string Text { get; init; } = null!;

		/// <summary>
		/// The <see cref="ICollection{T}"/> of <see cref="Chat.ChannelRepresentation.Id"/>s to sent the <see cref="Text"/> to. Must be safe to parse as <see cref="ulong"/>s.
		/// </summary>
		[Required]
		public ICollection<string> ChannelIds { get; init; } = null!;

		/// <summary>
		/// Initializes a new instance of the <see cref="ChatMessage"/> class.
		/// </summary>
		[Obsolete("For JSON deserialization only", true)]
		public ChatMessage()
		{
		}
	}
}
