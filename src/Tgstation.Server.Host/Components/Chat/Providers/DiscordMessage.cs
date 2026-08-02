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
		/// The Discord application ID, if the source was an interaction.
		/// </summary>
		public Snowflake? ApplicationId { get; }

		/// <summary>
		/// The Discord interaction token, if the source was an interaction.
		/// </summary>
		public string? InteractionToken { get; }

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
		/// <param name="applicationId">The value of <see cref="ApplicationId"/>.</param>
		/// <param name="interactionToken">The value of <see cref="InteractionToken"/>.</param>
		public DiscordMessage(ChatUser user, string content, Optional<IMessageReference> messageReference, Snowflake? applicationId = null, string? interactionToken = null)
			: base(
				  user,
				  content)
		{
			MessageReference = messageReference;
			ApplicationId = applicationId;
			InteractionToken = interactionToken;
		}
	}
}
