using System;

namespace Tgstation.Server.Host.Models
{
	/// <summary>
	/// Base class for <see cref="WatchdogReattachInformation"/>
	/// </summary>
	public abstract class WatchdogReattachInformationBase
	{
		/// <summary>
		/// If the Alpha session is the active session
		/// </summary>
		public bool AlphaIsActive { get; set; }

		/// <summary>
		/// Construct a <see cref="WatchdogReattachInformationBase"/>
		/// </summary>
		public WatchdogReattachInformationBase() { }

		/// <summary>
		/// Construct a <see cref="WatchdogReattachInformation"/> from a given <paramref name="copy"/>
		/// </summary>
		/// <param name="copy">The <see cref="WatchdogReattachInformationBase"/> to copy values from</param>
		protected WatchdogReattachInformationBase(WatchdogReattachInformationBase copy)
		{
			AlphaIsActive = copy?.AlphaIsActive ?? throw new ArgumentNullException(nameof(copy));
		}
	}
}
