using System.Collections.Generic;
using TGServiceInterface;

namespace TGServerService.ChatCommands
{
	/// <summary>
	/// Retrieve the installed, staged, or latest availab
	/// </summary>
	sealed class ByondCommand : ChatCommand
	{
		/// <summary>
		/// Construct a <see cref="ByondCommand"/>
		/// </summary>
		public ByondCommand()
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
			OutputProc(Instance.GetVersion(type) ?? "None");
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
