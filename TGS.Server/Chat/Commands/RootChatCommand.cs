using System.Collections.Generic;
using TGS.Interface;

namespace TGS.Server.Chat.Commands
{
	/// <summary>
	/// The main root chat command
	/// </summary>
	sealed class RootChatCommand : RootCommand
	{
		/// <summary>
		/// Construct a <see cref="RootChatCommand"/>
		/// </summary>
		/// <param name="serverCommands">List of <see cref="GameInteropChatCommand"/>s supplied by DreamDaemon</param>
		public RootChatCommand(List<Command> serverCommands)
		{
			var tmp = new List<Command> { new PullRequestsChatCommand(), new VersionChatCommand(), new RevisionChatCommand(), new ByondChatCommand(), new KekChatCommand() };
			if (serverCommands != null)
				tmp.AddRange(serverCommands);
			Children = tmp.ToArray();
			serverCommands = new List<Command>();
			PrintHelpList = true;
		}
	}
}
