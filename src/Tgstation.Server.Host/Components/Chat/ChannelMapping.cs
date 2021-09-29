using System;

namespace Tgstation.Server.Host.Components.Chat
{
	/// <summary>
	/// Represents a mapping of a <see cref="ChannelRepresentation.RealId"/>.
	/// </summary>
	sealed class ChannelMapping
	{
		/// <summary>
		/// The Id of the <see cref="Providers.IProvider"/>.
		/// </summary>
		public long ProviderId { get; init; }

		/// <summary>
		/// The original <see cref="ChannelRepresentation.RealId"/>.
		/// </summary>
		public ulong ProviderChannelId { get; init; }

		/// <summary>
		/// If <see cref="Channel"/> is a watchdog channel.
		/// </summary>
		public bool IsWatchdogChannel { get; init; }

		/// <summary>
		/// If the <see cref="Channel"/> is an updates channel.
		/// </summary>
		public bool IsUpdatesChannel { get; init; }

		/// <summary>
		/// If the <see cref="Channel"/> is an admin channel.
		/// </summary>
		public bool IsAdminChannel { get; init; }

		/// <summary>
		/// The <see cref="ChannelRepresentation"/> with the mapped Id.
		/// </summary>
		public ChannelRepresentation Channel { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="ChannelMapping"/> class.
		/// </summary>
		/// <param name="channel">The value of <see cref="Channel"/>.</param>
		public ChannelMapping(ChannelRepresentation channel)
		{
			Channel = channel ?? throw new ArgumentNullException(nameof(channel));
		}
	}
}
