namespace Tgstation.Server.Host.Components.Chat
{
	/// <summary>
	/// 
	/// </summary>
	public sealed class User
	{
		long Id { get; set; }
		string FriendlyName { get; set; }
		string Mention { get; set; }
		Channel Channel { get; set; }
	}
}
