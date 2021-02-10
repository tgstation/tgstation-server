using System;

namespace Tgstation.Server.Api.Rights
{
	/// <summary>
	/// Rights for chat bots.
	/// </summary>
	[Flags]
	public enum ChatBotRights : ulong
	{
		/// <summary>
		/// User has no rights.
		/// </summary>
		None = 0,

		/// <summary>
		/// User can change <see cref="Models.Internal.ChatBotSettings.Enabled"/>.
		/// </summary>
		WriteEnabled = 1,

		/// <summary>
		/// User can change <see cref="Models.Internal.ChatBotSettings.Provider"/>.
		/// </summary>
		WriteProvider = 2,

		/// <summary>
		/// User can change <see cref="Models.Internal.ChatBotApiBase.Channels"/>.
		/// </summary>
		WriteChannels = 4,

		/// <summary>
		/// User can change <see cref="Models.Internal.ChatBotSettings.ConnectionString"/>
		/// </summary>
		WriteConnectionString = 8,

		/// <summary>
		/// User can read <see cref="Models.Internal.ChatBotSettings.ConnectionString"/> requires the <see cref="Read"/> permission.
		/// </summary>
		ReadConnectionString = 16,

		/// <summary>
		/// User can read all chat bot properties except <see cref="Models.Internal.ChatBotSettings.ConnectionString"/>
		/// </summary>
		Read = 32,

		/// <summary>
		/// User can create new chat bots.
		/// </summary>
		Create = 64,

		/// <summary>
		/// User can delete chat bots.
		/// </summary>
		Delete = 128,

		/// <summary>
		/// User can change <see cref="Models.NamedEntity.Name"/>.
		/// </summary>
		WriteName = 256,

		/// <summary>
		/// User can change <see cref="Models.Internal.ChatBotSettings.ReconnectionInterval"/>.
		/// </summary>
		WriteReconnectionInterval = 512,

		/// <summary>
		/// User can change <see cref="Models.Internal.ChatBotSettings.ChannelLimit"/>.
		/// </summary>
		WriteChannelLimit = 1024,
	}
}
