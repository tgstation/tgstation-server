namespace Tgstation.Server.Host.Components.Chat
{
	/// <summary>
	/// Represents a mapping of a <see cref="ChannelRepresentation.RealId"/>
	/// </summary>
	sealed class ChannelMapping
	{
		/// <summary>
		/// The Id of the <see cref="Providers.IProvider"/>
		/// </summary>
		public long ProviderId { get; set; }

		/// <summary>
		/// The original <see cref="Components.Chat.ChannelRepresentation.RealId"/>
		/// </summary>
		public ulong ProviderChannelId { get; set; }

		/// <summary>
		/// If <see cref="Channel"/> is a watchdog channel
		/// </summary>
		public bool IsWatchdogChannel { get; set; }

		/// <summary>
		/// If the <see cref="Channel"/> is an updates channel
		/// </summary>
		public bool IsUpdatesChannel { get; set; }

		/// <summary>
		/// The <see cref="Components.Chat.ChannelRepresentation"/> with the mapped Id
		/// </summary>
		public ChannelRepresentation Channel { get; set; }
	}
}
