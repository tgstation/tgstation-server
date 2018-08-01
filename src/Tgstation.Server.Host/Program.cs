using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Tgstation.Server.Host
{
	/// <summary>
	/// Entrypoint for the <see cref="Process"/>
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
		/// <returns>The <see cref="Process.ExitCode"/></returns>
		public static async Task<int> Main(string[] args)
		{
			var listArgs = new List<string>(args);
			//first arg is 100% always the update path, starting it otherwise is solely for debugging purposes
			string updatePath;
			if (listArgs.Count > 0)
			{
				updatePath = listArgs[0];
				listArgs.RemoveAt(0);
				if (listArgs.Remove("--attach-debugger"))
					Debugger.Launch();
			}
			else
				updatePath = null;
			try
			{
				using (var server = serverFactory.CreateServer(listArgs.ToArray(), updatePath))
				{
					try
					{
						using (var cts = new CancellationTokenSource())
						{
							void AppDomainHandler(object a, EventArgs b) => cts.Cancel();
							AppDomain.CurrentDomain.ProcessExit += AppDomainHandler;
							try
							{
								Console.CancelKeyPress += (a, b) =>
								{
									b.Cancel = true;
									cts.Cancel();
								};
								await server.RunAsync(cts.Token).ConfigureAwait(false);
							}
							finally
							{
								AppDomain.CurrentDomain.ProcessExit -= AppDomainHandler;
							}
						}
					}
					catch (OperationCanceledException) { }
					return server.RestartRequested ? 1 : 0;
				}
			}
			catch (Exception e)
			{
				if (updatePath != null)
				{
					File.WriteAllText(updatePath, e.ToString());
					return 2;
				}
				throw;
			}
		}
	}
}
