using System.Collections.Generic;
using TGServiceInterface;

namespace TGCommandLine
{
	class AdminCommand : RootCommand
	{
		public AdminCommand()
		{
			Keyword = "admin";
			Children = new Command[] { new AdminViewGroupCommand(), new AdminSetGroupCommand(), new AdminClearGroupCommand() };
		}
		public override string GetHelpText()
		{
			return "Manage server service authentication";
		}
	}

	class AdminViewGroupCommand : Command
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
			var group = Server.GetComponent<ITGAdministration>().GetCurrentAuthorizedGroup();
			OutputProc(group ?? "ERROR");
			return group != null ? ExitCode.Normal : ExitCode.ServerError;
		}
	}

	class AdminSetGroupCommand : Command
	{
		public AdminSetGroupCommand()
		{
			Keyword = "set-group";
			RequiredParameters = 1;
		}

		public override string GetHelpText()
		{
			return "Search for and set the windows group allowed to use the service";
		}

		public override string GetArgumentString()
		{
			return "<windows group name>";
		}
		protected override ExitCode Run(IList<string> parameters)
		{
			var result = Server.GetComponent<ITGAdministration>().SetAuthorizedGroup(parameters[0]);
			if(result != null)
			{
				OutputProc("Group set to: " + result);
				return ExitCode.Normal;
			}
			else
			{
				OutputProc("Search failed to find a group named: " + parameters[0]);
				return ExitCode.ServerError;
			}
		}
	}

	class AdminClearGroupCommand : Command
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
			var res = Server.GetComponent<ITGAdministration>().SetAuthorizedGroup(null);
			if(res != "ADMIN")
			{
				OutputProc("Failed to clear the group??? We are currently set to: " + res);
				return ExitCode.ServerError;
			}
			return ExitCode.Normal;
		}
	}
}
