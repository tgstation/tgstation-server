using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Tgstation.Server.Api.Rights;

namespace Tgstation.Server.Api.Models.Internal
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
		[Permissions(WriteRight = ChatSettingsRights.SetIrcEnabled)]
		public bool IrcEnabled { get; set; }

		/// <summary>
		/// The IRC server name
		/// </summary>
		[Permissions(ReadRight = ChatSettingsRights.SetIrcSettings, WriteRight = ChatSettingsRights.SetIrcSettings)]
		[Required]
		public string IrcHost { get; set; }

		/// <summary>
		/// The IRC server port
		/// </summary>
		[Permissions(ReadRight = ChatSettingsRights.SetIrcSettings, WriteRight = ChatSettingsRights.SetIrcSettings)]
		public ushort IrcPort { get; set; }

		/// <summary>
		/// The IRC server NickServ password
		/// </summary>
		[Permissions(ReadRight = ChatSettingsRights.SetIrcSettings, WriteRight = ChatSettingsRights.SetIrcSettings)]
		public string IrcNickServPassword { get; set; }

		/// <summary>
		/// If the Discord bot is enabled
		/// </summary>
		[Permissions(WriteRight = ChatSettingsRights.SetDiscordEnabled)]
		public bool DiscordEnabled { get; set; }

		/// <summary>
		/// The Discord bot token
		/// </summary>
		[Permissions(ReadRight = ChatSettingsRights.SetDiscordSettings, WriteRight = ChatSettingsRights.SetDiscordSettings)]
		public string DiscordBotToken { get; set; }
	}
}
