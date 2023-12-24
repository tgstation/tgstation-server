using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.Versioning;
using System.ServiceProcess;
using System.Threading;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.EventLog;

using Tgstation.Server.Common;
using Tgstation.Server.Host.Watchdog;

namespace Tgstation.Server.Host.Service
{
	/// <summary>
	/// Represents a <see cref="IWatchdog"/> as a <see cref="ServiceBase"/>.
	/// </summary>
	[SupportedOSPlatform("windows")]
	sealed class ServerService : ServiceBase
	{
		/// <summary>
		/// The canonical windows service name.
		/// </summary>
		public const string Name = Constants.CanonicalPackageName;

		/// <summary>
		/// The <see cref="IWatchdog"/> for the <see cref="ServerService"/>.
		/// </summary>
		readonly IWatchdogFactory watchdogFactory;

		/// <summary>
		/// The <see cref="Lazy{T}"/> <see cref="ILoggerFactory"/> used by the <see cref="ServerService"/>.
		/// </summary>
		readonly Lazy<ILoggerFactory> loggerFactory;

		/// <summary>
		/// The <see cref="Array"/> of command line arguments the service was invoked with.
		/// </summary>
		readonly string[] commandLineArguments;

		/// <summary>
		/// The active <see cref="ServiceLifetime"/>.
		/// </summary>
#pragma warning disable CA2213 // Disposable fields should be disposed
		volatile ServiceLifetime? serviceLifetime;
#pragma warning restore CA2213 // Disposable fields should be disposed

		/// <summary>
		/// Initializes a new instance of the <see cref="ServerService"/> class.
		/// </summary>
		/// <param name="watchdogFactory">The value of <see cref="watchdogFactory"/>.</param>
		/// <param name="commandLineArguments">The value of <see cref="commandLineArguments"/>.</param>
		/// <param name="minimumLogLevel">The minimum <see cref="LogLevel"/> to record in the event log.</param>
		public ServerService(IWatchdogFactory watchdogFactory, string[] commandLineArguments, LogLevel minimumLogLevel)
		{
			this.watchdogFactory = watchdogFactory ?? throw new ArgumentNullException(nameof(watchdogFactory));
			this.commandLineArguments = commandLineArguments ?? throw new ArgumentNullException(nameof(commandLineArguments));

			ServiceName = Name;
			loggerFactory = new Lazy<ILoggerFactory>(() => LoggerFactory.Create(builder => builder.AddEventLog(new EventLogSettings
			{
				LogName = EventLog.Log,
				MachineName = EventLog.MachineName,
				SourceName = EventLog.Source,
				Filter = (message, logLevel) => logLevel >= minimumLogLevel,
			})));
		}

		/// <summary>
		/// Executes the <see cref="ServerService"/>.
		/// </summary>
		public void Run() => Run(this);

		/// <inheritdoc />
		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				OnStop();

				if (loggerFactory.IsValueCreated)
					loggerFactory.Value.Dispose();
			}

			base.Dispose(disposing);
		}

		/// <inheritdoc />
		protected override void OnCustomCommand(int command) => serviceLifetime!.HandleCustomCommand(command);

		/// <inheritdoc />
		protected override void OnStart(string[] args)
		{
			var newArgs = new List<string>(commandLineArguments.Length + args.Length + 1)
			{
				"--General:SetupWizardMode=Never",
			};

			newArgs.AddRange(commandLineArguments);
			newArgs.AddRange(args);

			serviceLifetime = new ServiceLifetime(
				Stop,
				signalChecker => watchdogFactory.CreateWatchdog(signalChecker, loggerFactory.Value),
				loggerFactory.Value.CreateLogger<ServiceLifetime>(),
				newArgs.ToArray());
		}

		/// <inheritdoc />
		protected override void OnStop()
		{
			var oldLifetime = Interlocked.Exchange(ref serviceLifetime, null);
			oldLifetime?.DisposeAsync().GetAwaiter().GetResult();
		}
	}
}
