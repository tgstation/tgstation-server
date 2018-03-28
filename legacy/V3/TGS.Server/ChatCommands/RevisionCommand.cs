using System;
using System.Collections.Generic;

namespace TGS.Server.ChatCommands
{
	/// <summary>
	/// Retrieves the git SHA of the live DreamDaemon code
	/// </summary>
	sealed class RevisionCommand : ChatCommand
	{
		/// <summary>
		/// Construct a <see cref="RevisionCommand"/>
		/// </summary>
		public RevisionCommand()
		{
			Keyword = "revision";
		}
		/// <inheritdoc />
		protected override ExitCode Run(IList<string> parameters)
		{
			var res = Instance.LiveSha();
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
