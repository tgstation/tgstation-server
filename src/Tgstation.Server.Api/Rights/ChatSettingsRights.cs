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
		/// User can change <see cref="Models.Internal.ChatSettings.Enabled"/>
		/// </summary>
		WriteEnabled = 1,
		/// <summary>
		/// User can change <see cref="Models.Internal.ChatSettings.Provider"/>
		/// </summary>
		WriteProvider = 2,
		/// <summary>
		/// User can change <see cref="Models.ChatSettings.Channels"/>
		/// </summary>
		WriteChannels = 4,
		/// <summary>
		/// User can change <see cref="Models.Internal.ChatSettings.ConnectionString"/>
		/// </summary>
		WriteConnectionString = 8,
		/// <summary>
		/// User can read <see cref="Models.Internal.ChatSettings.ConnectionString"/>
		/// </summary>
		ReadConnectionString = 16,
		/// <summary>
		/// User can read all chat settings except <see cref="Models.Internal.ChatSettings.ConnectionString"/>
		/// </summary>
		Read = 32,
		/// <summary>
		/// User can change <see cref="Models.Internal.ChatSettings.Name"/>
		/// </summary>
		WriteName = 32
	}
}
