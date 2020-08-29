using System;

namespace Tgstation.Server.Api.Rights
{
	/// <summary>
	/// Rights for <see cref="Models.ChatBot"/>
	/// </summary>
	[Flags]
	public enum ChatBotRights : ulong
	{
		/// <summary>
		/// User has no rights.
		/// </summary>
		None = 0,

		/// <summary>
		/// User can change <see cref="Models.Internal.ChatBot.Enabled"/>.
		/// </summary>
		WriteEnabled = 1,

		/// <summary>
		/// User can change <see cref="Models.Internal.ChatBot.Provider"/>.
		/// </summary>
		WriteProvider = 2,

		/// <summary>
		/// User can change <see cref="Models.ChatBot.Channels"/>.
		/// </summary>
		WriteChannels = 4,

		/// <summary>
		/// User can change <see cref="Models.Internal.ChatBot.ConnectionString"/>
		/// </summary>
		WriteConnectionString = 8,

		/// <summary>
		/// User can read <see cref="Models.Internal.ChatBot.ConnectionString"/> requires the <see cref="Read"/> permission.
		/// </summary>
		ReadConnectionString = 16,

		/// <summary>
		/// User can read all <see cref="Models.ChatBot"/> properties except <see cref="Models.Internal.ChatBot.ConnectionString"/>
		/// </summary>
		Read = 32,

		/// <summary>
		/// User can create new <see cref="Models.ChatBot"/>s.
		/// </summary>
		Create = 64,

		/// <summary>
		/// User can delete <see cref="Models.ChatBot"/>s.
		/// </summary>
		Delete = 128,

		/// <summary>
		/// User can change <see cref="Models.Internal.ChatBot.Name"/>.
		/// </summary>
		WriteName = 256,

		/// <summary>
		/// User can change <see cref="Models.Internal.ChatBot.ReconnectionInterval"/>.
		/// </summary>
		WriteReconnectionInterval = 512,

		/// <summary>
		/// User can change <see cref="Models.Internal.ChatBot.ChannelLimit"/>.
		/// </summary>
		WriteChannelLimit = 1024,
	}
}
