namespace Tgstation.Server.Host.Components.Watchdog
{
	/// <summary>
	/// Reattach information for a <see cref="IWatchdog"/>
	/// </summary>
	sealed class WatchdogReattachInformation
	{
		/// <summary>
		/// If the Alpha session is the active session
		/// </summary>
		public bool AlphaIsActive { get; set; }

		/// <summary>
		/// <see cref="ReattachInformation"/> for the Alpha session
		/// </summary>
		public ReattachInformation Alpha { get; set; }

		/// <summary>
		/// <see cref="ReattachInformation"/> for the Bravo session
		/// </summary>
		public ReattachInformation Bravo { get; set; }
	}
}
