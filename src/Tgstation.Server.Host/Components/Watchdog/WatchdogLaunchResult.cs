using System;

namespace Tgstation.Server.Host.Components.Watchdog
{
	/// <summary>
	/// Launch results for a <see cref="IWatchdog"/>
	/// </summary>
	public sealed class WatchdogLaunchResult
	{
		/// <summary>
		/// The <see cref="LaunchResult"/> for the alpha process
		/// </summary>
		public LaunchResult Alpha { get; set; }

		/// <summary>
		/// The <see cref="LaunchResult"/> for the bravo process
		/// </summary>
		public LaunchResult Bravo { get; set; }
	}
}
