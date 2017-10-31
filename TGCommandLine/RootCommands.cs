using System;
using System.Collections.Generic;
using TGServiceInterface;
using TGServiceInterface.Components;

namespace TGCommandLine
{
	class CLICommand : RootCommand
	{
		public CLICommand(Interface I)
		{
			var tmp = new List<Command> { new UpdateCommand(), new TestmergeCommand(), new RepoCommand(), new BYONDCommand(), new DMCommand(), new DDCommand(), new ConfigCommand(), new IRCCommand(), new DiscordCommand(), new AutoUpdateCommand(), new SetAutoUpdateCommand() };
			if (I.VerifyConnection() == null && I.Authenticate() && I.AuthenticateAdmin())
				tmp.Add(new AdminCommand());
			Children = tmp.ToArray();
		}

		public override void PrintHelp()
		{
			OutputProc("/tg/station 13 Server Command Line");
			base.PrintHelp();
		}
	}
	class AutoUpdateCommand : ConsoleCommand
	{
		public AutoUpdateCommand()
		{
			Keyword = "auto-update";
		}

		public override string GetHelpText()
		{
			return "Get the interval in minutes that the server automatically updates";
		}

		protected override ExitCode Run(IList<string> parameters)
		{
			var res = Interface.GetComponent<ITGRepository>().AutoUpdateInterval();
			OutputProc(res == 0 ? "OFF" : String.Format("Auto updating every {0} minutes", res));
			return ExitCode.Normal;
		}
	}
	class SetAutoUpdateCommand : ConsoleCommand
	{
		public SetAutoUpdateCommand()
		{
			Keyword = "set-auto-update";
			RequiredParameters = 1;
		}

		public override string GetArgumentString()
		{
			return "<off|interval in minutes>";
		}

		public override string GetHelpText()
		{
			return "Set the interval in minutes that the server automatically updates";
		}

		protected override ExitCode Run(IList<string> parameters)
		{
			ulong NewInterval;
			if (parameters[0].ToLower() == "off")
				NewInterval = 0;
			else
				try
				{
					NewInterval = Convert.ToUInt64(parameters[0]);
				}
				catch
				{
					OutputProc("Invalid interval specified!");
					return ExitCode.BadCommand;
				}

			Interface.GetComponent<ITGRepository>().SetAutoUpdateInterval(NewInterval);
			return ExitCode.Normal;
		}

	}
	class UpdateCommand : ConsoleCommand
	{
		public UpdateCommand()
		{
			Keyword = "update";
			RequiredParameters = 1;
		}
		protected override ExitCode Run(IList<string> parameters)
		{
			var gen_cl = parameters.Count > 1 && parameters[1].ToLower() == "--cl";
			var Repo = Interface.GetComponent<ITGRepository>();
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
					res = Repo.SynchronizePush();
					if (res != null)
						OutputProc(res);
				}
			}
			var resu = Interface.GetComponent<ITGCompiler>().Compile(true);
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

	class TestmergeCommand : ConsoleCommand
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
			var Repo = Interface.GetComponent<ITGRepository>();
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
			var resu = Interface.GetComponent<ITGCompiler>().Compile(true);
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
