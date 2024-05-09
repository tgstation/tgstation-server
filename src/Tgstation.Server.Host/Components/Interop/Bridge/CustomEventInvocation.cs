using System.Collections.Generic;

namespace Tgstation.Server.Host.Components.Interop.Bridge
{
	/// <summary>
	/// Parameters for invoking a custom event.
	/// </summary>
	public sealed class CustomEventInvocation
	{
		/// <summary>
		/// The name of the event being invoked.
		/// </summary>
		public string? EventName { get; set; }

		/// <summary>
		/// The parameters for the invoked event.
		/// </summary>
		public ICollection<string?>? Parameters { get; set; }

		/// <summary>
		/// If the DMAPI should be notified when the event compeletes.
		/// </summary>
		public bool? NotifyCompletion { get; set; }
	}
}
