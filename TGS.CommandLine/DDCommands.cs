﻿using System;
using System.Collections.Generic;
using TGS.Interface;

namespace TGS.CommandLine
{
	class DDCommand : RootCommand
	{
		public DDCommand()
		{
			Keyword = "dd";
			Children = new Command[] { new DDStartCommand(), new DDStopCommand(), new DDRestartCommand(), new DDStatusCommand(), new DDAutostartCommand(), new DDPortCommand(), new DDSecurityCommand(), new DDWorldAnnounceCommand(), new DDWebclientCommand() };
		}
		public override string GetHelpText()
		{
			return "Manage DreamDaemon";
		}
	}

	class DDWorldAnnounceCommand : ConsoleCommand
	{
		public DDWorldAnnounceCommand()
		{
			Keyword = "announce";
			RequiredParameters = 1;
		}

		public override string GetHelpText()
		{
			return "Sends a message all players on the server";
		}

		public override string GetArgumentString()
		{
			return "<message>";
		}

		protected override ExitCode Run(IList<string> parameters)
		{
			Instance.Interop.WorldAnnounce(String.Join(" ", parameters));
			return ExitCode.Normal;
		}
	}

	class DDStartCommand : ConsoleCommand
	{
		public DDStartCommand()
		{
			Keyword = "start";
		}

		public override string GetHelpText()
		{
			return "Starts the server and watchdog";
		}

		protected override ExitCode Run(IList<string> parameters)
		{
			var res = Instance.DreamDaemon.Start();
			OutputProc(res ?? "Success!");
			return res == null ? ExitCode.Normal : ExitCode.ServerError;
		}
	}

	class DDStopCommand : ConsoleCommand
	{
		public DDStopCommand()
		{
			Keyword = "stop";
		}
		public override string GetArgumentString()
		{
			return "[--graceful]";
		}

		public override string GetHelpText()
		{
			return "Stops the server and watchdog optionally waiting for the current round to end";
		}

		protected override ExitCode Run(IList<string> parameters)
		{
			var DD = Instance.DreamDaemon;
			if (parameters.Count > 0 && parameters[0].ToLower() == "--graceful")
			{
				if (DD.DaemonStatus() != DreamDaemonStatus.Online)
				{
					OutputProc("Error: The game is not currently running!");
					return ExitCode.ServerError;
				}
				DD.RequestStop();
				return ExitCode.Normal;
			}
			var res = DD.Stop();
			OutputProc(res ?? "Success!");
			return res == null ? ExitCode.Normal : ExitCode.ServerError;
		}
	}
	class DDRestartCommand : ConsoleCommand
	{
		public DDRestartCommand()
		{
			Keyword = "restart";
		}

		public override string GetArgumentString()
		{
			return "[--graceful]";
		}
		protected override ExitCode Run(IList<string> parameters)
		{
			var DD = Instance.DreamDaemon;
			if (parameters.Count > 0 && parameters[0].ToLower() == "--graceful")
			{
				if (DD.DaemonStatus() != DreamDaemonStatus.Online)
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
	class DDStatusCommand : ConsoleCommand
	{
		public DDStatusCommand()
		{
			Keyword = "status";
		}

		public override string GetHelpText()
		{
			return "Gets the current status of the watchdog and server";
		}

		protected override ExitCode Run(IList<string> parameters)
		{
			var DD = Instance.DreamDaemon;
			OutputProc(DD.StatusString(true));
			if (DD.ShutdownInProgress())
				OutputProc("The server will shutdown once the current round completes.");
			return ExitCode.Normal;
		}
	}

	class DDAutostartCommand : ConsoleCommand
	{
		public DDAutostartCommand()
		{
			Keyword = "autostart";
			RequiredParameters = 1;
		}

		protected override ExitCode Run(IList<string> parameters)
		{
			var DD = Instance.DreamDaemon;
			switch (parameters[0].ToLower())
			{
				case "on":
					DD.SetAutostart(true);
					break;
				case "off":
					DD.SetAutostart(false);
					break;
				case "check":
					OutputProc("Autostart is: " + (DD.Autostart() ? "On" : "Off"));
					break;
				default:
					OutputProc("Invalid parameter: " + parameters[0]);
					return ExitCode.BadCommand;
			}
			return ExitCode.Normal;
		}

		public override string GetArgumentString()
		{
			return "<on|off|check>";
		}
		public override string GetHelpText()
		{
			return "Change or check autostarting of the game server with the service";
		}
	}
	class DDWebclientCommand : ConsoleCommand
	{
		public DDWebclientCommand()
		{
			Keyword = "webclient";
			RequiredParameters = 1;
		}

		protected override ExitCode Run(IList<string> parameters)
		{
			var DD = Instance.DreamDaemon;
			switch (parameters[0].ToLower())
			{
				case "on":
					DD.SetWebclient(true);
					break;
				case "off":
					DD.SetWebclient(false);
					break;
				case "check":
					OutputProc("Webclient is: " + (DD.Webclient() ? "Enabled" : "Disabled"));
					break;
				default:
					OutputProc("Invalid parameter: " + parameters[0]);
					return ExitCode.BadCommand;
			}
			return ExitCode.Normal;
		}

		public override string GetArgumentString()
		{
			return "<on|off|check>";
		}
		public override string GetHelpText()
		{
			return "Change or check if the BYOND webclient is enabled for the game server";
		}
	}

	class DDPortCommand : ConsoleCommand
	{
		public DDPortCommand()
		{
			Keyword = "set-port";
			RequiredParameters = 1;
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

			Instance.DreamDaemon.SetPort(port);
			return ExitCode.Normal;
		}

		public override string GetArgumentString()
		{
			return "<number>";
		}

		public override string GetHelpText()
		{
			return "Sets the port DreamDaemon will open the server on. Requires a server restart to apply and queues a graceful one up";
		}
	}

	class DDSecurityCommand : ConsoleCommand
	{
		public DDSecurityCommand()
		{
			Keyword = "set-security";
			RequiredParameters = 1;
		}

		protected override ExitCode Run(IList<string> parameters)
		{
			DreamDaemonSecurity sec;
			switch (parameters[0].ToLower())
			{
				case "safe":
					sec = DreamDaemonSecurity.Safe;
					break;
				case "ultra":
				case "ultrasafe":
					sec = DreamDaemonSecurity.Ultrasafe;
					break;
				case "trust":
				case "trusted":
					sec = DreamDaemonSecurity.Trusted;
					break;
				default:
					OutputProc("Invalid security word!");
					return ExitCode.BadCommand;
			}
			Instance.DreamDaemon.SetSecurityLevel(sec);
			return ExitCode.Normal;
		}

		public override string GetArgumentString()
		{
			return "<safe|ultrasafe|trusted>";
		}

		public override string GetHelpText()
		{
			return "Sets the visibility option for the DreamDaemon world";
		}
	}
}
