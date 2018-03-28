using System.Collections.Generic;

namespace TGS.Server.Chat.Commands
{
	/// <summary>
	/// kek
	/// </summary>
	sealed class KekChatCommand : ChatCommand
	{
		/// <summary>
		/// Construct a <see cref="KekChatCommand"/>
		/// </summary>
		public KekChatCommand()
		{
			Keyword = "kek";
		}

		/// <inheritdoc />
		protected override ExitCode Run(IList<string> parameters)
		{
			OutputProc("kek");
			return ExitCode.Normal;
		}

		/// <inheritdoc />
		public override string GetHelpText()
		{
			return "kek";
		}
	}
}
