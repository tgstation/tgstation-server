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
		public string Content { get; set; }

		/// <summary>
		/// The <see cref="ChatUser"/> who sent the <see cref="Message"/>.
		/// </summary>
		public ChatUser User { get; set; }
	}
}
