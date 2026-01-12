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
		/// <param name="sensitiveParameters">If parameters are considered sensitive and should not be logged.</param>
		/// <param name="deploymentPipeline">If this event is part of the deployment pipeline.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask"/> representing the running operation.</returns>
		ValueTask HandleEvent(EventType eventType, IEnumerable<string?> parameters, bool sensitiveParameters, bool deploymentPipeline, CancellationToken cancellationToken);

		/// <summary>
		/// Handles a given custom event.
		/// </summary>
		/// <param name="eventName">The name of the event.</param>
		/// <param name="parameters">An <see cref="IEnumerable{T}"/> of <see cref="string"/> parameters for the event.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask"/> representing the running operation if the event was triggered successfully, <see langword="null"/> if it matched a TGS event and wasn't executed.</returns>
		ValueTask? HandleCustomEvent(string eventName, IEnumerable<string?> parameters, CancellationToken cancellationToken);
	}
}
