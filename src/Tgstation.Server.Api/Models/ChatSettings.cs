using System.Collections.Generic;
using Tgstation.Server.Api.Rights;

namespace Tgstation.Server.Api.Models
{
	/// <summary>
	/// Manage the server chat bots
	/// </summary>
	[Model(RightsType.Chat, RequiresInstance = true)]
	public class ChatSettings
	{
		/// <summary>
		/// If the IRC client is enabled
		/// </summary>
		[Permissions(WriteRight = ChatRights.SetIrcEnabled)]
		public bool IrcEnabled { get; set; }

		/// <summary>
		/// The IRC server name
		/// </summary>
		[Permissions(ReadRight = ChatRights.SetIrcSettings, WriteRight = ChatRights.SetIrcSettings)]
		public string IrcHost { get; set; }

		/// <summary>
		/// The IRC server port
		/// </summary>
		[Permissions(ReadRight = ChatRights.SetIrcSettings, WriteRight = ChatRights.SetIrcSettings)]
		public ushort IrcPort { get; set; }

		/// <summary>
		/// The IRC server NickServ password
		/// </summary>
		[Permissions(ReadRight = ChatRights.SetIrcSettings, WriteRight = ChatRights.SetIrcSettings)]
		public string IrcNickServPassword { get; set; }

		/// <summary>
		/// Channels the IRC client should join/listen/announce in and allow admin commands
		/// </summary>
		[Permissions(WriteRight = ChatRights.SetIrcChannels)]
		public List<string> IrcAdminChannels { get; set; }

		/// <summary>
		/// Channels the IRC client should join/listen/announce in
		/// </summary>
		[Permissions(WriteRight = ChatRights.SetIrcChannels)]
		public List<string> IrcGeneralChannels { get; set; }

		/// <summary>
		/// If the Discord bot is enabled
		/// </summary>
		[Permissions(WriteRight = ChatRights.SetDiscordEnabled)]
		public bool DiscordEnabled { get; set; }

		/// <summary>
		/// The Discord bot token
		/// </summary>
		[Permissions(ReadRight = ChatRights.SetDiscordSettings, WriteRight = ChatRights.SetDiscordSettings)]
		public string DiscordBotToken { get; set; }

		/// <summary>
		/// Channels the Discord bot should listen/announce in and allow admin commands
		/// </summary>
		[Permissions(WriteRight = ChatRights.SetDiscordChannels)]
		public List<long> DiscordAdminChannels { get; set; }
		/// <summary>
		/// Channels the Discord bot should listen/announce in
		/// </summary>
		[Permissions(WriteRight = ChatRights.SetDiscordChannels)]
		public List<long> DiscordGeneralChannels { get; set; }
	}
}
