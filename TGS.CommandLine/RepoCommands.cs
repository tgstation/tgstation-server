﻿using System;
using System.Collections.Generic;
using TGS.Interface;
using TGS.Interface.Components;

namespace TGS.CommandLine
{
	class RepoCommand : RootCommand
	{
		public RepoCommand()
		{
			Keyword = "repo";
			Children = new Command[] { new RepoSetupCommand(), new RepoUpdateCommand(), new RepoGenChangelogCommand(), new RepoPushChangelogCommand(), new RepoSetEmailCommand(), new RepoSetNameCommand(), new RepoMergePRCommand(), new RepoListPRsCommand(), new RepoStatusCommand(), new RepoListBackupsCommand(), new RepoCheckoutCommand(), new RepoResetCommand(), new RepoUpdateJsonCommand(), new RepoSetPushTestmergeCommitsCommand() };
		}
		public override string GetHelpText()
		{
			return "Manage the git repository";
		}
	}

	class RepoSetPushTestmergeCommitsCommand : ConsoleCommand
	{
		public RepoSetPushTestmergeCommitsCommand()
		{
			Keyword = "push-testmerges";
			RequiredParameters = 1;
		}
		public override string GetHelpText()
		{
			return "Set if a temporary branch is to the remote when we make testmerge commits and then delete it";
		}

		public override string GetArgumentString()
		{
			return "<on|off>";
		}

		protected override ExitCode Run(IList<string> parameters)
		{
			switch (parameters[0].ToLower())
			{
				case "on":
					Interface.GetComponent<ITGRepository>().SetPushTestmergeCommits(true);
					break;
				case "off":
					Interface.GetComponent<ITGRepository>().SetPushTestmergeCommits(false);
					break;
				default:
					OutputProc("Invalid option!");
					return ExitCode.BadCommand;
			}
			return ExitCode.Normal;
		}
	}

	class RepoUpdateJsonCommand : ConsoleCommand
	{
		public RepoUpdateJsonCommand()
		{
			Keyword = "update-json";
		}

		public override string GetHelpText()
		{
			return "Updates the cached TGS3.json with the one from the repo. Compilation is blocked if these two do not match.";
		}

		protected override ExitCode Run(IList<string> parameters)
		{
			var res = Interface.GetComponent<ITGRepository>().UpdateTGS3Json();
			if (res != null)
			{
				OutputProc(res);
				return ExitCode.ServerError;
			}
			return ExitCode.Normal;
		}
	}

