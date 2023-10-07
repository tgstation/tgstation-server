using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Tgstation.Server.Host.Components.Chat
{
	/// <summary>
	/// Notifyee of when <see cref="ChannelRepresentation"/>s in a <see cref="IChatTrackingContext"/> are updated.
	/// </summary>
	public interface IChannelSink
	{
		/// <summary>
		/// Called when <paramref name="newChannels"/> are set.
		/// </summary>
		/// <param name="newChannels">The <see cref="IEnumerable{T}"/> of new <see cref="ChannelRepresentation"/>s.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask"/> representing the running operation.</returns>
		ValueTask UpdateChannels(IEnumerable<ChannelRepresentation> newChannels, CancellationToken cancellationToken);
	}
}
