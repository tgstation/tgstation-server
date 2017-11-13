using System.Collections.Generic;

namespace TGS.Server.Service.ChatCommands
{
	/// <summary>
	/// kek
	/// </summary>
	sealed class KekCommand : ChatCommand
	{
		/// <summary>
		/// Construct a <see cref="KekCommand"/>
		/// </summary>
		public KekCommand()
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
