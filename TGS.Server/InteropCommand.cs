namespace TGS.Server
{
	/// <summary>
	/// Commands avaiable to send over interop
	/// </summary>
	enum InteropCommand
	{
		/// <summary>
		/// Request DreamDaemon to notify us on world/Reboot to restart the process. No parameters
		/// </summary>
		RestartOnWorldReboot,
		/// <summary>
		/// Request DreamDaemon to notify us on world/Reboot to terminate the process. No parameters
		/// </summary>
		ShutdownOnWorldReboot,
	}
}
