using System;
using System.Collections.Generic;

namespace TGS.Server.Chat.Commands
{
	/// <summary>
	/// Retrieves the git SHA of the live DreamDaemon code
	/// </summary>
	sealed class RevisionChatCommand : ChatCommand
	{
		/// <summary>
		/// Construct a <see cref="RevisionChatCommand"/>
		/// </summary>
		public RevisionChatCommand()
		{
			Keyword = "revision";
		}
		/// <inheritdoc />
		protected override ExitCode Run(IList<string> parameters)
		{
			var res = CommandInfo.Repo.LiveSha();
			if (res == "UNKNOWN")
			{
				OutputProc(res);
				return ExitCode.ServerError;
			}
			OutputProc(String.Format("^{0}", res));
			return ExitCode.Normal;
		}

		/// <inheritdoc />
		public override string GetHelpText()
		{
			return "Prints the current code revision of the repository (not the server)";
		}
	}
}
