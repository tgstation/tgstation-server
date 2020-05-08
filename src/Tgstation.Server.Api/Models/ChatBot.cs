using System;
using System.Collections.Generic;
using System.Linq;

namespace Tgstation.Server.Api.Models
{
	/// <inheritdoc />
	public sealed class ChatBot : Internal.ChatBot
	{
		/// <summary>
		/// Channels the Discord bot should listen/announce in
		/// </summary>
		public ICollection<ChatChannel> Channels { get; set; }

		/// <summary>
		/// Validates <see cref="Channels"/> are correct for the <see cref="Internal.ChatBot.Provider"/>
		/// </summary>
		/// <returns><see langword="true"/> if the <see cref="Channels"/> are valid for the <see cref="Internal.ChatBot.Provider"/>, <see langword="false"/> otherwise</returns>
		public bool ValidateProviderChannelTypes()
		{
			if (!Provider.HasValue)
				return true;
			switch (Provider.Value)
			{
				case ChatProvider.Discord:
					return Channels?.Select(x => x.DiscordChannelId.HasValue && x.IrcChannel == null).All(x => x) ?? true;
				case ChatProvider.Irc:
					return Channels?.Select(x => !x.DiscordChannelId.HasValue && x.IrcChannel != null).All(x => x) ?? true;
				default:
					throw new InvalidOperationException("Invalid provider type!");
			}
		}
	}
}
