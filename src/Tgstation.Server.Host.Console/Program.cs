using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using Tgstation.Server.Host.Watchdog;

namespace Tgstation.Server.Host.Console
{
	/// <summary>
	/// Contains the entrypoint for the application.
	/// </summary>
	static class Program
	{
		/// <summary>
		/// The <see cref="IWatchdogFactory"/> for the <see cref="Program"/>.
		/// </summary>
		internal static IWatchdogFactory WatchdogFactory { get; set; } = new WatchdogFactory();

		/// <summary>
		/// Entrypoint for the application.
		/// </summary>
		/// <param name="args">The arguments for the <see cref="Program"/>.</param>
		/// <returns>A <see cref="Task"/> representing the running operation.</returns>
		internal static async Task Main(string[] args)
		{
			using var loggerFactory = new LoggerFactory();
			var arguments = new List<string>(args);
			var trace = arguments.Remove("--trace-host-watchdog");
			var debug = arguments.Remove("--debug-host-watchdog");

#pragma warning disable CS0618 // Type or member is obsolete
			loggerFactory.AddConsole();
#pragma warning restore CS0618 // Type or member is obsolete

			if (trace && debug)
			{
				loggerFactory.CreateLogger(nameof(Program)).LogCritical("Please specify only 1 of --trace-host-watchdog or --debug-host-watchdog!");
				return;
			}

			using var cts = new CancellationTokenSource();
			void AppDomainHandler(object a, EventArgs b) => cts.Cancel();
			AppDomain.CurrentDomain.ProcessExit += AppDomainHandler;
			try
			{
				System.Console.CancelKeyPress += (a, b) =>
				{
					b.Cancel = true;
					cts.Cancel();
				};
				await WatchdogFactory.CreateWatchdog(loggerFactory).RunAsync(false, arguments.ToArray(), cts.Token);
			}
			finally
			{
				AppDomain.CurrentDomain.ProcessExit -= AppDomainHandler;
			}
		}
	}
}
