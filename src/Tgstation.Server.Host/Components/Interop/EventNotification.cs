using System.Collections.Generic;

namespace Tgstation.Server.Host.Components.Interop
{
	/// <summary>
	/// For notifying DD of <see cref="EventType"/>s
	/// </summary>
	sealed class EventNotification
	{
		/// <summary>
		/// The <see cref="EventType"/>
		/// </summary>
		public EventType Type { get; set; }

		/// <summary>
		/// The event parameters
		/// </summary>
		public IEnumerable<string> Parameters { get; set; }
	}
}
