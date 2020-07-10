namespace Tgstation.Server.Api.Models
{
	/// <summary>
	/// The current status of the watchdog.
	/// </summary>
	public enum WatchdogStatus
	{
		/// <summary>
		/// The watchdog is not running.
		/// </summary>
		Offline,

		/// <summary>
		/// The watchdog is online and attempting to bring DreamDaemon back to operational status.
		/// </summary>
		Restoring,

		/// <summary>
		/// The watchdog is online and DreamDaemon is running.
		/// </summary>
		Online,

		/// <summary>
		/// The watchdog is online and in a delayed sleep to bring DreamDaemon back.
		/// </summary>
		DelayedRestart,
	}
}