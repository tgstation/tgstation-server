namespace Tgstation.Server.Host.Components.Watchdog
{
	/// <summary>
	/// Represents the action to take when /world/Reboot() is called
	/// </summary>
    enum RebootState : int
    {
		/// <summary>
		/// Run DreamDaemon's normal reboot process
		/// </summary>
		Normal = 0,
		/// <summary>
		/// Shutdown DreamDaemon
		/// </summary>
		Shutdown = 1,
		/// <summary>
		/// Restart the DreamDaemon process
		/// </summary>
		Restart = 2
    }
}
