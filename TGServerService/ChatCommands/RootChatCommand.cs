using System.Collections.Generic;
using TGServiceInterface;

namespace TGServerService.ChatCommands
{
	/// <summary>
	/// The main root chat command
	/// </summary>
	sealed class RootChatCommand : RootCommand
	{
		/// <summary>
		/// Construct a <see cref="RootChatCommand"/>
		/// </summary>
		/// <param name="serverCommands">List of <see cref="ServerChatCommand"/>s supplied by DreamDaemon</param>
		public RootChatCommand(List<Command> serverCommands)
		{
			var tmp = new List<Command> { new PullRequestsCommand(), new VersionCommand(), new RevisionCommand(), new ByondCommand(), new KekCommand() };
			if (serverCommands != null)
				tmp.AddRange(serverCommands);
			Children = tmp.ToArray();
			serverCommands = new List<Command>();
			PrintHelpList = true;
		}
	}
}
