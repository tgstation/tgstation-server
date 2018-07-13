namespace Tgstation.Server.Host.Components.Chat
{
    sealed class Channel
	{
		public long Id { get; set; }

		public string FriendlyName { get; set; }

		public string ConnectionName { get; set; }

		public bool IsAdminChannel { get; set; }
		public bool IsPrivateChannel { get; set; }
	}
}
