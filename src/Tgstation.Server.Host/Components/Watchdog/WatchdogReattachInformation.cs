using System;
using System.Globalization;
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
		/// Construct a <see cref="WatchdogReattachInformation"/> from a given <paramref name="copy"/> with a given <paramref name="dmbAlpha"/> and <paramref name="dmbBravo"/>
		/// </summary>
		/// <param name="copy">The <see cref="WatchdogReattachInformationBase"/> to copy information from</param>
		/// <param name="dmbAlpha">The <see cref="IDmbProvider"/> used to build <see cref="Alpha"/></param>
		/// <param name="dmbBravo">The <see cref="IDmbProvider"/> used to build <see cref="Bravo"/></param>
		public WatchdogReattachInformation(Models.WatchdogReattachInformation copy, IDmbProvider dmbAlpha, IDmbProvider dmbBravo) : base(copy)
		{
			if (copy.Alpha != null)
				Alpha = new ReattachInformation(copy.Alpha, dmbAlpha);
			if (copy.Bravo != null)
				Bravo = new ReattachInformation(copy.Bravo, dmbBravo);
		}

		/// <inheritdoc />
		public override string ToString() => String.Format(CultureInfo.InvariantCulture, "Alpha: {0}, Bravo {1}", Alpha, Bravo);
	}
}
