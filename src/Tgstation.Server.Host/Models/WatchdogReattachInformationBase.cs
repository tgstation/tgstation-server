using System;

namespace Tgstation.Server.Host.Models
{
	public class WatchdogReattachInformationBase
	{
		/// <summary>
		/// If the Alpha session is the active session
		/// </summary>
		public bool AlphaIsActive { get; set; }

		public WatchdogReattachInformationBase() { }
		protected WatchdogReattachInformationBase(WatchdogReattachInformationBase copy)
		{
			AlphaIsActive = copy?.AlphaIsActive ?? throw new ArgumentNullException(nameof(copy));
		}
	}
}
