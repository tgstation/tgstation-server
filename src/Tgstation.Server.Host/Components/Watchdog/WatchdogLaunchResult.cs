using System;

namespace Tgstation.Server.Host.Components.Watchdog
{
	/// <summary>
	/// Launch results for a <see cref="IWatchdog"/>
	/// </summary>
	sealed class WatchdogLaunchResult
	{
		/// <summary>
		/// If the watchdog is running
		/// </summary>
		bool Running {
			get {
				if (Alpha == null || Bravo == null)
					throw new InvalidOperationException("Alpha or Bravo is null!");
				return !Alpha.ExitCode.HasValue && !Bravo.ExitCode.HasValue;
			}
		}

		/// <summary>
		/// The <see cref="LaunchResult"/> for the alpha process
		/// </summary>
		LaunchResult Alpha { get; set; }

		/// <summary>
		/// The <see cref="LaunchResult"/> for the bravo process
		/// </summary>
		LaunchResult Bravo { get; set; }
	}
}
