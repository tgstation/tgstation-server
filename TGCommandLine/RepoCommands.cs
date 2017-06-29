using System;
using System.Collections.Generic;
using TGServiceInterface;

namespace TGCommandLine
{
	class RepoCommand : RootCommand
	{
		public RepoCommand()
		{
			Keyword = "repo";
			Children = new Command[] { new RepoSetupCommand(), new RepoUpdateCommand(), new RepoChangelogCommand(), new RepoPythonPathCommand(), new RepoSetEmailCommand(), new RepoSetNameCommand(), new RepoMergePRCommand(), new RepoListPRsCommand(), new RepoStatusCommand(), new RepoListBackupsCommand(), new RepoCheckoutCommand(), new RepoResetCommand() };
		}
		public override string GetHelpText()
		{
			return "Manage the git repository";
		}
	}

	class RepoSetupCommand : Command
	{
		public RepoSetupCommand()
		{
			Keyword = "setup";
			RequiredParameters = 1;
		}
		public override ExitCode Run(IList<string> parameters)
		{
			var res = Server.GetComponent<ITGRepository>().Setup(parameters[0], parameters.Count > 1 ? parameters[1] : "master");
			if (res != null)
			{
				OutputProc("Error: " + res);
				return ExitCode.ServerError;
			}
			OutputProc("Setting up repo. This will take a while...");
			return ExitCode.Normal;
		}
		public override string GetArgumentString()
		{
			return "<git-url> [branchname]";
		}
		public override string GetHelpText()
		{
			return "Clean up everything and clones the repo at git-url with optional branch name";
		}
	}

	class RepoStatusCommand : Command
	{
		public RepoStatusCommand()
		{
			Keyword = "status";
		}
		public override ExitCode Run(IList<string> parameters)
		{
			var Repo = Server.GetComponent<ITGRepository>();
			var busy = Repo.OperationInProgress();
			if (!busy)
			{
				OutputProc("Repo: Idle");
				var head = Repo.GetHead(out string error);
				if (head == null)
					head = "Error: " + error;
				var branch = Repo.GetBranch(out error);
				if (branch == null)
					branch = "Error: " + error;
				var remote = Repo.GetRemote(out error);
				if (remote == null)
					remote = "Error: " + error;
				OutputProc("Remote: " + remote);
				OutputProc("Branch: " + branch);
				OutputProc("HEAD: " + head);
			}
			else
			{
				OutputProc("Repo: Busy");
				var progress = Repo.CheckoutProgress();
				if (progress != -1)
				{
					var eqs = "";
					for (var I = 0; I < progress / 10; ++I)
						eqs += "=";
					var dshs = "";
					for (var I = 0; I < 10 - (progress / 10); ++I)
						eqs += "-";
					OutputProc(String.Format("Progress: [{0}{1}] {2}%", eqs, dshs, progress));
				}
			}
			return ExitCode.Normal;
		}
		public override string GetHelpText()
		{
			return "Shows the busy status of the repo, remote, branch, and HEAD information";
		}
	}

	class RepoResetCommand : Command
	{
		public RepoResetCommand()
		{
			Keyword = "reset";
		}
		public override ExitCode Run(IList<string> parameters)
		{
			var result = Server.GetComponent<ITGRepository>().Reset(parameters.Count > 0 && parameters[0].ToLower() == "--origin");
			OutputProc(result ?? "Success!");
			return result == null ? ExitCode.Normal : ExitCode.ServerError;
		}
		public override string GetArgumentString()
		{
			return "[--origin]";
		}
		public override string GetHelpText()
		{
			return "Hard resets the repo. If a target is specified, the current branch is reset to that branch";
		}
	}

	class RepoUpdateCommand : Command
	{
		public RepoUpdateCommand()
		{
			Keyword = "update";
			RequiredParameters = 1;
		}
		public override ExitCode Run(IList<string> parameters)
		{
			bool hard;
			switch (parameters[0].ToLower())
			{
				case "hard":
					hard = true;
					break;
				case "merge":
					hard = false;
					break;
				default:
					OutputProc("Invalid parameter: " + parameters[0]);
					return ExitCode.BadCommand;
			}
			var res = Server.GetComponent<ITGRepository>().Update(hard);
			OutputProc(res ?? "Success");
			return res == null ? ExitCode.Normal : ExitCode.ServerError;
		}
		public override string GetHelpText()
		{
			return "Updates the current branch the repo is on either via a merge or hard reset";
		}
		public override string GetArgumentString()
		{
			return "<hard|merge>";
		}
	}
	class RepoChangelogCommand : Command
	{
		public RepoChangelogCommand()
		{
			Keyword = "gen-changelog";
		}
		public override ExitCode Run(IList<string> parameters)
		{
			var result = Server.GetComponent<ITGRepository>().GenerateChangelog(out string error);
			OutputProc(error ?? "Success!");
			if (result != null)
				OutputProc(result);
			return error == null ? ExitCode.Normal : ExitCode.ServerError;
		}

