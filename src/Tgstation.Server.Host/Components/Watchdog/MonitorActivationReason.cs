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
		/// The active server called /world/Reboot()
		/// </summary>
		ActiveServerRebooted,

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

		/// <summary>
		/// Server primed.
		/// </summary>
		ActiveServerPrimed,
	}
}
