using System;
using System.Collections.Generic;

namespace TGServiceInterface
{
	public enum ExitCode
	{
		Normal = 0,
		ConnectionError = 1,
		BadCommand = 2,
		ServerError = 3,
	}
	public abstract class Command
	{
		public static Action<string> OutputProc;
		public string Keyword { get; protected set; }
		public Command[] Children { get; protected set; } = { };
		public int RequiredParameters { get; protected set; }
		public abstract ExitCode Run(IList<string> parameters);
		public virtual void PrintHelp()
		{
			var argstr = GetArgumentString();
			OutputProc(String.Format("{0} {1}- {2}", Keyword, argstr.Length > 0 ? argstr + " " : "", GetHelpText()));
		}
		public virtual string GetArgumentString()
		{
			return "";
		}
		public abstract string GetHelpText();
	}

	public class RootCommand : Command
	{
		protected bool RealRoot { get; set; }
		public override ExitCode Run(IList<string> parameters)
		{
			if (parameters.Count > 0)
			{
				var LocalKeyword = parameters[0].Trim().ToLower();
				parameters.RemoveAt(0);

				switch (LocalKeyword)
				{
					case "help":
					case "?":
						PrintHelp();
						return ExitCode.Normal;
					default:
						foreach (var c in Children)
							if (c.Keyword == LocalKeyword)
							{
								if(parameters.Count> 0)
								{
									var possibleHelp = parameters[0].ToLower();
									if (possibleHelp == "help" || possibleHelp == "?")
									{
										c.PrintHelp();
										return ExitCode.Normal;
									}
								}
								if (parameters.Count < c.RequiredParameters)
								{
									OutputProc("Not enough parameters!");
									return ExitCode.BadCommand;
								}
								return c.Run(parameters);
							}
						parameters.Insert(0, LocalKeyword);
						break;
				}
			}
			OutputProc(String.Format("Invalid command: {0} {1}", Keyword, String.Join(" ", parameters)));
			OutputProc(String.Format("Type '{0}?' or '{0}help' for available commands.", Keyword != null ? Keyword + " " : ""));
			return ExitCode.BadCommand;
		}
		public override void PrintHelp()
		{
			OutputProc("Available commands (type '?' or 'help' after command for more info):");
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
			for (var I = 0; I < Prefixes.Count; ++I)
			{
				var lp = Prefixes[I];
				for (; lp.Length < MaxPrefixLen + 1; lp += " ") ;
				Final.Add(lp + "- " + Postfixes[I]);
			}
			Final.Sort();
			Final.ForEach(Console.WriteLine);
		}

		public override string GetHelpText()
		{
			throw new NotImplementedException();
		}
	}
}
