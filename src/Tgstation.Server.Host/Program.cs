using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Tgstation.Server.Host.Common;
using Tgstation.Server.Host.Core;
using Tgstation.Server.Host.Properties;
using Tgstation.Server.Host.System;

namespace Tgstation.Server.Host
{
	/// <summary>
	/// Entrypoint for the <see cref="Process"/>.
	/// </summary>
	sealed class Program
	{
		/// <summary>
		/// The expected host watchdog <see cref="Version"/>.
		/// </summary>
		internal static Version HostWatchdogVersion => Version.Parse(MasterVersionsAttribute.Instance.RawHostWatchdogVersion);

		/// <summary>
		/// The <see cref="IServerFactory"/> to use.
		/// </summary>
		internal IServerFactory ServerFactory { get; set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="Program"/> class.
		/// </summary>
		public Program()
		{
			ServerFactory = Application.CreateDefaultServerFactory();
		}

		/// <summary>
		/// Entrypoint for the <see cref="Program"/>.
		/// </summary>
		/// <param name="args">The command line arguments.</param>
		/// <returns>A <see cref="Task"/> resulting in the <see cref="global::System.Diagnostics.Process.ExitCode"/>.</returns>
		public static async Task<int> Main(string[] args)
		{
			// first arg is 100% always the update path, starting it otherwise is solely for debugging purposes
			var updatePath = TopLevelArgsParse(ref args);

			var program = new Program();
			return (int)await program.Main(args, updatePath);
		}

		/// <summary>
		/// Parse <see cref="Program"/> top level <paramref name="args"/>.
		/// </summary>
		/// <param name="args">The arguments <see cref="Array"/> which may be changed.</param>
		/// <returns>The update path for the server, if present.</returns>
		static string? TopLevelArgsParse(ref string[] args)
		{
			if (args.Length == 0)
				return null;

			var potentialUpdatePath = args[0];
			if (potentialUpdatePath.Equals("cli", StringComparison.OrdinalIgnoreCase))
				return null;

			var listArgs = new List<string>(args);
			listArgs.RemoveAt(0);

			// second arg should be host watchdog version
			if (listArgs.Count > 0)
			{
				var expectedHostWatchdogVersion = HostWatchdogVersion;
				if (Version.TryParse(listArgs.First(), out var actualHostWatchdogVersion)
					&& actualHostWatchdogVersion.Major != expectedHostWatchdogVersion.Major)
					throw new InvalidOperationException(
						$"Incompatible host watchdog version ({actualHostWatchdogVersion}) for server ({expectedHostWatchdogVersion})! A major update was released and a full restart will be required. Please manually offline your servers!");
			}

			if (listArgs.Remove("--attach-debugger"))
				Debugger.Launch();

			args = [.. listArgs];
			return potentialUpdatePath;
		}

		/// <summary>
		/// Executes the <see cref="Program"/>.
		/// </summary>
		/// <param name="args">The command line arguments, minus the <paramref name="updatePath"/>.</param>
		/// <param name="updatePath">The path to extract server updates to be applied to.</param>
		/// <returns>A <see cref="ValueTask"/> resulting in the <see cref="HostExitCode"/>.</returns>
		internal async ValueTask<HostExitCode> Main(string[] args, string? updatePath)
		{
			try
			{
				using var shutdownNotifier = new ProgramShutdownTokenSource();
				var cancellationToken = shutdownNotifier.Token;
				IServer? server;
				try
				{
					server = await ServerFactory.CreateServer(
						args,
						updatePath,
						cancellationToken);
				}
				catch (OperationCanceledException)
				{
					// Console cancelled
					return HostExitCode.CompleteExecution;
				}

				if (server == null)
					return HostExitCode.CompleteExecution;

				await server.Run(cancellationToken);

				return server.RestartRequested
					? HostExitCode.RestartRequested
					: HostExitCode.CompleteExecution;
			}
			catch (Exception e)
			{
				if (updatePath != null)
				{
					// DCT: None available, operation should always run
					await ServerFactory.IOManager.WriteAllBytes(updatePath, Encoding.UTF8.GetBytes(e.ToString()), CancellationToken.None);
					return HostExitCode.Error;
				}

				// If you hit an exception debug break on this line it caused the application to crash
				throw;
			}
		}
	}
}
