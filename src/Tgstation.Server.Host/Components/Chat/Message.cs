namespace Tgstation.Server.Host.Components.Chat.Providers
{
	sealed class Message
	{
		string Content { get; set; }
		User User { get; set; }
	}
}