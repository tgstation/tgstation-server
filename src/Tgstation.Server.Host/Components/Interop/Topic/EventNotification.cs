using System;
using System.Collections.Generic;
using System.Linq;

using Tgstation.Server.Host.Components.Events;

namespace Tgstation.Server.Host.Components.Interop.Topic
{
	/// <summary>
	/// Data structure for <see cref="TopicCommandType.EventNotification"/> requests.
	/// </summary>
	sealed class EventNotification
	{
		/// <summary>
		/// The <see cref="EventType"/> triggered.
		/// </summary>
		/// <remarks>Nullable to prevent ignoring when serializing.</remarks>
		public EventType? Type { get; }

		/// <summary>
		/// The set of parameters.
		/// </summary>
		public IReadOnlyCollection<string?> Parameters { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="EventNotification"/> class.
		/// </summary>
		/// <param name="eventType">The value of <see cref="Type"/>.</param>
		/// <param name="parameters">The <see cref="IEnumerable{T}"/> that forms the value of <see cref="Parameters"/>.</param>
		public EventNotification(EventType eventType, IEnumerable<string?> parameters)
		{
			Type = eventType;
			Parameters = parameters?.ToList() ?? throw new ArgumentNullException(nameof(parameters));
		}
	}
}
