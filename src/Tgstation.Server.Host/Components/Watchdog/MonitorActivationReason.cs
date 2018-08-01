namespace Tgstation.Server.Host.Components.Watchdog
{
	enum MonitorActivationReason
	{
		ActiveServerCrashed,
		InactiveServerCrashed,
		ActiveServerRebooted,
		InactiveServerRebooted,
		InactiveServerStartupComplete,
		NewDmbAvailable,
		ActiveLaunchParametersUpdated
	}
}
