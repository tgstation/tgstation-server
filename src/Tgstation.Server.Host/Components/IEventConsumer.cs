using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Tgstation.Server.Host.Components
{
	/// <summary>
	/// Consumes <see cref="EventType"/>s and takes the appropriate actions
	/// </summary>
	public interface IEventConsumer
	{
		/// <summary>
		/// Handle a given <paramref name="eventType"/>
		/// </summary>
		/// <param name="eventType">The <see cref="EventType"/></param>
		/// <param name="parameters">The parameters for <paramref name="eventType"/></param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in <see langword="true"/> if more <see cref="IEventConsumer"/> should run, <see langword="false"/> otherwise</returns>
		Task<bool> HandleEvent(EventType eventType, IEnumerable<string> parameters, CancellationToken cancellationToken);
	}
}
