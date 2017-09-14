using System;
using System.Collections.Generic;
using System.Linq;
using TGServiceInterface;

namespace TGCommandLine
{

	class Program
	{
		static ExitCode RunCommandLine(IList<string> argsAsList)
		{
			var res = Server.VerifyConnection();
			if (res != null)
			{
				Console.WriteLine("Unable to connect to service: " + res);
				return ExitCode.ConnectionError;
			}

			if (!Server.Authenticate())
			{
				Console.WriteLine("Authentication error! Username/password/windows identity is not authorized!");
				return ExitCode.ConnectionError;
			}

			try
			{
				return new CLICommand().DoRun(argsAsList);
			}
			catch (Exception e)
			{
				Console.WriteLine("Error: " + e.ToString());
				return ExitCode.ConnectionError;
			};
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
			return result;
		}

		static int Main(string[] args)
		{
			Command.OutputProcVar.Value = Console.WriteLine;
			if (args.Length != 0)
				return (int)RunCommandLine(new List<string>(args));

			//interactive mode
			while (true)
			{
				Console.Write("Enter command: ");
				var NextCommand = Console.ReadLine();
				switch (NextCommand.ToLower())
				{
					case "quit":
					case "exit":
						return (int)ExitCode.Normal;
#if DEBUG
					case "debug-upgrade":
						Server.GetComponent<ITGSService>().PrepareForUpdate();
						return (int)ExitCode.Normal;
#endif
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
