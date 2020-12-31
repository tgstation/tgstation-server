using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tgstation.Server.Host.Core;
using Tgstation.Server.Host.System;

namespace Tgstation.Server.Host
{
	/// <summary>
	/// Entrypoint for the <see cref="Process"/>
	/// </summary>
	static class Program
	{
#pragma warning disable SA1401 // Fields must be private
		/// <summary>
		/// The expected host watchdog <see cref="Version"/>.
		/// </summary>
		internal static readonly Version HostWatchdogVersion = new Version(1, 1, 0);

		/// <summary>
		/// The <see cref="IServerFactory"/> to use.
		/// </summary>
		internal static IServerFactory ServerFactory = Application.CreateDefaultServerFactory();
#pragma warning restore SA1401 // Fields must be private

		/// <summary>
		/// Entrypoint for the <see cref="Program"/>
		/// </summary>
		/// <param name="args">The command line arguments</param>
		/// <returns>The <see cref="global::System.Diagnostics.Process.ExitCode"/>.</returns>
		public static async Task<int> Main(string[] args)
		{
			// first arg is 100% always the update path, starting it otherwise is solely for debugging purposes
			var listArgs = new List<string>(args);
			string updatePath = null;
			if (listArgs.Count > 0)
			{
				updatePath = listArgs.First();
				listArgs.RemoveAt(0);

				// second arg should be host watchdog version
				if (listArgs.Count > 0
					&& Version.TryParse(listArgs.First(), out var hostWatchdogVersion)
					&& hostWatchdogVersion.Major != HostWatchdogVersion.Major)
					throw new InvalidOperationException(
						$"Incompatible host watchdog version ({hostWatchdogVersion}) for server ({HostWatchdogVersion})! A major update was released and a full restart will be required. Please manually offline your servers!");

				if (listArgs.Remove("--attach-debugger"))
					Debugger.Launch();
			}

			var updatedArgsArray = listArgs.ToArray();
			try
			{
				using var shutdownNotifier = new ProgramShutdownTokenSource();
				var cancellationToken = shutdownNotifier.Token;
				IServer server;
				try
				{
					server = await ServerFactory.CreateServer(updatedArgsArray, updatePath, cancellationToken).ConfigureAwait(false);
				}
				catch (OperationCanceledException)
				{
					// Console cancelled
					return 0;
				}

				if (server == null)
					return 0;

				try
				{
					await server.Run(cancellationToken).ConfigureAwait(false);
				}
				catch (OperationCanceledException) { }
				return server.RestartRequested ? 1 : 0;
			}
			catch (Exception e)
			{
				if (updatePath != null)
				{
					// DCT: None available, operation should always run
					await ServerFactory.IOManager.WriteAllBytes(updatePath, Encoding.UTF8.GetBytes(e.ToString()), default).ConfigureAwait(false);
					return 2;
				}

				// If you hit an exception debug break on this line it caused the application to crash
				throw;
			}
		}
	}
}
