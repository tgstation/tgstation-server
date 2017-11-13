using System;

namespace TGS.Server.Console
{
	/// <summary>
	/// Console runner for a <see cref="Server"/>
	/// </summary>
	sealed class Program : ILogger
	{
		/// <summary>
		/// Entry point to the <see cref="Program"/>
		/// </summary>
		/// <param name="args"></param>
		static void Main(string[] args) => new Program(args);

		/// <summary>
		/// Construct and run a <see cref="Program"/>
		/// </summary>
		/// <param name="args">Command line arguments</param>
		Program(string[] args)
		{
			System.Console.WriteLine("Starting server...");
			var server = new Server(args, this);	//no using to avoid including more references
			try
			{
				System.Console.WriteLine("Server started!");
				System.Console.Write("Press any key to exit...");
				System.Console.ReadKey();
			}
			finally
			{
				server.Dispose();
			}
		}

		/// <inheritdoc />
		public void WriteAccess(string username, bool authSuccess, byte loggingID)
		{
			System.Console.WriteLine(String.Format("[{0}]: {1}: Authentication {3} from {2}", DateTime.UtcNow.ToString(), EventID.Authentication + loggingID, username, authSuccess ? "success" : "fail"));
		}

		/// <inheritdoc />
		public void WriteError(string message, EventID id, byte loggingID)
		{
			System.Console.WriteLine(String.Format("[{0}]: {1}: ERROR: {2}", DateTime.UtcNow.ToString(), id + loggingID, message));
		}

		/// <inheritdoc />
		public void WriteInfo(string message, EventID id, byte loggingID)
		{
			System.Console.WriteLine(String.Format("[{0}]: {1}: Warning: {2}", DateTime.UtcNow.ToString(), id + loggingID, message));
		}

		/// <inheritdoc />
		public void WriteWarning(string message, EventID id, byte loggingID)
		{
			System.Console.WriteLine(String.Format("[{0}]: {1}: {2}", DateTime.UtcNow.ToString(), id + loggingID, message));
		}
	}
}
