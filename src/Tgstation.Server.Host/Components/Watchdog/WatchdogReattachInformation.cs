using System;
using Tgstation.Server.Host.Models;

namespace Tgstation.Server.Host.Components.Watchdog
{
	/// <summary>
	/// Reattach information for a <see cref="IWatchdog"/>
	/// </summary>
	public sealed class WatchdogReattachInformation : WatchdogReattachInformationBase
	{
		/// <summary>
		/// <see cref="ReattachInformation"/> for the Alpha session
		/// </summary>
		public ReattachInformation Alpha { get; set; }

		/// <summary>
		/// <see cref="ReattachInformation"/> for the Bravo session
		/// </summary>
		public ReattachInformation Bravo { get; set; }

		/// <summary>
		/// Construct a <see cref="WatchdogReattachInformation"/>
		/// </summary>
		public WatchdogReattachInformation() { }

		/// <summary>
		/// Construct a <see cref="WatchdogReattachInformation"/> from a given <paramref name="copy"/> with a given <paramref name="dmbFactory"/>
		/// </summary>
		/// <param name="copy">The <see cref="WatchdogReattachInformationBase"/> to copy information from</param>
		/// <param name="dmbFactory">The <see cref="IDmbFactory"/> used to build the <see cref="ReattachInformation.Dmb"/>s</param>
		public WatchdogReattachInformation(Models.WatchdogReattachInformation copy, IDmbFactory dmbFactory): base(copy)
		{
			if (copy.Alpha != null)
				Alpha = new ReattachInformation(copy.Alpha, dmbFactory);
			if (copy.Bravo != null)
				Bravo = new ReattachInformation(copy.Bravo, dmbFactory);
		}
	}
}
