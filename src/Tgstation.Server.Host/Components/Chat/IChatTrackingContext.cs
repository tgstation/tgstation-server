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
	interface IChatTrackingContext : IDisposable
	{
		/// <summary>
		/// <see cref="IReadOnlyCollection{T}"/> of <see cref="ChannelRepresentation"/>s in the <see cref="IChatTrackingContext"/>.
		/// </summary>
		IReadOnlyCollection<ChannelRepresentation> Channels { get; }

		/// <summary>
		/// Sets the <paramref name="customCommands"/> for the <see cref="IChatTrackingContext"/>.
		/// </summary>
		/// <param name="customCommands">An <see cref="IEnumerable{T}"/> of <see cref="CustomCommand"/>s.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task"/> representing the running operation.</returns>
		Task SetCustomCommands(IEnumerable<CustomCommand> customCommands, CancellationToken cancellationToken);

		/// <summary>
		/// Sets the <paramref name="channelSink"/> for the <see cref="IChatTrackingContext"/>.
		/// </summary>
		/// <param name="channelSink">The <see cref="IChannelSink"/>s to send updates to.</param>
		void SetChannelSink(IChannelSink channelSink);
	}
}