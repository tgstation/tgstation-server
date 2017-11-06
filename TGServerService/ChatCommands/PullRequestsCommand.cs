using System.Collections.Generic;

namespace TGServerService.ChatCommands
{
	/// <summary>
	/// Retrieve the list of test-merged github pull requests
	/// </summary>
	sealed class PullRequestsCommand : ChatCommand
	{
		/// <summary>
		/// Construct a <see cref="PullRequestsCommand"/>
		/// </summary>
		public PullRequestsCommand()
		{
			Keyword = "prs";
		}

		/// <inheritdoc />
		protected override ExitCode Run(IList<string> parameters)
		{
			var PRs = Instance.MergedPullRequests(out string res);
			if (PRs == null)
			{
				OutputProc(res);
				return ExitCode.ServerError;
			}
			if (PRs.Count == 0)
				OutputProc("None!");
			else
			{
				res = "";
				foreach (var I in PRs)
					res += "#" + I.Number + " ";
				OutputProc(res);
			}
			return ExitCode.Normal;
		}

		/// <inheritdoc />
		public override string GetHelpText()
		{
			return "Gets the currently merged pull requests in the repository";
		}
	}
}
