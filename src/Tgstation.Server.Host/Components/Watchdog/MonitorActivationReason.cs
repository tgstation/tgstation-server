namespace Tgstation.Server.Host.Components.Watchdog
{
	/// <summary>
	/// Reasons for the monitor to wake up
	/// </summary>
	enum MonitorActivationReason
	{
		/// <summary>
		/// The active server crashed or exited
		/// </summary>
		ActiveServerCrashed,

		/// <summary>
		/// The inactive server crashed or exited
		/// </summary>
		InactiveServerCrashed,

		/// <summary>
		/// The active server called /world/Reboot()
		/// </summary>
		ActiveServerRebooted,

		/// <summary>
		/// The inactive server called /world/Reboot()
		/// </summary>
		InactiveServerRebooted,

		/// <summary>
		/// The inactive server is past that point where DD hangs when you press "Go"
		/// </summary>
		InactiveServerStartupComplete,

		/// <summary>
		/// A new .dmb was deployed
		/// </summary>
		NewDmbAvailable,

		/// <summary>
		/// Server launch parameters were changed
		/// </summary>
		ActiveLaunchParametersUpdated,

		/// <summary>
		/// A heartbeat is required.
		/// </summary>
		Heartbeat,
	}
}
