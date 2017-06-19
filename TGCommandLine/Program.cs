﻿using System;
using System.Collections.Generic;
using System.Linq;
using TGServiceInterface;

namespace TGCommandLine
{
	enum ExitCode
	{
		Normal = 0,
		ConnectionError = 1,
		BadCommand = 2,
		ServerError = 3,
	}

	abstract class Command
	{
		public bool RequiresInstance { get; protected set; }
		public string Keyword { get; protected set; }
		public Command[] Children { get; protected set; } = { };
		public int RequiredParameters { get; protected set; }
		public abstract ExitCode Run(IList<string> parameters);
		public Command()
		{
			RequiresInstance = true;
		}
		public virtual void PrintHelp()
		{
			var Prefixes = new List<string>();
			var Postfixes = new List<string>();
			int MaxPrefixLen = 0;
			foreach (var c in Children)
			{
				var ns = c.Keyword + " " + c.GetArgumentString();
				MaxPrefixLen = Math.Max(MaxPrefixLen, ns.Length);
				Prefixes.Add(ns);
				Postfixes.Add(c.GetHelpText());
			}

			var Final = new List<string>();
			for(var I = 0; I < Prefixes.Count; ++I)
			{
				var lp = Prefixes[I];
				for (; lp.Length < MaxPrefixLen + 1; lp += " ") ;
				Final.Add(lp + "- " + Postfixes[I]);
			}
			Final.Sort();
			Final.ForEach(Console.WriteLine);
		}
		protected virtual string GetArgumentString()
		{
			return "";
		}
		protected abstract string GetHelpText();
	}

	class Program
	{
		public static int Instance = 0;
		static ExitCode RunCommandLine(IList<string> argsAsList)
		{
			var res = Service.VerifyConnection();
			if (res != null)
			{
				Console.WriteLine("Unable to connect to service: " + res);
				return ExitCode.ConnectionError;
			}
			try
			{
				try
				{
					for (var I = 0; I < argsAsList.Count - 1; ++I)
					{
						if (argsAsList[I].ToLower() == "--instance")
						{
							Instance = Convert.ToInt32(argsAsList[I + 1]);
							if (Instance == 0 || !Service.Get().ListInstances().ContainsKey(Instance)) 
								throw new Exception();
							argsAsList.RemoveAt(I);
							argsAsList.RemoveAt(I);
						}
					}
				}
				catch
				{
					Console.WriteLine("Invalid instance id!");
				}

				return new RootCommand().Run(argsAsList);
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
					case "q":
						return (int)ExitCode.Normal;
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
						Instance = 0;
						break;
				}
			}
		}
	}
}
