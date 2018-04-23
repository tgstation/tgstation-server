using System.Collections.Generic;
using Tgstation.Server.Api.Rights;

namespace Tgstation.Server.Api.Models
{
	/// <inheritdoc />
	public sealed class ChatSettings : Internal.ChatSettings
	{
		/// <summary>
		/// If the IRC connection is established
		/// </summary>
		[Permissions(DenyWrite = true)]
		bool IrcConnected { get; set; }

		/// <summary>
		/// If the Discord connection is established
		/// </summary>
		[Permissions(DenyWrite = true)]
		bool DiscordConnected { get; set; }

		/// <summary>
		/// Channels the Discord bot should listen/announce in
		/// </summary>
		[Permissions(WriteRight = ChatSettingsRights.SetChannels)]
		public List<ChatChannel> Channels { get; set; }
	}
}
