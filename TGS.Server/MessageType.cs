using System;

namespace TGS.Server
{
	/// <summary>
	/// Type of chat message, these may be OR'd together
	/// </summary>
	[Flags]
	enum MessageType
	{
		/// <summary>
		/// Send message to the admin channels
		/// </summary>
		AdminInfo = 1,
		/// <summary>
		/// Send message to the game channels
		/// </summary>
		GameInfo = 2,
		/// <summary>
		/// Send message to the watchdog channels
		/// </summary>
		WatchdogInfo = 4,
		/// <summary>
		/// Send message to the coder channels
		/// </summary>
		DeveloperInfo = 8,
	}
}
