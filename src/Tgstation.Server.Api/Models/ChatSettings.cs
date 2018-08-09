using System;
using System.Collections.Generic;
using System.Linq;
using Tgstation.Server.Api.Rights;

namespace Tgstation.Server.Api.Models
{
	/// <inheritdoc />
	public sealed class ChatSettings : Internal.ChatSettings
	{
		/// <summary>
		/// Channels the Discord bot should listen/announce in
		/// </summary>
		[Permissions(WriteRight = ChatSettingsRights.WriteChannels)]
		public List<ChatChannel> Channels { get; set; }

		/// <summary>
		/// Validates <see cref="Channels"/> are correct for the <see cref="Internal.ChatSettings.Provider"/>
		/// </summary>
		/// <returns></returns>
		public bool ValidateProviderChannelTypes()
		{
			switch (Provider)
			{
				case ChatProvider.Discord:
					return Channels.Select(x => x.DiscordChannelId.HasValue && x.IrcChannel == null).All(x => x);
				case ChatProvider.Irc:
					return Channels.Select(x => !x.DiscordChannelId.HasValue && x.IrcChannel != null).All(x => x);
				default:
					throw new InvalidOperationException("Invalid provider type!");
			}
		}
	}
}
