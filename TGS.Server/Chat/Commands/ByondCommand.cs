using System.Collections.Generic;
using TGS.Interface;

namespace TGS.Server.Chat.Commands
{
	/// <summary>
	/// Retrieve the installed, staged, or latest availab
	/// </summary>
	sealed class ByondChatCommand : ChatCommand
	{
		/// <summary>
		/// Construct a <see cref="ByondCommand"/>
		/// </summary>
		public ByondChatCommand()
		{
			Keyword = "byond";
		}

		/// <inheritdoc />
		protected override ExitCode Run(IList<string> parameters)
		{
			var type = ByondVersion.Installed;
			if (parameters.Count > 0)
				if (parameters[0].ToLower() == "--staged")
					type = ByondVersion.Staged;
				else if (parameters[0].ToLower() == "--latest")
					type = ByondVersion.Latest;
			OutputProc(CommandInfo.Byond.GetVersion(type) ?? "None");
			return ExitCode.Normal;
		}

		/// <inheritdoc />
		public override string GetHelpText()
		{
			return "Gets the specified BYOND version";
		}

		/// <inheritdoc />
		public override string GetArgumentString()
		{
			return "[--staged|--latest]";
		}
	}
}
