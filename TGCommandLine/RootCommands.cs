﻿using System;
using System.Collections.Generic;
using System.Threading;
using TGServiceInterface;

namespace TGCommandLine
{

	class RootCommand : Command
	{
		bool IsRealRoot()
		{
			return !GetType().IsSubclassOf(typeof(RootCommand));
		}
		public RootCommand()
		{
			if (IsRealRoot())   //stack overflows
				Children = new Command[] { new UpdateCommand(), new TestmergeCommand(), new IRCCommand(), new RepoCommand(), new BYONDCommand(), new DMCommand(), new DDCommand(), new ConfigCommand(), new ChatCommand(), new ServiceUpdateCommand() };
		}
		public override ExitCode Run(IList<string> parameters)
		{
			if (parameters.Count > 0)
			{
				var LocalKeyword = parameters[0].Trim().ToLower();
				parameters.RemoveAt(0);

				switch (LocalKeyword)
				{
					case "help":
					case "?":
						if (!IsRealRoot())
						{
							Console.WriteLine(Keyword + " commands:");
							Console.WriteLine();
						}
						PrintHelp();
						return ExitCode.Normal;
					default:
						foreach (var c in Children)
							if (c.Keyword == LocalKeyword)
							{
								if (parameters.Count < c.RequiredParameters)
								{
									Console.WriteLine("Not enough parameters!");
									return ExitCode.BadCommand;
								}
								return c.WrapRun(parameters);
							}
						parameters.Insert(0, LocalKeyword);
						break;
				}
			}
			Console.WriteLine(String.Format("Invalid command: {0} {1}", Keyword, String.Join(" ", parameters)));
			Console.WriteLine(String.Format("Type '{0}?' or '{0}help' for available commands.", Keyword != null ? Keyword + " " : ""));
			return ExitCode.BadCommand;
		}

		public override void PrintHelp()
		{
			Console.WriteLine("/tg/station 13 Server Command Line");
			Console.WriteLine("Available commands (type '?' or 'help' after command for more info):");
			Console.WriteLine();
			base.PrintHelp();
		}

		protected override string GetHelpText()
		{
			throw new NotImplementedException();
		}
	}

	class UpdateCommand : Command
	{
		public UpdateCommand()
		{
			Keyword = "update";
			RequiredParameters = 1;
		}
		public override ExitCode Run(IList<string> parameters)
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
					Console.WriteLine("Please specify hard or merge");
					return ExitCode.BadCommand;
			}
			var result = Service.GetComponent<ITGInstance>().UpdateServer(method, gen_cl);
			Console.WriteLine(result ?? "Compilation started!");
			return result == null ? ExitCode.Normal : ExitCode.ServerError;
		}

		protected override string GetArgumentString()
		{
			return "<merge|hard> [--cl]";
		}

		protected override string GetHelpText()
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
		public override ExitCode Run(IList<string> parameters)
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
				Console.WriteLine("Invalid tesmerge #: " + parameters[0]);
				return ExitCode.BadCommand;
			}
			var result = Service.GetComponent<ITGInstance>().UpdateServer(TGRepoUpdateMethod.None, false, tm);
			Console.WriteLine(result ?? "Compilation started!");
			return result == null ? ExitCode.Normal : ExitCode.ServerError;
		}
		protected override string GetArgumentString()
		{
			return "<pull request #>";
		}

		protected override string GetHelpText()
		{
			return "Merges the specified pull request and updates the server";
		}
	}

	class ServiceUpdateCommand : Command
	{
		public ServiceUpdateCommand()
		{
			Keyword = "service-update";
		}

		public override ExitCode Run(IList<string> parameters)
		{
			if (parameters.Count == 0 || parameters[0] != "--verify")
				Console.WriteLine("This command should only be used by the installer program. Please use the --verify option to confirm this command");
			else
			{
				Service.GetComponent<ITGSService>().StopForUpdate();
				GC.Collect();
				Thread.Sleep(10000);
			}
			return ExitCode.Normal;
		}

		protected override string GetArgumentString()
		{
			return "[--verify]";
		}

		protected override string GetHelpText()
		{
			return "Internal. Stops the service in preparation of an update operation.";
		}
	}

	class SetInstanceCommand : Command
	{

	}
}
