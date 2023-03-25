using System;
using System.Diagnostics;
using System.Globalization;
using System.ServiceProcess;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.EventLog;

using Tgstation.Server.Host.Watchdog;

namespace Tgstation.Server.Host.Service
{
	/// <summary>
	/// Represents a <see cref="IWatchdog"/> as a <see cref="ServiceBase"/>.
	/// </summary>
	sealed class ServerService : ServiceBase
	{
		/// <summary>
		/// The canonical windows service name.
		/// </summary>
		public const string Name = "tgstation-server";

		/// <summary>
		/// The <see cref="IWatchdog"/> for the <see cref="ServerService"/>.
		/// </summary>
		readonly IWatchdogFactory watchdogFactory;

		/// <summary>
		/// The minimum <see cref="Microsoft.Extensions.Logging.LogLevel"/> for the <see cref="EventLog"/>.
		/// </summary>
		readonly LogLevel minimumLogLevel;

		/// <summary>
		/// The <see cref="Task"/> that represents the running service.
		/// </summary>
		Task watchdogTask;

		/// <summary>
		/// The <see cref="cancellationTokenSource"/> for the <see cref="ServerService"/>.
		/// </summary>
		CancellationTokenSource cancellationTokenSource;

		/// <summary>
		/// Initializes a new instance of the <see cref="ServerService"/> class.
		/// </summary>
		/// <param name="watchdogFactory">The value of <see cref="watchdogFactory"/>.</param>
		/// <param name="minimumLogLevel">The minimum <see cref="Microsoft.Extensions.Logging.LogLevel"/> to record in the event log.</param>
		public ServerService(IWatchdogFactory watchdogFactory, LogLevel minimumLogLevel)
		{
			this.watchdogFactory = watchdogFactory ?? throw new ArgumentNullException(nameof(watchdogFactory));
			this.minimumLogLevel = minimumLogLevel;
			ServiceName = Name;
		}

		/// <inheritdoc />
		protected override void Dispose(bool disposing)
		{
			cancellationTokenSource?.Dispose();
			base.Dispose(disposing);
		}

		/// <inheritdoc />
		protected override void OnStart(string[] args)
		{
			var loggerFactory = LoggerFactory.Create(builder => builder.AddEventLog(new EventLogSettings
			{
				LogName = EventLog.Log,
				MachineName = EventLog.MachineName,
				SourceName = EventLog.Source,
				Filter = (message, logLevel) => logLevel >= minimumLogLevel,
			}));

			var watchdog = watchdogFactory.CreateWatchdog(loggerFactory);

			cancellationTokenSource?.Dispose();
			cancellationTokenSource = new CancellationTokenSource();

			watchdogTask = RunWatchdog(watchdog, args, cancellationTokenSource.Token);
		}

		/// <inheritdoc />
		protected override void OnStop()
		{
			cancellationTokenSource.Cancel();
			watchdogTask.GetAwaiter().GetResult();
		}

		/// <summary>
		/// Executes the <paramref name="watchdog"/>, stopping the service if it exits.
		/// </summary>
		/// <param name="watchdog">The <see cref="IWatchdog"/> to run.</param>
		/// <param name="args">The arguments for the <paramref name="watchdog"/>.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task"/> representing the running operation.</returns>
		async Task RunWatchdog(IWatchdog watchdog, string[] args, CancellationToken cancellationToken)
		{
			await watchdog.RunAsync(false, args, cancellationTokenSource.Token);

			void StopServiceAsync()
			{
				try
				{
					Task.Run(Stop, cancellationToken);
				}
				catch (OperationCanceledException)
				{
				}
				catch (Exception e)
				{
					EventLog.WriteEntry(String.Format(CultureInfo.InvariantCulture, "Error stopping service! Exception: {0}", e));
				}
			}

			StopServiceAsync();
		}
	}
}
