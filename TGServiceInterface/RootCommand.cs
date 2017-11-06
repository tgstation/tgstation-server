using System;
using System.Collections.Generic;

namespace TGServiceInterface
{
	/// <summary>
	/// Helper for creating commands that contain sub commands
	/// </summary>
	public abstract class RootCommand : Command
	{
		/// <summary>
		/// <see cref="Command"/>s further down the tree from this one. Set in Constructor
		/// </summary>
		public Command[] Children { get; protected set; } = { };
		/// <summary>
		/// If set to <see langword="true"/> a multiline, detailed list of <see cref="Command"/>s will be printed. Otherwise a singleline list of <see cref="Command"/>s will be printed
		/// </summary>
		public static bool PrintHelpList = false;

		/// <summary>
		/// Forward parameters to commands further down the tree
		/// </summary>
		/// <param name="parameters">List of parameters passed to the <see cref="RootCommand"/></param>
		/// <returns>The result of a sub <see cref="Command"/> or an appropriate <see cref="Command.ExitCode"/> if the <see cref="RootCommand"/> handled it</returns>
		protected override ExitCode Run(IList<string> parameters)
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
								if (parameters.Count > 0)
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
								return c.DoRun(parameters);
							}
						parameters.Insert(0, LocalKeyword);
						break;
				}
			}
			OutputProc(String.Format("Invalid command! Type '{0}?' or '{0}help' for available commands.", Keyword != null ? Keyword + " " : ""));
			return ExitCode.BadCommand;
		}

		/// <inheritdoc />
		public override void PrintHelp()
		{
			var Final = new List<string>();
			if (PrintHelpList)
			{
				foreach (var c in Children)
					Final.Add(c.Keyword);
				OutputProc("Available commands (type '?' or 'help' after command for more info): " + String.Join(", ", Final));
			}
			else
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

				for (var I = 0; I < Prefixes.Count; ++I)
				{
					var lp = Prefixes[I];
					for (; lp.Length < MaxPrefixLen + 1; lp += " ") ;
					Final.Add(lp + "- " + Postfixes[I]);
				}
				Final.Sort();
				Final.ForEach(OutputProc);
			}
		}

		/// <inheritdoc />
		public override string GetHelpText()
		{
			throw new NotImplementedException();
		}
	}
}
