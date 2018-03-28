﻿using System;
using System.Collections.Generic;
using System.IO;
using TGS.Interface;

namespace TGS.CommandLine
{
	class ConfigCommand : RootCommand
	{
		public ConfigCommand()
		{
			Keyword = "config";
			Children = new Command[] { new ConfigDeleteCommand(), new ConfigServerDirectoryCommand(), new ConfigDownloadCommand(), new ConfigUploadCommand(), new ConfigListCommand() };
		}
		public override string GetHelpText()
		{
			return "Manage settings";
		}
	}
	class ConfigDeleteCommand : ConsoleCommand
	{
		public ConfigDeleteCommand()
		{
			Keyword = "delete";
			RequiredParameters = 1;
		}

		protected override ExitCode Run(IList<string> parameters)
		{
			var res = Instance.StaticFiles.DeleteFile(parameters[0], out bool unauthorized);
			if (res != null)
			{
				OutputProc(res);
				return ExitCode.ServerError;
			}
			return ExitCode.Normal;
		}
		public override string GetArgumentString()
		{
			return "<static file>";
		}
		public override string GetHelpText()
		{
			return "Deletes the specified file from the static tree.";
		}
	}

	class ConfigListCommand : ConsoleCommand
	{
		public ConfigListCommand()
		{
			Keyword = "list";
		}

		public override string GetHelpText()
		{
			return "Lists the contents of the server's static directory, optionally specifying a subdirectory";
		}

		public override string GetArgumentString()
		{
			return "[path to subdirectory]";
		}
		protected override ExitCode Run(IList<string> parameters)
		{
			var list = Instance.StaticFiles.ListStaticDirectory(parameters.Count > 0 ? parameters[0] : null, out string error, out bool unauthorized);
			if(list == null)
			{
				OutputProc(error);
				return ExitCode.ServerError;
			}
			if (list.Count == 0)
				OutputProc("The static directory is empty!");
			else
				foreach (var I in list)
					OutputProc(I);
			return ExitCode.Normal;
		}
	}

	class ConfigServerDirectoryCommand : ConsoleCommand
	{
		public ConfigServerDirectoryCommand()
		{
			Keyword = "server-dir";
		}

		protected override ExitCode Run(IList<string> parameters)
		{
			OutputProc(Instance.ServerDirectory());
			return ExitCode.Normal;
		}
		
		public override string GetHelpText()
		{
			return "Print the directory the server is installed in";
		}
	}

	class ConfigDownloadCommand : ConsoleCommand
	{
		public ConfigDownloadCommand()
		{
			Keyword = "download";
			RequiredParameters = 2;
		}

		protected override ExitCode Run(IList<string> parameters)
		{
			var bytes = Instance.StaticFiles.ReadText(parameters[0], parameters.Count > 2 && parameters[2].ToLower() == "--repo", out string error, out bool unauthorized);
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
	
	class ConfigUploadCommand : ConsoleCommand
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
				var res = Instance.StaticFiles.WriteText(parameters[0], File.ReadAllText(parameters[1]), out bool unauthorized);
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
