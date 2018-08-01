using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Tgstation.Server.Host
{
	/// <summary>
	/// Entrypoint for the <see cref="System.Diagnostics.Process"/>
	/// </summary>
    static class Program
	{
		/// <summary>
		/// The <see cref="IServerFactory"/> to use
		/// </summary>
		internal static IServerFactory serverFactory = new ServerFactory();

		/// <summary>
		/// Entrypoint for the <see cref="Program"/>
		/// </summary>
		/// <param name="args">The command line arguments</param>
		/// <returns>The <see cref="System.Diagnostics.Process.ExitCode"/></returns>
		public static async Task<int> Main(string[] args)
		{
			var listArgs = new List<string>(args);
			//first arg is 100% always the update path
			string updatePath;
			if (listArgs.Count > 0)
			{
				updatePath = listArgs[0];
				listArgs.RemoveAt(0);
#if DEBUG
				System.Diagnostics.Debugger.Launch();
#endif
			}
			else
				updatePath = null;
			try
			{
				using (var cts = new CancellationTokenSource())
				{
					AppDomain.CurrentDomain.ProcessExit += (a, b) => cts.Cancel();
					Console.CancelKeyPress += (a, b) =>
					{
						b.Cancel = true;
						cts.Cancel();
					};
					using (var server = serverFactory.CreateServer(listArgs.ToArray(), updatePath))
						await server.RunAsync(cts.Token).ConfigureAwait(false);
				}
			}
			catch (OperationCanceledException) { }
			catch (Exception e)
			{
				if (updatePath != null)
				{
					File.WriteAllText(updatePath, e.ToString());
					return 1;
				}
				throw;
			}
			return 0;
		}
	}
}
