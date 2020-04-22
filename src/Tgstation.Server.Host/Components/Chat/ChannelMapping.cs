namespace Tgstation.Server.Host.Components.Chat
{
	/// <summary>
	/// Represents a mapping of a <see cref="ChatChannel.RealId"/>
	/// </summary>
	sealed class ChannelMapping
	{
		/// <summary>
		/// The Id of the <see cref="Providers.IProvider"/>
		/// </summary>
		public long ProviderId { get; set; }

		/// <summary>
		/// The original <see cref="Components.Chat.ChatChannel.RealId"/>
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
		/// The <see cref="Components.Chat.ChatChannel"/> with the mapped Id
		/// </summary>
		public ChatChannel Channel { get; set; }
	}
}
