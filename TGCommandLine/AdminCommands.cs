using System;
using System.Collections.Generic;
using TGServiceInterface;

namespace TGCommandLine
{
	class AdminCommand : RootCommand
	{
		public AdminCommand()
		{
			Keyword = "admin";
			Children = new Command[] { new AdminViewGroupCommand(), new AdminSetGroupCommand(), new AdminClearGroupCommand(), new AdminViewPortCommand(), new AdminSetPortCommand(), new AdminSetURLCommand(), new AdminViewURLCommand() };
		}
		public override string GetHelpText()
		{
			return "Manage server service authentication";
		}
	}



    class AdminViewURLCommand : Command
    {
        public AdminViewURLCommand()
        {
            Keyword = "view-url";
        }
        public override string GetHelpText()
        {
            return "Print the URL used to remotely access the service and to find a certificate";
        }

        protected override ExitCode Run(IList<string> parameters)
        {
            var group = Server.GetComponent<ITGAdministration>().RemoteURL();
            OutputProc(group ?? "ERROR");
            return group != null ? ExitCode.Normal : ExitCode.ServerError;
        }
    }

    class AdminSetURLCommand : Command
    {
        public AdminSetURLCommand()
        {
            Keyword = "set-url";
            RequiredParameters = 1;
        }

        public override string GetHelpText()
        {
            return "Set the URL used to remotely access the service and to find a certificate";
        }

        public override string GetArgumentString()
        {
            return "<url>";
        }
        protected override ExitCode Run(IList<string> parameters)
        {
            Server.GetComponent<ITGAdministration>().SetRemoteURL(parameters[0]);
            return ExitCode.Normal;
        }
    }

    class AdminSetPortCommand : Command
	{
		public AdminSetPortCommand()
		{
			Keyword = "set-port";
			RequiredParameters = 1;
		}
		public override string GetHelpText()
		{
			return "Set the port used for remote access. Requires a service restart to take effect";
		}

		public override string GetArgumentString()
		{
			return "<port>";
		}

		protected override ExitCode Run(IList<string> parameters)
		{
			ushort port;
			try
			{
				port = Convert.ToUInt16(parameters[0]);
			}
			catch
			{
				OutputProc("Invalid port number!");
				return ExitCode.BadCommand;
			}
			var res = Server.GetComponent<ITGAdministration>().SetRemoteAccessPort(port);
			OutputProc(res ?? "Success!");
			return ExitCode.Normal;
		}
	}

	class AdminViewPortCommand : Command {
		public AdminViewPortCommand()
		{
			Keyword = "view-port";
		}
		public override string GetHelpText()
		{
			return "Print the port currently designated for remote access";
		}

		protected override ExitCode Run(IList<string> parameters)
		{
			var port = Server.GetComponent<ITGAdministration>().RemoteAccessPort();
			OutputProc(String.Format("{0}", port));
			return ExitCode.Normal;
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
			var group = Server.GetComponent<ITGAdministration>().AuthorizedGroup();
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
			return "Set the windows group allowed to use the service";
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
				OutputProc("Failed to find a group named: " + parameters[0]);
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
