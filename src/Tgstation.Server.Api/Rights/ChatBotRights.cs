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
		WriteEnabled = 1 << 0,

		/// <summary>
		/// User can change <see cref="Models.Internal.ChatBotSettings.Provider"/>.
		/// </summary>
		WriteProvider = 1 << 1,

		/// <summary>
		/// User can change <see cref="Models.Internal.ChatBotApiBase.Channels"/>.
		/// </summary>
		WriteChannels = 1 << 2,

		/// <summary>
		/// User can change <see cref="Models.Internal.ChatBotSettings.ConnectionString"/>.
		/// </summary>
		WriteConnectionString = 1 << 3,

		/// <summary>
		/// User can read <see cref="Models.Internal.ChatBotSettings.ConnectionString"/> requires the <see cref="Read"/> permission.
		/// </summary>
		ReadConnectionString = 1 << 4,

		/// <summary>
		/// User can read all chat bot properties except <see cref="Models.Internal.ChatBotSettings.ConnectionString"/>.
		/// </summary>
		Read = 1 << 5,

		/// <summary>
		/// User can create new chat bots.
		/// </summary>
		Create = 1 << 6,

		/// <summary>
		/// User can delete chat bots.
		/// </summary>
		Delete = 1 << 7,

		/// <summary>
		/// User can change <see cref="Models.NamedEntity.Name"/>.
		/// </summary>
		WriteName = 1 << 8,

		/// <summary>
		/// User can change <see cref="Models.Internal.ChatBotSettings.ReconnectionInterval"/>.
		/// </summary>
		WriteReconnectionInterval = 1 << 9,

		/// <summary>
		/// User can change <see cref="Models.Internal.ChatBotSettings.ChannelLimit"/>.
		/// </summary>
		WriteChannelLimit = 1 << 10,
	}
}
