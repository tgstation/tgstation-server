using System;
using System.Collections.Generic;
using TGServiceInterface;

namespace TGCommandLine
{
	class CLICommand : RootCommand
	{
		public CLICommand()
		{
			Children = new Command[] { new UpdateCommand(), new TestmergeCommand(), new RepoCommand(), new BYONDCommand(), new DMCommand(), new DDCommand(), new ConfigCommand(), new IRCCommand(), new DiscordCommand() };
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
			TGRepoUpdateMethod method;
			switch (parameters[0].ToLower())
			{
				case "hard":
					method = TGRepoUpdateMethod.Hard;
					break;
				case "merge":
					method = TGRepoUpdateMethod.Merge;
					break;
				default:
					OutputProc("Please specify hard or merge");
					return ExitCode.BadCommand;
			}
			var result = Server.GetComponent<ITGServerUpdater>().UpdateServer(method, gen_cl);
			OutputProc(result ?? "Compilation started!");
			return result == null ? ExitCode.Normal : ExitCode.ServerError;
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
			var result = Server.GetComponent<ITGServerUpdater>().UpdateServer(TGRepoUpdateMethod.None, false, tm);
			OutputProc(result ?? "Compilation started!");
			return result == null ? ExitCode.Normal : ExitCode.ServerError;
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
