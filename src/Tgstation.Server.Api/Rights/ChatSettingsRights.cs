using System;

namespace Tgstation.Server.Api.Rights
{
	/// <summary>
	/// Rights for <see cref="Models.ChatSettings"/>
	/// </summary>
	[Flags]
	public enum ChatSettingsRights
	{
		/// <summary>
		/// User has no rights
		/// </summary>
		None = 0,
		/// <summary>
		/// User can enable/disable the IRC client
		/// </summary>
		SetIrcEnabled = 1,
		/// <summary>
		/// User can change the IRC settings
		/// </summary>
		SetIrcSettings = 2,
		/// <summary>
		/// User can change the chat channels
		/// </summary>
		SetChannels = 4,
		/// <summary>
		/// User can enable/disable the Discord bot
		/// </summary>
		SetDiscordEnabled = 8,
		/// <summary>
		/// User can change the Discord settings
		/// </summary>
		SetDiscordSettings = 16,
	}
}
