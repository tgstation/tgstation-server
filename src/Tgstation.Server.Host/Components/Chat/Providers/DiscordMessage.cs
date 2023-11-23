using Remora.Discord.API.Abstractions.Objects;
using Remora.Rest.Core;

#nullable disable

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
	}
}