	class RepoSetupCommand : ConsoleCommand
	{
		public RepoSetupCommand()
		{
			Keyword = "setup";
			RequiredParameters = 1;
		}
		protected override ExitCode Run(IList<string> parameters)
		{
			var res = Interface.GetComponent<ITGRepository>().Setup(parameters[0], parameters.Count > 1 ? parameters[1] : "master");
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

	class RepoStatusCommand : ConsoleCommand
	{
		public RepoStatusCommand()
		{
			Keyword = "status";
		}
		protected override ExitCode Run(IList<string> parameters)
		{
			var Repo = Interface.GetComponent<ITGRepository>();
			var busy = Repo.OperationInProgress();
			if (!busy)
			{
				OutputProc("Repo: Idle");
				var head = Repo.GetHead(false, out string error);
				if (head == null)
					head = "Error: " + error;
				var remotehead = Repo.GetHead(true, out error);
				if (remotehead == null)
					remotehead = "Error: " + error;
				var branch = Repo.GetBranch(out error);
				if (branch == null)
					branch = "Error: " + error;
				var remote = Repo.GetRemote(out error);
				if (remote == null)
					remote = "Error: " + error;
				OutputProc("Remote: " + remote + " (" + remotehead + ")");
				OutputProc("Branch: " + branch);
				OutputProc("HEAD: " + head);
				OutputProc("Push testmerge commits: " + (Repo.PushTestmergeCommits() ? "ON" : "OFF"));
				OutputProc(String.Format("Committer Identity: {0} ({1})", Repo.GetCommitterName(), Repo.GetCommitterEmail()));
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

	class RepoResetCommand : ConsoleCommand
	{
		public RepoResetCommand()
		{
			Keyword = "reset";
		}
		protected override ExitCode Run(IList<string> parameters)
		{
			var result = Interface.GetComponent<ITGRepository>().Reset(parameters.Count > 0 && parameters[0].ToLower() == "--origin");
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

	class RepoUpdateCommand : ConsoleCommand
	{
		public RepoUpdateCommand()
		{
			Keyword = "update";
			RequiredParameters = 1;
		}
		protected override ExitCode Run(IList<string> parameters)
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
			var res = Interface.GetComponent<ITGRepository>().Update(hard).Result;
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
	class RepoGenChangelogCommand : ConsoleCommand
	{
		public RepoGenChangelogCommand()
		{
			Keyword = "gen-changelog";
		}
		protected override ExitCode Run(IList<string> parameters)
		{
			var result = Interface.GetComponent<ITGRepository>().GenerateChangelog(out string error);
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
	class RepoPushChangelogCommand : ConsoleCommand
	{
		public RepoPushChangelogCommand()
		{
			Keyword = "push-changelog";
		}
		protected override ExitCode Run(IList<string> parameters)
		{
			var result = Interface.GetComponent<ITGRepository>().SynchronizePush();
			if(result != null)
				OutputProc(result);
			return result == null ? ExitCode.Normal : ExitCode.ServerError;
		}

		public override string GetHelpText()
		{
			return "Pushes the html changelog if the SSH authentication is configured correctly";
		}
	}
	class RepoSetEmailCommand : ConsoleCommand
	{
		public RepoSetEmailCommand()
		{
			Keyword = "set-email";
			RequiredParameters = 1;
		}
		protected override ExitCode Run(IList<string> parameters)
		{
			Interface.GetComponent<ITGRepository>().SetCommitterEmail(parameters[0]);
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
	class RepoSetNameCommand : ConsoleCommand
	{
		public RepoSetNameCommand()
		{
			Keyword = "set-name";
			RequiredParameters = 1;
		}
		protected override ExitCode Run(IList<string> parameters)
		{
			Interface.GetComponent<ITGRepository>().SetCommitterName(parameters[0]);
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

	class RepoMergePRCommand : ConsoleCommand
	{
		public RepoMergePRCommand()
		{
			Keyword = "merge-pr";
			RequiredParameters = 1;
		}

		protected override ExitCode Run(IList<string> parameters)
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
			var res = Interface.GetComponent<ITGRepository>().MergePullRequest(PR);
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

	class RepoListPRsCommand : ConsoleCommand
	{
		public RepoListPRsCommand()
		{
			Keyword = "list-prs";
		}
		public override string GetHelpText()
		{
			return "Lists currently merge pull requests";
		}
		protected override ExitCode Run(IList<string> parameters)
		{
			var data = Interface.GetComponent<ITGRepository>().MergedPullRequests().Result;
			if (data == null)
			{
				//OutputProc(error);
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

	class RepoListBackupsCommand : ConsoleCommand
	{
		public RepoListBackupsCommand()
		{
			Keyword = "list-backups";
		}
		public override string GetHelpText()
		{
			return "Lists backup tags created by compilation";
		}
		protected override ExitCode Run(IList<string> parameters)
		{
			var data = Interface.GetComponent<ITGRepository>().ListBackups(out string error);
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
	class RepoCheckoutCommand : ConsoleCommand
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
		protected override ExitCode Run(IList<string> parameters)
		{
			var res = Interface.GetComponent<ITGRepository>().Checkout(parameters[0]);
			OutputProc(res ?? "Success");
			return res == null ? ExitCode.Normal : ExitCode.ServerError;
		}
	}
}
