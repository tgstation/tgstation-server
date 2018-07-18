namespace Tgstation.Server.Host.Components.Chat.Providers
{
	sealed class Message
	{
		public string Content { get; set; }
		public User User { get; set; }
	}
}