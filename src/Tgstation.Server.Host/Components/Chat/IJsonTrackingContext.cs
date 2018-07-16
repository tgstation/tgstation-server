using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Host.Components.Chat.Commands;

namespace Tgstation.Server.Host.Components.Chat
{
	/// <summary>
	/// Represents a tracking of dynamic chat json files
	/// </summary>
	public interface IJsonTrackingContext : IDisposable
	{
		/// <summary>
		/// Read <see cref="CustomCommand"/>s from the <see cref="IJsonTrackingContext"/>
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in a <see cref="IReadOnlyList{T}"/> of <see cref="CustomCommand"/>s in the <see cref="IJsonTrackingContext"/></returns>
		Task<IReadOnlyList<CustomCommand>> GetCustomCommands(CancellationToken cancellationToken);

		/// <summary>
		/// Writes information about connected <paramref name="channels"/> to the <see cref="IJsonTrackingContext"/>
		/// </summary>
		/// <param name="channels">The <see cref="Channel"/>s to write out</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		Task SetChannels(IEnumerable<Channel> channels, CancellationToken cancellationToken);
	}
}