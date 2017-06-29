using TGServiceInterface;
using System.Threading;
using System.Collections.Generic;
using System;

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
	class RootChatCommand : RootCommand
	{
		public RootChatCommand()
		{
			Children = new Command[] { new CheckCommand(), new StatusCommand(), new PRsCommand(), new VersionCommand(), new AHelpCommand(), new NameCheckCommand(), new RevisionCommand(), new AdminWhoCommand(), new ByondCommand(), new KekCommand() };
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
	class NameCheckCommand : ChatCommand
	{
		public NameCheckCommand()
		{
			Keyword = "namecheck";
			RequiredParameters = 1;
			RequiresAdmin = true;
		}
		protected override ExitCode Run(IList<string> parameters)
		{
			OutputProc(Instance.NameCheck(parameters[0], CommandInfo.Value.Speaker));
			return ExitCode.Normal;
		}

		public override string GetHelpText()
		{
			return "Returns mob info of the specified target";
		}
		public override string GetArgumentString()
		{
			return "<target>";
		}
	}
	class AdminWhoCommand : ChatCommand
	{
		public AdminWhoCommand()
		{
			Keyword = "adminwho";
			RequiresAdmin = true;
		}
		protected override ExitCode Run(IList<string> parameters)
		{
			OutputProc(Instance.SendCommand(TGStationServer.SCAdminWho));
			return ExitCode.Normal;
		}

		public override string GetHelpText()
		{
			return "Returns mob info of the specified target";
		}
		public override string GetArgumentString()
		{
			return "<target>";
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
			if (parameters.Count > 0)
				if (parameters[0].ToLower() == "--staged")
					OutputProc(Instance.GetVersion(TGByondVersion.Staged) ?? "None");
				else if (parameters[0].ToLower() == "--latest")
					OutputProc(Instance.GetVersion(TGByondVersion.Latest) ?? "Unknown");
			else
				OutputProc(Instance.GetVersion(TGByondVersion.Installed) ?? "Uninstalled");
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
	class CheckCommand : ChatCommand
	{
		public CheckCommand()
		{
			Keyword = "check";
		}
		protected override ExitCode Run(IList<string> parameters)
		{
			OutputProc(Instance.StatusString(CommandInfo.Value.IsAdmin && CommandInfo.Value.IsAdminChannel));
			return ExitCode.Normal;
		}

		public override string GetHelpText()
		{
			return "Gets the playercount, gamemode, and address of the server";
		}
	}

	class StatusCommand : ChatCommand
	{
		public StatusCommand()
		{
			Keyword = "status";
			RequiresAdmin = true;
		}
		protected override ExitCode Run(IList<string> parameters)
		{
			OutputProc(Instance.SendCommand(TGStationServer.SCIRCStatus));
			return ExitCode.Normal;
		}

		public override string GetHelpText()
		{
			return "Gets the admincount, playercount, gamemode, and true game mode of the server";
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

	class AHelpCommand : ChatCommand
	{
		public AHelpCommand()
		{
			Keyword = "ahelp";
			RequiresAdmin = true;
			RequiredParameters = 2;
		}
		protected override ExitCode Run(IList<string> parameters)
		{
			var ckey = parameters[0];
			parameters.RemoveAt(0);
			OutputProc(Instance.SendPM(ckey, CommandInfo.Value.Speaker, String.Join(" ", parameters)));
			return ExitCode.Normal;
		}

		public override string GetHelpText()
		{
			return "Respond to a relayed admin help request";
		}

		public override string GetArgumentString()
		{
			return "<ckey> <message|ticket <close|resolve|icissue|reject|reopen <ticket #>|list>>";
		}
	}
}
