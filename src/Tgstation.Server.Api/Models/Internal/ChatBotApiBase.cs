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
				ChatProvider.Discord => Channels?.All(x => UInt64.TryParse(x.ChannelData, out _)) ?? true,
				ChatProvider.Irc => Channels?.All(x => x.ChannelData != null && x.ChannelData[0] == '#') ?? true,
				_ => throw new InvalidOperationException("Invalid provider type!"),
			};
		}
	}
}
