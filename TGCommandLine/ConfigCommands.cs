using System;
using System.Collections.Generic;
using System.IO;
using TGServiceInterface;

namespace TGCommandLine
{
	class ConfigCommand : RootCommand
	{
		public ConfigCommand()
		{
			Keyword = "config";
			Children = new Command[] { new ConfigMoveServerCommand(), new ConfigServerDirectoryCommand(), new ConfigDownloadCommand(), new ConfigUploadCommand() };
		}
		public override string GetHelpText()
		{
			return "Manage settings";
		}
	}

	class ConfigMoveServerCommand : Command
	{
		public ConfigMoveServerCommand()
		{
			Keyword = "move-server";
			RequiredParameters = 1;
		}

		protected override ExitCode Run(IList<string> parameters)
		{
			var res = Server.GetComponent<ITGConfig>().MoveServer(parameters[0]);
			OutputProc(res ?? "Success");
			return res == null ? ExitCode.Normal : ExitCode.ServerError;
		}

		public override string GetArgumentString()
		{
			return "<new path>";
		}

		public override string GetHelpText()
		{
			return "Move the server installation (BYOND, Repo, Game) to a new location. Nothing else may be running for this task to complete";
		}
	}

	class ConfigServerDirectoryCommand : Command
	{
		public ConfigServerDirectoryCommand()
		{
			Keyword = "server-dir";
		}

		protected override ExitCode Run(IList<string> parameters)
		{
			OutputProc(Server.GetComponent<ITGConfig>().ServerDirectory());
			return ExitCode.Normal;
		}
		
		public override string GetHelpText()
		{
			return "Print the directory the server is installed in";
		}
	}

	class ConfigDownloadCommand : Command
	{
		public ConfigDownloadCommand()
		{
			Keyword = "download";
			RequiredParameters = 2;
		}

		protected override ExitCode Run(IList<string> parameters)
		{
			var bytes = Server.GetComponent<ITGConfig>().ReadText(parameters[0], parameters.Count > 2 && parameters[2].ToLower() == "--repo", out string error);
			if(bytes == null)
			{
				OutputProc("Error: " + error);
				return ExitCode.ServerError;
			}

			try
			{
				File.WriteAllText(parameters[1], bytes);
			}
			catch (Exception e)
			{
				OutputProc("Error: " + e.ToString());
			}

			return ExitCode.Normal;
		}
		public override string GetArgumentString()
		{
			return "<source static file> <out file> [--repo]";
		}
		public override string GetHelpText()
		{
			return "Downloads the specified file from the static tree and writes it to out file. --repo will fetch it from the repository instead of the Static directory";
		}
	}
	
	class ConfigUploadCommand : Command
	{
		public ConfigUploadCommand()
		{
			Keyword = "upload";
			RequiredParameters = 2;
		}

		protected override ExitCode Run(IList<string> parameters)
		{
			try
			{
				var res = Server.GetComponent<ITGConfig>().WriteText(parameters[0], File.ReadAllText(parameters[1]));
				if (res != null)
				{
					OutputProc("Error: " + res);
					return ExitCode.ServerError;
				}
			}
			catch (Exception e)
			{
				OutputProc("Error: " + e.ToString());
			}
			return ExitCode.Normal;
		}

		public override string GetArgumentString()
		{
			return "<destination statoc file> <source file> [--repo]";
		}

		public override string GetHelpText()
		{
			return "Uploads the specified file to the static tree from source file";
		}
	}
}
