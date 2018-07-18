namespace Tgstation.Server.Host.Components.Chat
{
	sealed class ChannelMapping
	{
		public long ProviderId { get; set; }
		public ulong ProviderChannelId { get; set; }
		public bool IsWatchdogChannel { get; set; }

		public Channel Channel { get; set; }
	}
}
