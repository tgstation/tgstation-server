﻿using System;
using System.Collections.Generic;
using System.Linq;
using TGS.Interface;
using TGS.Interface.Components;

namespace TGS.CommandLine
{
	class Program
	{
		static bool interactive = false, saidSrvVersion = false;
		static IServerInterface currentInterface;
		static Command.ExitCode RunCommandLine(IList<string> argsAsList)
		{
			//first lookup the connection string
			bool badConnectionString = false;
			for (var I = 0; I < argsAsList.Count - 1; ++I) {
				var lowerarg = argsAsList[I].ToLower();
				if (lowerarg == "-c" || lowerarg == "--connect")
				{
					var connectionString = argsAsList[I + 1];
					var splits = connectionString.Split('@');
					var userpass = splits[0].Split(':');
					if (splits.Length != 2 || userpass.Length != 2)
					{
						badConnectionString = true;
						break;
					}
					var addrport = splits[1].Split(':');
					if (addrport.Length != 2)
					{
						badConnectionString = true;
						break;
					}
					var username = userpass[0];
					var password = userpass[1];
					var address = addrport[0];
					ushort port;
					try
					{
						port = Convert.ToUInt16(addrport[1]);
					}
					catch
					{
						badConnectionString = true;
						break;
					}
					if(String.IsNullOrWhiteSpace(username) || String.IsNullOrWhiteSpace(password) || String.IsNullOrWhiteSpace(address))
					{
						badConnectionString = true;
						break;
					}
					argsAsList.RemoveAt(I);
					argsAsList.RemoveAt(I);
					ReplaceInterface(new ServerInterface(new RemoteLoginInfo(address, port, username, password)));
					break;
				}
			}

			if (badConnectionString)
			{
				Console.WriteLine("Remote connection usage: <-c/--connect> username:password@address:port");
				return Command.ExitCode.BadCommand;
			}

			var res = currentInterface.ConnectionStatus(out string error);
			if (!res.HasFlag(ConnectivityLevel.Connected))
			{
				Console.WriteLine("Unable to connect to service: " + error);
				Console.WriteLine("Remote connection usage: <-c/--connect> username:password@address:port");
				return Command.ExitCode.ConnectionError;
			}

			if (!res.HasFlag(ConnectivityLevel.Authenticated))
			{
				Console.WriteLine("Authentication error: Username/password/windows identity is not authorized!");
				return Command.ExitCode.ConnectionError;
			}

			if (!SentVMMWarning && currentInterface.VersionMismatch(out error))
			{
				SentVMMWarning = true;
				Console.WriteLine(error);
			}
			else if (interactive && !saidSrvVersion)
			{
				Console.WriteLine("Connectd to service version: " + currentInterface.GetServiceComponent<ITGLanding>().Version());
				saidSrvVersion = true;
			}

			try
			{
				return new CLICommand(currentInterface).DoRun(argsAsList);
			}
			catch (Exception e)
			{
				Console.WriteLine("Error: " + e.ToString());
				return Command.ExitCode.ConnectionError;
			};
		}

		static void ReplaceInterface(IServerInterface I)
		{
			currentInterface = I;
			ConsoleCommand.Interface = I;
			InstanceRootCommand.currentInterface = I;
			saidSrvVersion = false;
		}

		public static string ReadLineSecure()
		{
			string result = "";
			while (true)
			{
				ConsoleKeyInfo i = Console.ReadKey(true);
				if (i.Key == ConsoleKey.Enter)
				{
					break;
				}
				else if (i.Key == ConsoleKey.Backspace)
				{
					if (result.Length > 0)
					{
						result = result.Substring(0, result.Length - 1);
						Console.Write("\b \b");
					}
				}
				else
				{
					result += i.KeyChar;
					Console.Write("*");
				}
			}
			Console.WriteLine();
			return result;
		}
		static bool SentVMMWarning = false;
		static string AcceptedBadCert;
		static bool BadCertificateInteractive(string message)
		{
			if (AcceptedBadCert == message)
				return true;
			Console.WriteLine(message);
			Console.Write("Do you wish to continue? NOT RECCOMENDED! (y/N): ");
			var result = Console.ReadLine().Trim().ToLower();
			if (result == "y" || result == "yes")
			{
				AcceptedBadCert = message;
				return true;
			}
			return false;
		}

