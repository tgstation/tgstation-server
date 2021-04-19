using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Tgstation.Server.Host.Components.Events
{
	/// <summary>
	/// Consumes <see cref="EventType"/>s and takes the appropriate actions.
	/// </summary>
	public interface IEventConsumer
	{
		/// <summary>
		/// Handle a given <paramref name="eventType"/>.
		/// </summary>
		/// <param name="eventType">The <see cref="EventType"/>.</param>
		/// <param name="parameters">An <see cref="IEnumerable{T}"/> of <see cref="string"/> parameters for <paramref name="eventType"/>.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task"/> representing the running operation.</returns>
		Task HandleEvent(EventType eventType, IEnumerable<string> parameters, CancellationToken cancellationToken);
	}
}
