using System;
using System.Collections.Generic;
using System.Text;

namespace Tgstation.Server.Host.Models
{
	/// <summary>
	/// Database representation of <see cref="Components.Watchdog.WatchdogReattachInformation"/>
	/// </summary>
	public sealed class WatchdogReattachInformation : WatchdogReattachInformationBase
	{
		/// <summary>
		/// The row Id
		/// </summary>
		public long Id { get; set; }

		/// <summary>
		/// The <see cref="ReattachInformation"/> for the Alpha server
		/// </summary>
		public ReattachInformation Alpha { get; set; }

		/// <summary>
		/// The <see cref="ReattachInformation"/> for the Bravo server
		/// </summary>
		public ReattachInformation Bravo { get; set; }
	}
}
