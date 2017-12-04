using System;
using System.Collections.Generic;
using System.Threading;
using TGS.Interface;
using TGS.Interface.Components;

namespace TGS.CommandLine
{
	class BYONDCommand : InstanceRootCommand
	{
		public BYONDCommand()
		{
			Keyword = "byond";
			Children = new Command[] { new BYONDUpdateCommand(), new BYONDVersionCommand(), new BYONDStatusCommand() };
		}
		public override string GetHelpText()
		{
			return "Manage BYOND installation";
		}
	}

	class BYONDVersionCommand : ConsoleCommand
	{
		public BYONDVersionCommand()
		{
			Keyword = "version";
		}
		protected override ExitCode Run(IList<string> parameters)
		{
			var type = ByondVersion.Installed;
			if (parameters.Count > 0)
				if (parameters[0].ToLower() == "--staged")
					type = ByondVersion.Staged;
				else if (parameters[0].ToLower() == "--latest")
					type = ByondVersion.Latest;
			OutputProc(Interface.GetComponent<ITGByond>().GetVersion(type).Result ?? "Unistalled");
			return ExitCode.Normal;
		}
		public override string GetArgumentString()
		{
			return "[--staged|--latest]";
		}

		public override string GetHelpText()
		{
			return "Print the currently installed BYOND version";
		}
	}


	class BYONDStatusCommand : ConsoleCommand
	{
		public BYONDStatusCommand()
		{
			Keyword = "status";
		}
		protected override ExitCode Run(IList<string> parameters)
		{
			switch (Interface.GetComponent<ITGByond>().CurrentStatus().Result)
			{
				case ByondStatus.Downloading:
					OutputProc("Downloading update...");
					break;
				case ByondStatus.Idle:
					OutputProc("Updater Idle");
					break;
				case ByondStatus.Staged:
					OutputProc("Update staged and awaiting server restart");
					break;
				case ByondStatus.Staging:
					OutputProc("Staging update...");
					break;
				case ByondStatus.Starting:
					OutputProc("Starting update...");
					break;
				case ByondStatus.Updating:
					OutputProc("Applying update...");
					break;
				default:
					OutputProc("Limmexing (This is an error).");
					return ExitCode.ServerError;
			}
			return ExitCode.Normal;
		}
		public override string GetHelpText()
		{
			return "Print the current status of the BYOND updater";
		}
	}

	class BYONDUpdateCommand : ConsoleCommand
	{
		public BYONDUpdateCommand()
		{
			Keyword = "update";
			RequiredParameters = 2;
		}
		protected override ExitCode Run(IList<string> parameters)
		{
			int Major = 0, Minor = 0;
			try
			{
				Major = Convert.ToInt32(parameters[0]);
				Minor = Convert.ToInt32(parameters[1]);
			}
			catch
			{
				OutputProc("Please enter version as <Major>.<Minor>");
				return ExitCode.BadCommand;
			}

			var BYOND = Interface.GetComponent<ITGByond>();
			if (!BYOND.UpdateToVersion(Major, Minor).Result)

			{
				OutputProc("Failed to begin update!");
				return ExitCode.ServerError;
			}
			
			var stat = BYOND.CurrentStatus().Result;
			while (stat != ByondStatus.Idle && stat != ByondStatus.Staged)
			{
				Thread.Sleep(100);
				stat = BYOND.CurrentStatus().Result;
			}
			var res = BYOND.GetError().Result;
			OutputProc(res ?? (stat == ByondStatus.Staged ? "Update staged and will apply next DD reboot" : "Update finished"));
			return res == null ? ExitCode.Normal : ExitCode.ServerError;
		}
		public override string GetArgumentString()
		{
			return "<Major> <Minor>";
		}
		public override string GetHelpText()
		{
			return "Updates the BYOND installation to the specified version";
		}
	}
}
