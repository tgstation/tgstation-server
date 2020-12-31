﻿using System;
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
		public ICollection<ChatChannel>? Channels { get; set; }

		/// <summary>
		/// Validates <see cref="Channels"/> are correct for the <see cref="Internal.ChatBot.Provider"/>
		/// </summary>
		/// <returns><see langword="true"/> if the <see cref="Channels"/> are valid for the <see cref="Internal.ChatBot.Provider"/>, <see langword="false"/> otherwise</returns>
		public bool ValidateProviderChannelTypes()
		{
			if (!Provider.HasValue)
				return true;
			return Provider.Value switch
			{
				ChatProvider.Discord => Channels?.Select(x => x.DiscordChannelId.HasValue && x.IrcChannel == null).All(x => x) ?? true,
				ChatProvider.Irc => Channels?.Select(x => !x.DiscordChannelId.HasValue && x.IrcChannel != null).All(x => x) ?? true,
				_ => throw new InvalidOperationException("Invalid provider type!"),
			};
		}
	}
}
