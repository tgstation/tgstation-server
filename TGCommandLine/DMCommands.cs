using System;
using System.Collections.Generic;
using System.Threading;
using TGServiceInterface;

namespace TGCommandLine
{
	class DMCommand : RootCommand
	{
		public DMCommand()
		{
			Keyword = "dm";
			Children = new Command[] { new DMCompileCommand(), new DMInitializeCommand(), new DMStatusCommand(), new DMSetProjectNameCommand(), new DMCancelCommand() };
		}
		public override string GetHelpText()
		{
			return "Manage compiling the server";
		}
	}

	class DMCompileCommand : Command
	{
		public DMCompileCommand()
		{
			Keyword = "compile";
		}

		public override ExitCode Run(IList<string> parameters)
		{
			var DM = Server.GetComponent<ITGCompiler>();
			var stat = DM.GetStatus();
			if (stat != TGCompilerStatus.Initialized)
			{
				OutputProc("Error: Compiler is " + ((stat == TGCompilerStatus.Uninitialized) ? "unintialized!" : "busy with another task!"));
				return ExitCode.ServerError;
			}

			if (Server.GetComponent<ITGByond>().GetVersion(TGByondVersion.Installed) == null)
			{
				Console.Write("Error: BYOND is not installed!");
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
			if (parameters.Count > 0 && parameters[0] == "--wait")
			{
				do
				{
					Thread.Sleep(1000);
				} while (DM.GetStatus() == TGCompilerStatus.Compiling);
				var res = DM.CompileError();
				OutputProc(res ?? "Compilation successful");
				if (res != null)
					return ExitCode.ServerError;
			}
			return ExitCode.Normal;
		}

		public override string GetArgumentString()
		{
			return "[--wait]";
		}

		public override string GetHelpText()
		{
			return "Starts a compile/update job optionally waiting for completion";
		}
	}
	class DMStatusCommand : Command
	{
		public DMStatusCommand()
		{
			Keyword = "status";
		}

		void ShowError()
		{
			var error = Server.GetComponent<ITGCompiler>().CompileError();
			if (error != null)
				OutputProc("Last error: " + error);
		}

		public override ExitCode Run(IList<string> parameters)
		{
			var DM = Server.GetComponent<ITGCompiler>();
			OutputProc(String.Format("Target Project: /{0}.dme", DM.ProjectName()));
			Console.Write("Compilier is currently: ");
			switch (DM.GetStatus())
			{
				case TGCompilerStatus.Compiling:
					OutputProc("Compiling...");
					break;
				case TGCompilerStatus.Initialized:
					OutputProc("Idle");
					ShowError();
					break;
				case TGCompilerStatus.Initializing:
					OutputProc("Setting up...");
					break;
				case TGCompilerStatus.Uninitialized:
					OutputProc("Uninitialized");
					ShowError();
					break;
				default:
					OutputProc("Seizing the means of production (This is an error).");
					return ExitCode.ServerError;
			}		
			return ExitCode.Normal;
		}

		public override string GetHelpText()
		{
			return "Get the current status of the compiler";
		}
	}

	class DMSetProjectNameCommand : Command
	{
		public DMSetProjectNameCommand()
		{
			Keyword = "project-name";
			RequiredParameters = 1;
		}
		public override string GetArgumentString()
		{
			return "<path>";
		}

		public override string GetHelpText()
		{
			return "Set the relative path of the .dme/.dmb to compile/run";
		}

		public override ExitCode Run(IList<string> parameters)
		{
			Server.GetComponent<ITGCompiler>().SetProjectName(parameters[0]);
			return ExitCode.Normal;
		}
	}

	class DMInitializeCommand : Command
	{
		public DMInitializeCommand()
		{
			Keyword = "initialize";
		}

		public override string GetArgumentString()
		{
			return "[--wait]";
		}

		public override string GetHelpText()
		{
			return "Starts an initialization job optionally waiting for completion";
		}

		public override ExitCode Run(IList<string> parameters)
		{
			var DM = Server.GetComponent<ITGCompiler>();
			var stat = DM.GetStatus();
			if (stat == TGCompilerStatus.Compiling || stat == TGCompilerStatus.Initializing)
			{
				OutputProc("Error: Compiler is " + ((stat == TGCompilerStatus.Initializing) ? "already initialized!" : " already running!"));
				return ExitCode.ServerError;
			}
			if (!DM.Initialize())
			{
				OutputProc("Error: Unable to start initialization!");
				var err = DM.CompileError();
				if (err != null)
					OutputProc(err);
				return ExitCode.ServerError;
			}
			OutputProc("Initialize job started");
			if (parameters.Count > 0 && parameters[0] == "--wait")
			{
				do
				{
					Thread.Sleep(1000);
				} while (DM.GetStatus() == TGCompilerStatus.Initializing);
				var res = DM.CompileError();
				OutputProc(res ?? "Initialization successful");
				if (res != null)
					return ExitCode.ServerError;
			}
			return ExitCode.Normal;
		}
	}
	
	class DMCancelCommand : Command
	{
		public DMCancelCommand()
		{
			Keyword = "cancel";
		}

		public override ExitCode Run(IList<string> parameters)
		{
			var res = Server.GetComponent<ITGCompiler>().Cancel();
			OutputProc(res ?? "Success!");
			return ExitCode.Normal;	//because failing cancellation implys it's already cancelled
		}

		public override string GetHelpText()
		{
			return "Cancels the current compilation job";
		}
	}
}
