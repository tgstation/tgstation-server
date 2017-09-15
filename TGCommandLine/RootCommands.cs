using System;
using System.Collections.Generic;
using TGServiceInterface;

namespace TGCommandLine
{
	class CLICommand : RootCommand
	{
		public CLICommand()
		{
			var tmp = new List<Command> { new UpdateCommand(), new TestmergeCommand(), new RepoCommand(), new BYONDCommand(), new DMCommand(), new DDCommand(), new ConfigCommand(), new IRCCommand(), new DiscordCommand() };
			if (Server.VerifyConnection() == null && Server.Authenticate() && Server.AuthenticateAdmin())
				tmp.Add(new AdminCommand());
			Children = tmp.ToArray();
		}

		public override void PrintHelp()
		{
			OutputProc("/tg/station 13 Server Command Line");
			base.PrintHelp();
		}
	}
	class UpdateCommand : Command
	{
		public UpdateCommand()
		{
			Keyword = "update";
			RequiredParameters = 1;
		}
		protected override ExitCode Run(IList<string> parameters)
		{
			var gen_cl = parameters.Count > 1 && parameters[1].ToLower() == "--cl";
			var Repo = Server.GetComponent<ITGRepository>();
			switch (parameters[0].ToLower())
			{
				case "hard":
					var res = Repo.Update(true);
					if (res != null)
					{
						OutputProc(res);
						return ExitCode.ServerError;
					}
					break;
				case "merge":
					res = Repo.Update(false);
					if (res != null)
					{
						OutputProc(res);
						return ExitCode.ServerError;
					}
					break;
				default:
					OutputProc("Please specify hard or merge");
					return ExitCode.BadCommand;
			}
			if (gen_cl)
			{
				Repo.GenerateChangelog(out string res);
				if (res != null)
					OutputProc(res);
				else
				{
					res = Repo.PushChangelog();
					if (res != null)
						OutputProc(res);
				}
			}
			var resu = Server.GetComponent<ITGCompiler>().Compile(true);
			OutputProc(resu ? "Compilation started!" : "Compilation could not be started!");
			return resu ? ExitCode.Normal : ExitCode.ServerError;
		}

		public override string GetArgumentString()
		{
			return "<merge|hard> [--cl]";
		}

		public override string GetHelpText()
		{
			return "Updates the server fully, optionally generating and pushing a changelog. Runs asynchronously once compilation starts";
		}
	}

	class TestmergeCommand : Command
	{
		public TestmergeCommand()
		{
			Keyword = "testmerge";
			RequiredParameters = 1;
		}
		protected override ExitCode Run(IList<string> parameters)
		{
			ushort tm;
			try
			{
				tm = Convert.ToUInt16(parameters[0]);
				if (tm == 0)
					throw new Exception();
			}
			catch
			{
				OutputProc("Invalid tesmerge #: " + parameters[0]);
				return ExitCode.BadCommand;
			}
			var Repo = Server.GetComponent<ITGRepository>();
			var res = Repo.MergePullRequest(tm);
			if (res != null)
			{
				OutputProc(res);
				return ExitCode.ServerError;
			}
			Repo.GenerateChangelog(out res);
			if (res != null)
			{
				OutputProc(res);
				return ExitCode.ServerError;
			}
			var resu = Server.GetComponent<ITGCompiler>().Compile(true);
			OutputProc(resu ? "Compilation started!" : "Compilation could not be started!");
			return resu ? ExitCode.Normal : ExitCode.ServerError;
		}
		public override string GetArgumentString()
		{
			return "<pull request #>";
		}

		public override string GetHelpText()
		{
			return "Merges the specified pull request and updates the server";
		}
	}
}
