using System;

namespace Tgstation.Server.Api.Rights
{
	/// <summary>
	/// Rights for <see cref="Models.Chat"/>
	/// </summary>
	[Flags]
	public enum ChatRights
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
		SetIrcSettings = 4,
		/// <summary>
		/// User can change the IRC channels
		/// </summary>
		SetIrcChannels = 8,
		/// <summary>
		/// User can enable/disable the Discord bot
		/// </summary>
		SetDiscordEnabled = 16,
		/// <summary>
		/// User can change the Discord settings
		/// </summary>
		SetDiscordSettings = 32,
		/// <summary>
		/// User can change the Discord channels
		/// </summary>
		SetDiscordChannels = 8,
	}
}
