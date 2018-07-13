namespace Tgstation.Server.Host.Components.Chat
{
	public sealed class User
	{
		long Id { get; set; }
		string FriendlyName { get; set; }
		string Mention { get; set; }
		Channel channel { get; set; }
	}
}
