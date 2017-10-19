using System;
using System.Collections.Generic;
using System.Threading;

namespace TGServiceInterface
{
	/// <summary>
	/// Exit codes for <see cref="Command"/>s
	/// </summary>
	public enum ExitCode
	{
		/// <summary>
		/// The <see cref="Command"/> ran successfully
		/// </summary>
		Normal = 0,
		/// <summary>
		/// The connection to the service was interrupted during the <see cref="Command"/>
		/// </summary>
		ConnectionError = 1,
		/// <summary>
		/// Invalid parameters for <see cref="Command"/>
		/// </summary>
		BadCommand = 2,
		/// <summary>
		/// The command failed due to conditions on the service
		/// </summary>
		ServerError = 3,
	}

	/// <summary>
	/// Helper for creating a text <see cref="Command"/> tree
	/// </summary>
	public abstract class Command
	{
		/// <summary>
		/// Proc that will show a message to the <see cref="Command"/> invoker. Do not call directly, use <see cref="OutputProc(string)"/> instead
		/// </summary>
		public static ThreadLocal<Action<string>> OutputProcVar = new ThreadLocal<Action<string>>();
		/// <summary>
		/// Write output to the <see cref="Command"/> invoker
		/// </summary>
		/// <param name="message">The output to display</param>
		protected static void OutputProc(string message)
		{
			OutputProcVar.Value(message);
		}
		/// <summary>
		/// The text that invokes this <see cref="Command"/>. Set in constructor
		/// </summary>
		public string Keyword { get; protected set; }
		/// <summary>
		/// The number of parameters this <see cref="Command"/> requires. Set in Constructor
		/// </summary>
		public int RequiredParameters { get; protected set; }
		/// <summary>
		/// Caller of <see cref="Run(IList{string})"/>, can be used to modify the root behaviour of the <see cref="Command"/>
		/// </summary>
		/// <param name="parameters">List of parameters passed to the <see cref="Command"/></param>
		/// <returns>An <see cref="ExitCode"/> describing the execution of the <see cref="Command"/></returns>
		public virtual ExitCode DoRun(IList<string> parameters)
		{
			return Run(parameters);
		}
		/// <summary>
		/// Override to do the actions of the <see cref="Command"/> 
		/// </summary>
		/// <param name="parameters">List of <see cref="string"/> parameters passed to the <see cref="Command"/>. Guaranteed to have at least <see cref="RequiredParameters"/> non-empty/whitespace entries</param>
		/// <returns>An <see cref="ExitCode"/> describing the execution of the <see cref="Command"/></returns>
		protected abstract ExitCode Run(IList<string> parameters);
		/// <summary>
		/// Prints usage text of the <see cref="Command"/> to the invoker
		/// </summary>
		public virtual void PrintHelp()
		{
			var argstr = GetArgumentString();
			OutputProc(String.Format("{0} {1}- {2}", Keyword, argstr.Length > 0 ? argstr + " " : "", GetHelpText()));
		}
		/// <summary>
		/// Override to add argument text to the <see cref="Command"/>
		/// Format is &lt;required&gt; &lt;arguments&gt; [optional] [arguments]
		/// </summary>
		/// <returns>Formatted argument text for the <see cref="Command"/></returns>
		public virtual string GetArgumentString()
		{
			return "";
		}
		/// <summary>
		/// Override to add usage text to the <see cref="Command"/>
		/// </summary>
		/// <returns>Formatted usage text for the <see cref="Command"/></returns>
		public abstract string GetHelpText();
	}
}
