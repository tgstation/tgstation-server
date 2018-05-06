namespace Tgstation.Server.Host.Components
{
	/// <summary>
	/// Indicates a server control event from a DreamDaemon instance
	/// </summary>
	enum ServerControlEventType
	{
		/// <summary>
		/// The server is loaded
		/// </summary>
		ServerPrimed,
		/// <summary>
		/// The server called world/Reboot()
		/// </summary>
		ServerRebooting,
		/// <summary>
		/// The server requested that it's process be terminated
		/// </summary>
		RequestedProcessTermination,
		/// <summary>
		/// The server has stopped responding to heartbeats
		/// </summary>
		ServerUnresponsive,
	}
}