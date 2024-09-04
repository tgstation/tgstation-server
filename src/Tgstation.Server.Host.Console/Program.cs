using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Hosting.Systemd;
using Microsoft.Extensions.Logging;

using Tgstation.Server.Common;
using Tgstation.Server.Common.Extensions;
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
		internal static IWatchdogFactory WatchdogFactory { get; set; }

		/// <summary>
		/// Initializes static members of the <see cref="Program"/> class.
		/// </summary>
		static Program()
		{
			WatchdogFactory = new WatchdogFactory();
		}

		/// <summary>
		/// Entrypoint for the application.
		/// </summary>
		/// <param name="args">The arguments for the <see cref="Program"/>.</param>
		/// <returns>A <see cref="Task"/> representing the running operation.</returns>
		internal static async Task<int> Main(string[] args)
		{
			System.Console.Title = $"{Constants.CanonicalPackageName} Host Watchdog v{Assembly.GetExecutingAssembly().GetName().Version?.Semver()}";

			var arguments = new List<string>(args);
			var trace = arguments.Remove("--trace-host-watchdog");
			var debug = arguments.Remove("--debug-host-watchdog");

			const string SystemDArg = "--Internal:UsingSystemD=true";
			if (!arguments.Any(arg => arg.Equals(SystemDArg, StringComparison.OrdinalIgnoreCase))
				&& SystemdHelpers.IsSystemdService())
				arguments.Add(SystemDArg);

			using var loggerFactory = LoggerFactory.Create(builder =>
			{
				if (trace)
					builder.SetMinimumLevel(LogLevel.Trace);
				else if (debug)
					builder.SetMinimumLevel(LogLevel.Debug);

				builder.AddConsole();
			});

			var logger = loggerFactory.CreateLogger(nameof(Program));
			try
			{
				if (trace && debug)
				{
					logger.LogCritical("Please specify only 1 of --trace-host-watchdog or --debug-host-watchdog!");
					return 2;
				}

				using var cts = new CancellationTokenSource();
				void AppDomainHandler(object? a, EventArgs b) => cts.Cancel();
				AppDomain.CurrentDomain.ProcessExit += AppDomainHandler;
				try
				{
					System.Console.CancelKeyPress += (a, b) =>
					{
						b.Cancel = true;
						cts.Cancel();
					};

					var watchdog = WatchdogFactory.CreateWatchdog(
						RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
							? new NoopSignalChecker()
							: new PosixSignalChecker(
								loggerFactory.CreateLogger<PosixSignalChecker>()),
						loggerFactory);

					return await watchdog.RunAsync(false, arguments.ToArray(), cts.Token)
						? 0
						: 1;
				}
				finally
				{
					AppDomain.CurrentDomain.ProcessExit -= AppDomainHandler;
				}
			}
			catch (Exception ex)
			{
				logger.LogCritical(ex, "Failed to run!");
				return 3;
			}
		}
	}
}
