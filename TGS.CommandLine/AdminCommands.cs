using System.Collections.Generic;
using TGS.Interface;
using TGS.Interface.Components;

namespace TGS.CommandLine
{
	class AdminCommand : InstanceRootCommand
	{
		public AdminCommand()
		{
			Keyword = "admin";
			Children = new Command[] { new AdminViewGroupCommand(), new AdminSetGroupCommand(), new AdminClearGroupCommand(), new AdminRecreateStaticCommand() };
		}
		public override string GetHelpText()
		{
			return "Manage instance authentication";
		}
	}

	class AdminRecreateStaticCommand : ConsoleCommand
	{
		public AdminRecreateStaticCommand()
		{
			Keyword = "recreate-static-directory";
		}

		protected override ExitCode Run(IList<string> parameters)
		{
			var res = Interface.GetComponent<ITGAdministration>().RecreateStaticFolder();
			OutputProc(res ?? "Success");
			return res == null ? ExitCode.Normal : ExitCode.ServerError;
		}

		public override string GetHelpText()
		{
			return "Backup the current static directory and repopulate it from the TGS3.json in the repository";
		}
	}

	class AdminViewGroupCommand : ConsoleCommand
	{
		public AdminViewGroupCommand()
		{
			Keyword = "view-group";
		}
		public override string GetHelpText()
		{
			return "Print the name of the windows group that is allowed to use the service";
		}

		protected override ExitCode Run(IList<string> parameters)
		{
			var group = Interface.GetComponent<ITGAdministration>().GetCurrentAuthorizedGroup();
			OutputProc(group ?? "ERROR");
			return group != null ? ExitCode.Normal : ExitCode.ServerError;
		}
	}

	class AdminSetGroupCommand : ConsoleCommand
	{
		public AdminSetGroupCommand()
		{
			Keyword = "set-group";
			RequiredParameters = 1;
		}

		public override string GetHelpText()
		{
			return "Set the windows group allowed to use the service";
		}

		public override string GetArgumentString()
		{
			return "<windows group name>";
		}
		protected override ExitCode Run(IList<string> parameters)
		{
			var result = Interface.GetComponent<ITGAdministration>().SetAuthorizedGroup(parameters[0]);
			if(result != null)
			{
				OutputProc("Group set to: " + result);
				return ExitCode.Normal;
			}
			else
			{
				OutputProc("Failed to find a group named: " + parameters[0]);
				return ExitCode.ServerError;
			}
		}
	}

	class AdminClearGroupCommand : ConsoleCommand
	{
		public AdminClearGroupCommand()
		{
			Keyword = "clear-group";
		}

		public override string GetHelpText()
		{
			return "Clears the groups allowed to use the service, leaving only windows administrators";
		}

		protected override ExitCode Run(IList<string> parameters)
		{
			var res = Interface.GetComponent<ITGAdministration>().SetAuthorizedGroup(null);
			if(res != "ADMIN")
			{
				OutputProc("Failed to clear the group??? We are currently set to: " + res);
				return ExitCode.ServerError;
			}
			return ExitCode.Normal;
		}
	}
}
