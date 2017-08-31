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
			PrintHelpList = true;
			Children = new Command[] { new CheckCommand(),
                new StatusCommand(),
                new PRsCommand(),
                new VersionCommand(),
                new AHelpCommand(),
                new NameCheckCommand(),
                new RevisionCommand(),
                new AdminWhoCommand(),
                new ByondCommand(),
                new KekCommand(),
                new RelayRestartCommand(),
                new UpdateCommand(),
                new DDRestartCommand()
            };
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
	class RelayRestartCommand : ChatCommand
	{
		public RelayRestartCommand()
		{
			Keyword = "relayrestart";
			RequiresAdmin = true;
		}
		protected override ExitCode Run(IList<string> parameters)
		{
			Instance.InitInterop();
			return ExitCode.Normal;
		}

		public override string GetHelpText()
		{
			return "Restart the relay listener. This is a massive hack but it'll do for now";
		}
	}

    class UpdateCommand : ChatCommand
    {
        public UpdateCommand()
        {
            Keyword = "update";
            RequiresAdmin = true;
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
            var res = Server.GetComponent<ITGRepository>().Update(hard);
            OutputProc(res ?? "Success");

            var exitcode = res == null ? ExitCode.Normal : ExitCode.ServerError;
            if (exitcode != ExitCode.Normal)
            {
                return exitcode;
            }

            var DM = Server.GetComponent<ITGCompiler>();
            var stat = DM.GetStatus();
            if (stat != TGCompilerStatus.Initialized)
            {
                OutputProc("Error: Compiler is " + ((stat == TGCompilerStatus.Uninitialized) ? "unintialized!" : "busy with another task!"));
                return ExitCode.ServerError;
            }

            if (Server.GetComponent<ITGByond>().GetVersion(TGByondVersion.Installed) == null)
            {
                OutputProc("Error: BYOND is not installed!");
                return ExitCode.ServerError;
            }

            if (!DM.Compile())
            {
                OutputProc("Error: Unable to start compilation!");
                var err = DM.CompileError();
                if (err != null)
                    OutputProc(err);
                return ExitCode.ServerError;
            }
            OutputProc("Compile job started");
            do
            {
                Thread.Sleep(1000);
            } while (DM.GetStatus() == TGCompilerStatus.Compiling);
            res = DM.CompileError();
            OutputProc(res ?? "Compilation successful");
            if (res != null)
                return ExitCode.ServerError;

            return ExitCode.Normal;
        }

        public override string GetHelpText()
        {
            return "Trigger an update of the server";
        }

        public override string GetArgumentString()
        {
            return "<hard|merge>";
        }
    }

    class DDRestartCommand : ChatCommand
    {
        public DDRestartCommand()
        {
            Keyword = "restart";
            RequiresAdmin = true;
        }

        public override string GetArgumentString()
        {
            return "[--graceful]";
        }
        protected override ExitCode Run(IList<string> parameters)
        {
            var DD = Server.GetComponent<ITGDreamDaemon>();
            if (parameters.Count > 0 && parameters[0].ToLower() == "--graceful")
            {
                if (DD.DaemonStatus() != TGDreamDaemonStatus.Online)
                {
                    OutputProc("Error: The game is not currently running!");
                    return ExitCode.ServerError;
                }
                DD.RequestRestart();
                return ExitCode.Normal;
            }
            var res = DD.Restart();
            OutputProc(res ?? "Success!");
            return res == null ? ExitCode.Normal : ExitCode.ServerError;
        }

        public override string GetHelpText()
        {
            return "Restarts the server and watchdog optionally waiting for the current round to end";
        }
    }
}
