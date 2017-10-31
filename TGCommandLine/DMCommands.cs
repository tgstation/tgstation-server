using System;
using System.Collections.Generic;
using System.Threading;
using TGServiceInterface;
using TGServiceInterface.Components;

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

	class DMCompileCommand : ConsoleCommand
	{
		public DMCompileCommand()
		{
			Keyword = "compile";
		}

		protected override ExitCode Run(IList<string> parameters)
		{
			var DM = Interface.GetComponent<ITGCompiler>();
			var stat = DM.GetStatus();
			if (stat != CompilerStatus.Initialized)
			{
				OutputProc("Error: Compiler is " + ((stat == CompilerStatus.Uninitialized) ? "unintialized!" : "busy with another task!"));
				return ExitCode.ServerError;
			}

			if (Interface.GetComponent<ITGByond>().GetVersion(ByondVersion.Installed) == null)
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
				} while (DM.GetStatus() == CompilerStatus.Compiling);
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
	class DMStatusCommand : ConsoleCommand
	{
		public DMStatusCommand()
		{
			Keyword = "status";
		}

		void ShowError()
		{
			var error = Interface.GetComponent<ITGCompiler>().CompileError();
			if (error != null)
				OutputProc("Last error: " + error);
		}

		protected override ExitCode Run(IList<string> parameters)
		{
			var DM = Interface.GetComponent<ITGCompiler>();
			OutputProc(String.Format("Target Project: /{0}.dme", DM.ProjectName()));
			Console.Write("Compilier is currently: ");
			switch (DM.GetStatus())
			{
				case CompilerStatus.Compiling:
					OutputProc("Compiling...");
					break;
				case CompilerStatus.Initialized:
					OutputProc("Idle");
					ShowError();
					break;
				case CompilerStatus.Initializing:
					OutputProc("Setting up...");
					break;
				case CompilerStatus.Uninitialized:
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

	class DMSetProjectNameCommand : ConsoleCommand
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

		protected override ExitCode Run(IList<string> parameters)
		{
			Interface.GetComponent<ITGCompiler>().SetProjectName(parameters[0]);
			return ExitCode.Normal;
		}
	}

	class DMInitializeCommand : ConsoleCommand
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

		protected override ExitCode Run(IList<string> parameters)
		{
			var DM = Interface.GetComponent<ITGCompiler>();
			var stat = DM.GetStatus();
			if (stat == CompilerStatus.Compiling || stat == CompilerStatus.Initializing)
			{
				OutputProc("Error: Compiler is " + ((stat == CompilerStatus.Initializing) ? "already initialized!" : " already running!"));
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
				} while (DM.GetStatus() == CompilerStatus.Initializing);
				var res = DM.CompileError();
				OutputProc(res ?? "Initialization successful");
				if (res != null)
					return ExitCode.ServerError;
			}
			return ExitCode.Normal;
		}
	}
	
	class DMCancelCommand : ConsoleCommand
	{
		public DMCancelCommand()
		{
			Keyword = "cancel";
		}

		protected override ExitCode Run(IList<string> parameters)
		{
			var res = Interface.GetComponent<ITGCompiler>().Cancel();
			OutputProc(res ?? "Success!");
			return ExitCode.Normal;	//because failing cancellation implys it's already cancelled
		}

		public override string GetHelpText()
		{
			return "Cancels the current compilation job";
		}
	}
}
