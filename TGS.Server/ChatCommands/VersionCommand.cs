using System.Collections.Generic;

namespace TGS.Server.ChatCommands
{
	/// <summary>
	/// Retrieve the current service version
	/// </summary>
	sealed class VersionCommand : ChatCommand
	{
		/// <summary>
		/// Construct a <see cref="VersionCommand"/>
		/// </summary>
		public VersionCommand()
		{
			Keyword = "version";
		}

		/// <inheritdoc />
		protected override ExitCode Run(IList<string> parameters)
		{
			OutputProc(Instance.Version());
			return ExitCode.Normal;
		}

		/// <inheritdoc />
		public override string GetHelpText()
		{
			return "Gets the running service version";
		}
	}
}
