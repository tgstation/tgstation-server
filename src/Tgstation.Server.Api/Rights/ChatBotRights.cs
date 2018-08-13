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
		/// User has no rights
		/// </summary>
		None = 0,
		/// <summary>
		/// User can change <see cref="Models.Internal.ChatBot.Enabled"/>
		/// </summary>
		WriteEnabled = 1,
		/// <summary>
		/// User can change <see cref="Models.Internal.ChatBot.Provider"/>
		/// </summary>
		WriteProvider = 2,
		/// <summary>
		/// User can change <see cref="Models.ChatBot.Channels"/>
		/// </summary>
		WriteChannels = 4,
		/// <summary>
		/// User can change <see cref="Models.Internal.ChatBot.ConnectionString"/>
		/// </summary>
		WriteConnectionString = 8,
		/// <summary>
		/// User can read <see cref="Models.Internal.ChatBot.ConnectionString"/> requires <see cref="Read"/>
		/// </summary>
		ReadConnectionString = 16,
		/// <summary>
		/// User can read all chat settings except <see cref="Models.Internal.ChatBot.ConnectionString"/>
		/// </summary>
		Read = 32,
		/// <summary>
		/// User can change <see cref="Models.Internal.ChatBot.Name"/>
		/// </summary>
		WriteName = 32,
		/// <summary>
		/// User can create new <see cref="Models.ChatBot"/>
		/// </summary>
		Create = 64,
		/// <summary>
		/// User can delete <see cref="Models.ChatBot"/>
		/// </summary>
		Delete = 128
	}
}
