using TGServiceInterface;
using System;
using System.Collections.Generic;
using System.Threading;

namespace TGServerService
{
	class CommandInfo
	{
		public bool IsAdmin { get; set; }
		public bool IsAdminChannel { get; set; }
		public string Speaker { get; set; }
		public TGStationServer Server { get; set; }
	}
	abstract class ChatCommand : Command
	{
		public bool RequiresAdmin { get; protected set; }
		public static ThreadLocal<CommandInfo> CommandInfo = new ThreadLocal<CommandInfo>();
		protected TGStationServer Instance { get { return CommandInfo.Value.Server; } }
		public override ExitCode DoRun(IList<string> parameters)
		{
			if (RequiresAdmin)
			{
				var Info = CommandInfo.Value;
				if (!Info.IsAdmin)
				{
					OutputProc("You are not authorized to use that command!");
					return ExitCode.BadCommand;
				}
				if (!Info.IsAdminChannel)
				{
					OutputProc("Use this command in an admin channel!");
					return ExitCode.BadCommand;
				}
			}
			return base.DoRun(parameters);
		}
	}

	class ServerChatCommand : ChatCommand
	{
		readonly string HelpText;
		public ServerChatCommand(string name, string helpText, bool adminOnly, int requiredParameters)
		{
			Keyword = name;
			RequiresAdmin = adminOnly;
			HelpText = helpText;
			RequiredParameters = requiredParameters;
		}

		public override string GetHelpText()
		{
			return HelpText;
		}

		protected override ExitCode Run(IList<string> parameters)
		{
			var res = Instance.SendCommand(String.Format("{0} {1}", Keyword, String.Join(" ", parameters)));
			if (res != "SUCESS" && !String.IsNullOrWhiteSpace(res))
				OutputProc(res);
			return ExitCode.Normal;
		}
	}

	class RootChatCommand : RootCommand
	{
		public RootChatCommand(List<Command> serverCommands)
		{
			if (serverCommands == null)
				serverCommands = new List<Command>();
			PrintHelpList = true;
			serverCommands.AddRange(new Command[] { new PRsCommand(), new VersionCommand(), new RevisionCommand(), new ByondCommand(), new KekCommand() });
			Children = serverCommands.ToArray();
		}
	}
	class RevisionCommand : ChatCommand
	{
		public RevisionCommand()
		{
			Keyword = "revision";
		}
		protected override ExitCode Run(IList<string> parameters)
		{
			OutputProc(Instance.GetHead(out string error) ?? error);
			return error == null ? ExitCode.Normal : ExitCode.ServerError;
		}

		public override string GetHelpText()
		{
			return "Returns mob info of the specified target";
		}
	}

	class ByondCommand : ChatCommand
	{
		public ByondCommand()
		{
			Keyword = "byond";
		}
		protected override ExitCode Run(IList<string> parameters)
		{
			var type = TGByondVersion.Installed;
			if (parameters.Count > 0)
				if (parameters[0].ToLower() == "--staged")
					type = TGByondVersion.Staged;
				else if (parameters[0].ToLower() == "--latest")
					type = TGByondVersion.Latest;
			OutputProc(Instance.GetVersion(type) ?? "None");
			return ExitCode.Normal;
		}

		public override string GetHelpText()
		{
			return "Gets the specified BYOND version";
		}
		public override string GetArgumentString()
		{
			return "[--staged|--latest]";
		}
	}
	class VersionCommand : ChatCommand
	{
		public VersionCommand()
		{
			Keyword = "version";
		}
		protected override ExitCode Run(IList<string> parameters)
		{
			OutputProc(Instance.Version());
			return ExitCode.Normal;
		}

		public override string GetHelpText()
		{
			return "Gets the running service version";
		}
	}
	class KekCommand : ChatCommand
	{
		public KekCommand()
		{
			Keyword = "kek";
		}
		protected override ExitCode Run(IList<string> parameters)
		{
			OutputProc("kek");
			return ExitCode.Normal;
		}

		public override string GetHelpText()
		{
			return "kek";
		}
	}
	class PRsCommand : ChatCommand
	{
		public PRsCommand()
		{
			Keyword = "prs";
		}
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
					res += I.Number + " ";
				OutputProc(res);
			}
			return ExitCode.Normal;
		}

		public override string GetHelpText()
		{
			return "Gets the currently merged pull requests in the repository";
		}
	}
	
}
