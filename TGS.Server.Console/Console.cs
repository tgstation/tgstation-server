using System;

namespace TGS.Server.Console
{
	/// <summary>
	/// Console runner for a <see cref="Server"/>
	/// </summary>
	sealed class Console : ILogger
	{
		/// <summary>
		/// Entry point to the <see cref="Console"/>
		/// </summary>
		/// <param name="args"></param>
		static void Main(string[] args) => new Console(args);

		/// <summary>
		/// Construct and run a <see cref="Console"/>
		/// </summary>
		/// <param name="args">Command line arguments</param>
		Console(string[] args)
		{
			try
			{
				System.Console.WriteLine("Starting server...");
				var server = new Server(args, this);    //no using to avoid including more references
				try
				{
					System.Console.WriteLine("Server started!");
					ExitPrompt();
				}
				finally
				{
					server.Dispose();
				}
			}
			catch (Exception e)
			{
				System.Console.WriteLine(String.Format("Unhandled exception: {0}", e.ToString()));
				ExitPrompt();
			}
		}

		void ExitPrompt()
		{
			System.Console.WriteLine("Press any key to exit...");
			System.Console.ReadKey();
		}

		/// <inheritdoc />
		public void WriteAccess(string username, bool authSuccess, byte loggingID)
		{
			System.Console.WriteLine(String.Format("[{0}]: {1}-{4}: Authentication {3} from {2}", DateTime.UtcNow.ToString(), EventID.Authentication, username, authSuccess ? "success" : "fail", loggingID));
		}

		/// <inheritdoc />
		public void WriteError(string message, EventID id, byte loggingID)
		{
			System.Console.WriteLine(String.Format("[{0}]: {1}-{3}: ERROR: {2}", DateTime.UtcNow.ToString(), id, message, loggingID));
		}

		/// <inheritdoc />
		public void WriteInfo(string message, EventID id, byte loggingID)
		{
			System.Console.WriteLine(String.Format("[{0}]: {1}-{3}: {2}", DateTime.UtcNow.ToString(), id, message, loggingID));
		}

		/// <inheritdoc />
		public void WriteWarning(string message, EventID id, byte loggingID)
		{
			System.Console.WriteLine(String.Format("[{0}]: {1}-{3}: Warning: {2}", DateTime.UtcNow.ToString(), id, message, loggingID));
		}
	}
}
