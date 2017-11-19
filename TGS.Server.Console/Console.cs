using System;
using System.Linq;
using System.Threading.Tasks;

namespace TGS.Server.Console
{
	/// <summary>
	/// Console runner for a <see cref="IServer"/>
	/// </summary>
	sealed class Console : ILogger, IDisposable
	{
		/// <summary>
		/// The <see cref="IServer"/> for the <see cref="Console"/>
		/// </summary>
		readonly IServer Server;

		/// <summary>
		/// Construct a <see cref="Console"/>
		/// </summary>
		/// <param name="serverFactory">The <see cref="IServerFactory"/> for creating <see cref="Server"/></param>
		public Console(IServerFactory serverFactory)
		{
			Server = serverFactory.CreateServer(this);
		}

		/// <summary>
		/// Prompts the user to exit the <see cref="Console"/>
		/// </summary>
		Task ExitPrompt()
		{
			System.Console.WriteLine("Press any key to exit...");
			return Task.Factory.StartNew(() => System.Console.ReadKey());
		}

		/// <summary>
		/// Construct and run a <see cref="Console"/>
		/// </summary>
		/// <param name="args">Command line arguments</param>
		public void Run(string[] args)
		{
			try
			{
				System.Console.WriteLine("Starting server...");
				Server.Start(args);
				try
				{
					System.Console.WriteLine("Server started!");
					ExitPrompt();
				}
				finally
				{
					Server.Stop();
				}
			}
			catch (Exception e)
			{
				System.Console.WriteLine(String.Format("Unhandled exception: {0}", e.ToString()));
				ExitPrompt();
			}
		}

		/// <inheritdoc />
		public void WriteAccess(string username, bool authSuccess, byte loggingID)
		{
			System.Console.WriteLine(String.Format("[{0}]: {1}-{4}: Authentication {3} from {2}", DateTime.UtcNow.ToString(), EventID.Authentication, username, authSuccess ? "success" : "fail", loggingID));
		}

		/// <inheritdoc />
		public void WriteError(string message, EventID id, byte loggingID)
		{
			System.Console.WriteLine(String.Format("[{0}]: {1}-{3}: ERROR: {2}", DateTime.Now.ToString(), id, message, loggingID));
		}

		/// <inheritdoc />
		public void WriteInfo(string message, EventID id, byte loggingID)
		{
			System.Console.WriteLine(String.Format("[{0}]: {1}-{3}: {2}", DateTime.Now.ToString(), id, message, loggingID));
		}

		/// <inheritdoc />
		public void WriteWarning(string message, EventID id, byte loggingID)
		{
			System.Console.WriteLine(String.Format("[{0}]: {1}-{3}: Warning: {2}", DateTime.Now.ToString(), id, message, loggingID));
		}

		/// <summary>
		/// Disposes <see cref="activeServer"/>
		/// </summary>
		public void Dispose()
		{
			Server.Dispose();
		}
	}
}
