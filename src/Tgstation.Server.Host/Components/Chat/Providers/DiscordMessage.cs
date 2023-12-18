using Remora.Discord.API.Abstractions.Objects;
using Remora.Rest.Core;

namespace Tgstation.Server.Host.Components.Chat.Providers
{
	/// <summary>
	/// A <see cref="Message"/> containing the source <see cref="IMessageReference"/>.
	/// </summary>
	sealed class DiscordMessage : Message
	{
		/// <summary>
		/// The <see cref="IMessageReference"/> of the source <see cref="Message"/>.
		/// </summary>
		public Optional<IMessageReference> MessageReference { get; set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="DiscordMessage"/> class.
		/// </summary>
		/// <param name="user">The value of <see cref="Message.User"/>.</param>
		/// <param name="content">The value of <see cref="Message.Content"/>.</param>
		/// <param name="messageReference">The value of <see cref="MessageReference"/>.</param>
		public DiscordMessage(ChatUser user, string content, Optional<IMessageReference> messageReference)
			: base(
				  user,
				  content)
		{
			MessageReference = messageReference;
		}
	}
}
