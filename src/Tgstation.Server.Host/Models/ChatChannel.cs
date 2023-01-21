using System.ComponentModel.DataAnnotations;

using Tgstation.Server.Api.Models;
using Tgstation.Server.Api.Models.Internal;

namespace Tgstation.Server.Host.Models
{
	/// <inheritdoc />
	public sealed class ChatChannel : ChatChannelBase
	{
		/// <summary>
		/// The row Id.
		/// </summary>
		public long Id { get; set; }

		/// <summary>
		/// The <see cref="EntityId.Id"/>.
		/// </summary>
		public long ChatSettingsId { get; set; }

		/// <summary>
		/// See <see cref="Api.Models.ChatChannel.IrcChannel"/>.
		/// </summary>
		[StringLength(Limits.MaximumIndexableStringLength, MinimumLength = 1)]
		public string IrcChannel { get; set; }

		/// <summary>
		/// See <see cref="Api.Models.ChatChannel.DiscordChannelId"/>.
		/// </summary>
		public ulong? DiscordChannelId { get; set; }

		/// <summary>
		/// The <see cref="ChatBot"/>.
		/// </summary>
		public ChatBot ChatSettings { get; set; }

		/// <summary>
		/// Convert to a <see cref="Api.Models.ChatChannel"/>.
		/// </summary>
		/// <param name="chatProvider">The channel's <see cref="ChatProvider"/>.</param>
		/// <returns>The converted <see cref="Api.Models.ChatChannel"/>.</returns>
		public Api.Models.ChatChannel ToApi(ChatProvider chatProvider) => new Api.Models.ChatChannel
		{
			ChannelData = chatProvider == ChatProvider.Discord ? DiscordChannelId.ToString() : IrcChannel,
			DiscordChannelId = DiscordChannelId,
			IsAdminChannel = IsAdminChannel,
			IsWatchdogChannel = IsWatchdogChannel,
			IsUpdatesChannel = IsUpdatesChannel,
			IrcChannel = IrcChannel,
			Tag = Tag,
		};
	}
}
