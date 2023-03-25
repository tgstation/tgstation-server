using System;
using System.Collections.Generic;
using System.Linq;

namespace Tgstation.Server.Api.Models.Internal
{
	/// <inheritdoc />
	public abstract class ChatBotApiBase : ChatBotSettings
	{
		/// <summary>
		/// Channels the Discord bot should listen/announce in.
		/// </summary>
		public ICollection<ChatChannel>? Channels { get; set; }

		/// <summary>
		/// Validates <see cref="Channels"/> are correct for the <see cref="ChatBotSettings.Provider"/>.
		/// </summary>
		/// <returns><see langword="true"/> if the <see cref="Channels"/> are valid for the <see cref="ChatBotSettings.Provider"/>, <see langword="false"/> otherwise.</returns>
		public bool ValidateProviderChannelTypes()
		{
			if (!Provider.HasValue)
				return true;
			return Provider.Value switch
			{
#pragma warning disable CS0618
				ChatProvider.Discord => Channels?.Select(x => (x.DiscordChannelId.HasValue || ulong.TryParse(x.ChannelData, out _)) && x.IrcChannel == null).All(x => x) ?? true,
				ChatProvider.Irc => Channels?.Select(x => !x.DiscordChannelId.HasValue && (x.IrcChannel != null || x.ChannelData != null)).All(x => x) ?? true,
#pragma warning restore CS0618
				_ => throw new InvalidOperationException("Invalid provider type!"),
			};
		}
	}
}
