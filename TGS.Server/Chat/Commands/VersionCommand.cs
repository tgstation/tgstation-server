using System.Collections.Generic;

namespace TGS.Server.Chat.Commands
{
	/// <summary>
	/// Retrieve the current service version
	/// </summary>
	sealed class VersionChatCommand : ChatCommand
	{
		/// <summary>
		/// Construct a <see cref="VersionCommand"/>
		/// </summary>
		public VersionChatCommand()
		{
			Keyword = "version";
		}

		/// <inheritdoc />
		protected override ExitCode Run(IList<string> parameters)
		{
			OutputProc(Server.VersionString);
			return ExitCode.Normal;
		}

		/// <inheritdoc />
		public override string GetHelpText()
		{
			return "Gets the running service version";
		}
	}
}
