namespace Tgstation.Server.Host.Components.Watchdog
{
	/// <summary>
	/// The action for the monitor loop to take when control is returned to it.
	/// </summary>
	enum MonitorAction
	{
		/// <summary>
		/// The monitor should continue as normal
		/// </summary>
		Continue,

		/// <summary>
		/// Skips the next call to HandleMonitorWakeup action
		/// </summary>
		Skip,

		/// <summary>
		/// The monitor should kill and restart both servers
		/// </summary>
		Restart,

		/// <summary>
		/// The monitor should stop checking actions for this iteration and continue its loop
		/// </summary>
		Break,

		/// <summary>
		/// The monitor should end all sessions and exit.
		/// </summary>
		Exit,
	}
}