		public override string GetHelpText()
		{
			return "Compiles the html changelog";
		}
	}
	class RepoSetEmailCommand : Command
	{
		public RepoSetEmailCommand()
		{
			Keyword = "set-email";
			RequiredParameters = 1;
		}
		public override ExitCode Run(IList<string> parameters)
		{
			Server.GetComponent<ITGRepository>().SetCommitterEmail(parameters[0]);
			return ExitCode.Normal;
		}

		public override string GetArgumentString()
		{
			return "<e-mail>";
		}
		public override string GetHelpText()
		{
			return "Set the e-mail used for commits";
		}
	}
	class RepoSetNameCommand : Command
	{
		public RepoSetNameCommand()
		{
			Keyword = "set-name";
			RequiredParameters = 1;
		}
		public override ExitCode Run(IList<string> parameters)
		{
			Server.GetComponent<ITGRepository>().SetCommitterName(parameters[0]);
			return ExitCode.Normal;
		}
		public override string GetArgumentString()
		{
			return "<name>";
		}
		public override string GetHelpText()
		{
			return "Set the name used for commits";
		}
	}
	class RepoPythonPathCommand : Command
	{
		public RepoPythonPathCommand()
		{
			Keyword = "python-path";
			RequiredParameters = 1;
		}
		public override ExitCode Run(IList<string> parameters)
		{
			Server.GetComponent<ITGRepository>().SetPythonPath(parameters[0]);
			return ExitCode.Normal;
		}
		public override string GetArgumentString()
		{
			return "<path>";
		}
		public override string GetHelpText()
		{
			return "Set the path to the folder containing the python 2.7 installation";
		}
	}

	class RepoMergePRCommand : Command
	{
		public RepoMergePRCommand()
		{
			Keyword = "merge-pr";
			RequiredParameters = 1;
		}

		public override ExitCode Run(IList<string> parameters)
		{
			ushort PR;
			try
			{
				PR = Convert.ToUInt16(parameters[0]);
			}
			catch
			{
				OutputProc("Invalid PR Number!");
				return ExitCode.BadCommand;
			}
			var res = Server.GetComponent<ITGRepository>().MergePullRequest(PR);
			OutputProc(res ?? "Success");
			return res == null ? ExitCode.Normal : ExitCode.ServerError;
		}
		
		public override string GetArgumentString()
		{
			return "<pr #>";
		}
		public override string GetHelpText()
		{
			return "Merge the given pull request from the origin repository into the current branch. Only supported with github remotes";
		}
	}

	class RepoListPRsCommand : Command
	{
		public RepoListPRsCommand()
		{
			Keyword = "list-prs";
		}
		public override string GetHelpText()
		{
			return "Lists currently merge pull requests";
		}
		public override ExitCode Run(IList<string> parameters)
		{
			var data = Server.GetComponent<ITGRepository>().MergedPullRequests(out string error);
			if (data == null)
			{
				OutputProc(error);
				return ExitCode.ServerError;
			}
			if (data.Count == 0)
				OutputProc("None!");
			else
				foreach (var I in data)
					OutputProc(String.Format("#{0}: {2} by {3} at commit {1}", I.Number, I.Sha, I.Title, I.Author));
			return ExitCode.Normal;
		}
	}

	class RepoListBackupsCommand : Command
	{
		public RepoListBackupsCommand()
		{
			Keyword = "list-backups";
		}
		public override string GetHelpText()
		{
			return "Lists backup tags created by compilation";
		}
		public override ExitCode Run(IList<string> parameters)
		{
			var data = Server.GetComponent<ITGRepository>().ListBackups(out string error);
			if (data == null)
			{
				OutputProc(error);
				return ExitCode.ServerError;
			}
			if (data.Count == 0)
				OutputProc("None!");
			else
				foreach (var I in data)
					OutputProc(String.Format("{0} at commit {1}", I.Key, I.Value));
			return ExitCode.Normal;
		}
	}
	class RepoCheckoutCommand : Command
	{
		public RepoCheckoutCommand()
		{
			Keyword = "checkout";
			RequiredParameters = 1;
		}
		public override string GetHelpText()
		{
			return "Checks out the targeted object";
		}
		public override ExitCode Run(IList<string> parameters)
		{
			var res = Server.GetComponent<ITGRepository>().Checkout(parameters[0]);
			OutputProc(res ?? "Success");
			return res == null ? ExitCode.Normal : ExitCode.ServerError;
		}
	}
}
