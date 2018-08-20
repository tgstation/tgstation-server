using Tgstation.Server.Host.Components.Chat;

namespace Tgstation.Server.Host.Components.Interop
{
	sealed class ChatCommand
	{
		public string Command { get; set; }
		public string Parameters { get; set; }
		public User User { get; set; }
	}
}
