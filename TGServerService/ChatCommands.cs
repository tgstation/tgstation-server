using TGServiceInterface;
using System.Threading;

namespace TGServerService
{
	class CommandInfo
	{
		public bool IsAdmin { get; set; }
		public bool IsAdminChannel { get; set; }

		public string Speaker { get; set; }
		public string Channel { get; set; }
	}
	abstract class ChatCommand : Command
	{
		public bool RequiresAdmin { get; protected set; }
		public static ThreadLocal<CommandInfo> CommandInfo = new ThreadLocal<CommandInfo>();
	}
	class RootChatCommand : RootCommand
	{

	}
}
