namespace Tgstation.Server.Host.Components.Watchdog
{
	/// <summary>
	/// The action for the monitor loop to take when control is returned to it
	/// </summary>
	enum MonitorAction
	{
		Continue,
		Restart,
		Break,
		Exit
	}
}