using System;
using System.Collections.Generic;

using Tgstation.Server.Host.Components.Chat.Commands;

#nullable disable

namespace Tgstation.Server.Host.Components.Chat
{
	/// <summary>
	/// Represents a tracking of dynamic chat json files.
	/// </summary>
	public interface IChatTrackingContext : IChannelSink, IDisposable
	{
		/// <summary>
		/// If the <see cref="CustomCommands"/> should be used.
		/// </summary>
		/// <remarks>This should only be set by the <see cref="object"/> that sets the <see cref="CustomCommands"/>.</remarks>
		bool Active { get; set; }

		/// <summary>
		/// <see cref="IReadOnlyCollection{T}"/> of <see cref="ChannelRepresentation"/>s in the <see cref="IChatTrackingContext"/>.
		/// </summary>
		IReadOnlyCollection<ChannelRepresentation> Channels { get; }

		/// <summary>
		/// <see cref="IReadOnlyCollection{T}"/> of <see cref="CustomCommand"/>s in the <see cref="IChatTrackingContext"/>.
		/// </summary>
		IEnumerable<CustomCommand> CustomCommands { get; set; }

		/// <summary>
		/// Sets the <paramref name="channelSink"/> for the <see cref="IChatTrackingContext"/>.
		/// </summary>
		/// <param name="channelSink">The <see cref="IChannelSink"/>s to send updates to.</param>
		void SetChannelSink(IChannelSink channelSink);
	}
}
