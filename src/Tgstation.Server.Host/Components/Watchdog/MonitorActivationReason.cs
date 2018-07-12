namespace Tgstation.Server.Host.Components.Watchdog
{
	enum MonitorActivationReason
	{
		ActiveServerCrashed,
		InactiveServerCrashed,
		ActiveServerRebooted,
		InactiveServerRebooted,
		NewDmbAvailable,
		InactiveServerStartupComplete
	}
}
