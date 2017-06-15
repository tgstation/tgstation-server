using System;
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
				Children = new Command[] { new UpdateCommand(), new TestmergeCommand(), new IRCCommand(), new RepoCommand(), new BYONDCommand(), new DMCommand(), new DDCommand(), new ConfigCommand(), new ChatCommand(), new ServiceUpdateCommand(), new ListInstancesCommand(), new CreateInstanceCommand(), new DeleteInstanceCommand() };
			RequiresInstance = false;
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
								if(c.RequiresInstance && Program.Instance == 0)
								{
									Console.WriteLine("Please specify an instance with --instance [id]");
									return ExitCode.BadCommand;
								}
								return c.Run(parameters);
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
			var result = Service.GetComponent<ITGInstance>(Program.Instance).UpdateServer(method, gen_cl);
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
			var result = Service.GetComponent<ITGInstance>(Program.Instance).UpdateServer(TGRepoUpdateMethod.None, false, tm);
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
			RequiresInstance = false;
		}

		public override ExitCode Run(IList<string> parameters)
		{
			if (parameters.Count == 0 || parameters[0] != "--verify")
				Console.WriteLine("This command should only be used by the installer program. Please use the --verify option to confirm this command");
			else
			{
				Service.Get().StopForUpdate();
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

	class ListInstancesCommand : Command
	{
		public ListInstancesCommand()
		{
			Keyword = "instance-list";
			RequiresInstance = false;
		}

		public override ExitCode Run(IList<string> parameters)
		{
			var instances = Service.Get().ListInstances();
			if (instances.Count > 0)
				foreach (var I in instances)
					Console.WriteLine(String.Format("\t{0}.\t-\t{1}", I.Key, I.Value));
			else
				Console.WriteLine("\tNone");
			return ExitCode.Normal;
		}

		protected override string GetHelpText()
		{
			return "Lists the IDs and names of all server instances";
		}
	}

	class CreateInstanceCommand : Command
	{
		public CreateInstanceCommand()
		{
			Keyword = "instance-create";
			RequiresInstance = false;
			RequiredParameters = 2;
		}

		public override ExitCode Run(IList<string> parameters)
		{
			var S = Service.Get();
			var res = S.CreateInstance(parameters[0], parameters[1]);
			int instanceID = 0;
			if (res == null)
			{
				foreach (var I in S.ListInstances())
					if (I.Value == parameters[0])
					{
						instanceID = I.Key;
						break;
					}
				//instance ID should never be 0 here
			}
			Console.WriteLine(res ?? String.Format("Instance {0} created!", instanceID));
			return res == null ? ExitCode.Normal : ExitCode.ServerError;
		}

		protected override string GetHelpText()
		{
			return "Create a new server instance";
		}

		protected override string GetArgumentString()
		{
			return "<instance name> <instance folder path>";
		}
	}

	class DeleteInstanceCommand : Command
	{
		public DeleteInstanceCommand()
		{
			Keyword = "instance-delete";
		}
		protected override string GetHelpText()
		{
			return "Deletes a server instance";
		}
		public override ExitCode Run(IList<string> parameters)
		{
			if (parameters.Count > 0 && parameters[0] == "--confirm")
				if (parameters.Count > 1 && parameters[1] == "--verify")
				{
					string instanceName = null;
					foreach (var I in Service.Get().ListInstances())
						if (I.Key == Program.Instance) {
							instanceName = I.Value;
							break;
						}
					Console.WriteLine("Deleting instance " + instanceName + "...");
					Service.GetComponent<ITGInstance>(Program.Instance).Delete();
					Thread.Sleep(2000);
				}
				else
				{
					Console.WriteLine("Are you absolutely sure you're sure? There's no turning back after this!");
					Console.WriteLine("Add an additional --verify parameter if you're sure");
				}
			else
			{
				Console.WriteLine("WARNING: This will delete the ENTIRE server instance! Absolutely no backup operation will be performed!");
				Console.WriteLine("Add a --confirm parameter to continue");
			}
			return ExitCode.Normal;
		}
	}
}
