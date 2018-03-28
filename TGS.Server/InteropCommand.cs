namespace TGS.Server
{
	/// <summary>
	/// Commands avaiable to send over <see cref="Components.IInteropManager.SendCommand(InteropCommand, System.Collections.Generic.IEnumerable{string})"/>
	/// </summary>
	enum InteropCommand
	{
		/// <summary>
		/// Tell the world we will accept bridge commands from their DMAPI. No parameters
		/// </summary>
		DMAPIIsCompatible,
		/// <summary>
		/// Request DreamDaemon to notify us on world/Reboot to restart the process. No parameters
		/// </summary>
		RestartOnWorldReboot,
		/// <summary>
		/// Request DreamDaemon to notify us on world/Reboot to terminate the process. No parameters
		/// </summary>
		ShutdownOnWorldReboot,
		/// <summary>
		/// Request the world to show a message to all players. 1 parameter
		/// </summary>
		WorldAnnounce,
		/// <summary>
		/// Request the world to return the json for the server_tools_command datums. No parameters
		/// </summary>
		ListCustomCommands,
		/// <summary>
		/// Request the world to return the number of connected clients. No parameters
		/// </summary>
		PlayerCount,
		/// <summary>
		/// Use a custom command string for the topic. 1 parameter
		/// </summary>
		CustomCommand,
	}
}