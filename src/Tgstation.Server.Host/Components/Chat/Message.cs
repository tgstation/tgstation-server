using System;

namespace Tgstation.Server.Host.Components.Chat.Providers
{
	/// <summary>
	/// Represents a message recieved by a <see cref="IProvider"/>.
	/// </summary>
	sealed class Message
	{
		/// <summary>
		/// The text of the message.
		/// </summary>
		public string Content { get; }

		/// <summary>
		/// The <see cref="ChatUser"/> who sent the <see cref="Message"/>.
		/// </summary>
		public ChatUser User { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="Message"/> class.
		/// </summary>
		/// <param name="user">The value of <see cref="User"/>.</param>
		/// <param name="content">The value of <see cref="Content"/>.</param>
		public Message(ChatUser user, string content)
		{
			User = user ?? throw new ArgumentNullException(nameof(user));
			Content = content ?? throw new ArgumentNullException(nameof(content));
		}
	}
}
