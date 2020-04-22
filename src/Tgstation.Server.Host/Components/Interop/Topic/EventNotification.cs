using System.Collections.Generic;
using System.Linq;

namespace Tgstation.Server.Host.Components.Interop.Topic
{
	sealed class EventNotification
	{
		public EventType Type { get; }

		public IReadOnlyCollection<object> Parameters { get; }

		public EventNotification(EventType eventType, IEnumerable<object> parameters = null)
		{
			Type = eventType;
			Parameters = parameters?.ToList();
		}
	}
}