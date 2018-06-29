namespace Tgstation.Server.Host.Components
{
	/// <summary>
	/// Represents the action to take when /world/Reboot() is called
	/// </summary>
    enum DreamDaemonRebootState
    {
		/// <summary>
		/// Run DreamDaemon's normal reboot process
		/// </summary>
		Normal,
		/// <summary>
		/// Shutdown DreamDaemon
		/// </summary>
		Shutdown,
		/// <summary>
		/// Restart the DreamDaemon process
		/// </summary>
		Restart
    }
}
