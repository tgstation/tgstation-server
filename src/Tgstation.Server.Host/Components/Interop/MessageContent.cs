#nullable disable

namespace Tgstation.Server.Host.Components.Interop
{
	/// <summary>
	/// Represents a message to send to a chat provider.
	/// </summary>
	public class MessageContent
	{
		/// <summary>
		/// The message <see cref="string"/>.
		/// </summary>
		public string Text { get; set; }

		/// <summary>
		/// The <see cref="ChatEmbed"/>.
		/// </summary>
		public ChatEmbed Embed { get; set; }
	}
}