		/// <summary>
		/// Tries to set <see cref="currentInterface"/>'s <see cref="ITGInstance"/> to <paramref name="instanceName"/>, outputting appropriate messages
		/// </summary>
		/// <param name="instanceName">The name of the <see cref="ITGInstance"/> to test</param>
		/// <param name="silentSuccess">If <see langword="true"/>, does not output on success</param>
		/// <returns><see langword="true"/> if a <see cref="ConnectivityLevel.Authenticated"/> was achieved with <see cref="IServerInterface.ConnectToInstance(string, bool)"/>, <see langword="false"/> otherwise</returns>
		static bool CheckInstanceConnectivity(string instanceName, bool silentSuccess)
		{
			var res = currentInterface.ConnectToInstance(instanceName);
			if (!res.HasFlag(ConnectivityLevel.Connected))
				Console.WriteLine("Unable to connect to instance! Does it exist?");
			else if (!res.HasFlag(ConnectivityLevel.Authenticated))
				Console.WriteLine("The current user is not authorized to use this instance!");
			else
			{
				if(!silentSuccess)
					Console.WriteLine("Successfully conected to instance!");
				return true;
			}
			return false;
		}

		static int Main(string[] args)
		{
			using (var si = new ServerInterface())
			{
				si.ConnectToInstance("TG Station Server", true);
				var prs = si.GetComponent<ITGRepository>().MergedPullRequests().Result;
				return 0;
			}

			ReplaceInterface(new ServerInterface());
			Command.OutputProcVar.Value = Console.WriteLine;
			if (args.Length != 0)
			{
				var argsAsList = new List<string>(args);
				for (var I = 0; I < argsAsList.Count - 1; ++I)
				{
					if (argsAsList[I].ToLower() == "--instance")
					{
						if (!CheckInstanceConnectivity(args[I + 1], true))
							return (int)Command.ExitCode.ConnectionError;
						argsAsList.RemoveRange(I, 2);
						break;
					}
					else if (argsAsList[I].ToLower() == "--disable-ssl-verification")    //im just not even going to document this because i hate it so much
					{
						argsAsList.RemoveAt(I);
						--I;
						ServerInterface.SetBadCertificateHandler(_ => false);
					}
				}
				return (int)RunCommandLine(argsAsList);
			}
			//interactive mode
			ServerInterface.SetBadCertificateHandler(BadCertificateInteractive);
			Console.WriteLine("Type 'instance' to connect to a server instance");
			Console.WriteLine("Type 'remote' to connect to a remote service");
			while (true)
			{
				Console.Write("Enter command: ");
				var NextCommand = Console.ReadLine();
				switch (NextCommand.ToLower())
				{
					case "instance":
						Console.Write("Enter instance name: ");
						CheckInstanceConnectivity(Console.ReadLine(), false);
						break;
					case "remote":
						SentVMMWarning = false;
						Console.Write("Enter server address: ");
						var address = Console.ReadLine();
						Console.Write("Enter server port: ");
						ushort port;
						try{
							port = Convert.ToUInt16(Console.ReadLine());
						}
						catch
						{
							Console.WriteLine("Error: Bad port!");
							break;
						}
						Console.Write("Enter username: ");
						var username = Console.ReadLine();
						Console.Write("Enter password: ");
						var password = ReadLineSecure();
						ReplaceInterface(new ServerInterface(new RemoteLoginInfo(address, port, username, password)));
						var res = currentInterface.ConnectionStatus(out string error);
						if (!res.HasFlag(ConnectivityLevel.Connected))
						{
							Console.WriteLine("Unable to connect: " + error);
							ReplaceInterface(new ServerInterface());
						}
						else if (!res.HasFlag(ConnectivityLevel.Authenticated))
						{
							Console.WriteLine("Authentication error: Username/password/windows identity is not authorized! Returning to local mode...");
							ReplaceInterface(new ServerInterface());
						}
						else
						{
							Console.WriteLine("Connected remotely");
							if (currentInterface.VersionMismatch(out error))
							{
								SentVMMWarning = true;
								Console.WriteLine(error);
							}
							Console.WriteLine("Type 'disconnect' to return to local mode");
						}
						break;
					case "disconnect":
						SentVMMWarning = false;
						ReplaceInterface(new ServerInterface());
						Console.WriteLine("Switch to local mode");
						break;
					case "quit":
					case "exit":
						return (int)Command.ExitCode.Normal;
					case "debug-upgrade":
						currentInterface.GetServiceComponent<ITGSService>().PrepareForUpdate();
						return (int)Command.ExitCode.Normal;
					default:
						//linq voodoo to get quoted strings
						var formattedCommand = NextCommand.Split('"')
										   .Select((element, index) => index % 2 == 0  // If even index
										   ? element.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)  // Split the item
										   : new string[] { element })  // Keep the entire item
										   .SelectMany(element => element).ToList();

						formattedCommand = formattedCommand.Select(x => x.Trim()).ToList();
						formattedCommand.Remove("");
						RunCommandLine(formattedCommand);
						break;
				}
			}
		}
	}
}
