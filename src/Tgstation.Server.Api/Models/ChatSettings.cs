using System.Collections.Generic;
using Tgstation.Server.Api.Rights;

namespace Tgstation.Server.Api.Models
{
	/// <inheritdoc />
	public sealed class ChatSettings : Internal.ChatSettings
	{
		/// <summary>
		/// Channels the bot should listen/announce in and allow admin commands
		/// </summary>
		[Permissions(WriteRight = ChatSettingsRights.SetDiscordChannels)]
		public List<ChatChannel> AdminChannels { get; set; }

		/// <summary>
		/// Channels the Discord bot should listen/announce in
		/// </summary>
		[Permissions(WriteRight = ChatSettingsRights.SetDiscordChannels)]
		public List<ChatChannel> GeneralChannels { get; set; }
	}
}
