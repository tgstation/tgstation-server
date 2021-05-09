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
		public const string Name = "tgstation-server-4";

		/// <summary>
		/// The <see cref="IWatchdog"/> for the <see cref="ServerService"/>.
		/// </summary>
		readonly IWatchdog watchdog;

		/// <summary>
		/// The <see cref="Task"/> recieved from <see cref="IWatchdog.RunAsync(bool, string[], CancellationToken)"/> of <see cref="watchdog"/>.
		/// </summary>
		Task watchdogTask;

		/// <summary>
		/// The <see cref="cancellationTokenSource"/> for the <see cref="ServerService"/>.
		/// </summary>
		CancellationTokenSource cancellationTokenSource;

		/// <summary>
		/// Initializes a new instance of the <see cref="ServerService"/> class.
		/// </summary>
		/// <param name="watchdogFactory">The <see cref="IWatchdogFactory"/> to create <see cref="watchdog"/> with.</param>
		/// <param name="loggerFactory">The <see cref="ILoggerFactory"/> for <paramref name="watchdogFactory"/>.</param>
		/// <param name="minumumLogLevel">The minimum <see cref="Microsoft.Extensions.Logging.LogLevel"/> to record in the event log.</param>
		public ServerService(IWatchdogFactory watchdogFactory, ILoggerFactory loggerFactory, LogLevel minumumLogLevel)
		{
			if (watchdogFactory == null)
				throw new ArgumentNullException(nameof(watchdogFactory));
			if (loggerFactory == null)
				throw new ArgumentNullException(nameof(loggerFactory));

#pragma warning disable CS0618 // Type or member is obsolete
			loggerFactory.AddEventLog(new EventLogSettings
			{
				LogName = EventLog.Log,
				MachineName = EventLog.MachineName,
				SourceName = EventLog.Source,
				Filter = (message, logLevel) => logLevel >= minumumLogLevel,
			});
#pragma warning restore CS0618 // Type or member is obsolete

			ServiceName = Name;
			watchdog = watchdogFactory.CreateWatchdog(loggerFactory);
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
			cancellationTokenSource?.Dispose();
			cancellationTokenSource = new CancellationTokenSource();
			watchdogTask = RunWatchdog(args, cancellationTokenSource.Token);
		}

		/// <inheritdoc />
		protected override void OnStop()
		{
			cancellationTokenSource.Cancel();
			watchdogTask.GetAwaiter().GetResult();
		}

		/// <summary>
		/// Executes the <see cref="watchdog"/>, stopping the service if it exits.
		/// </summary>
		/// <param name="args">The arguments for the <see cref="watchdog"/>.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task"/> representing the running operation.</returns>
		async Task RunWatchdog(string[] args, CancellationToken cancellationToken)
		{
			await watchdog.RunAsync(false, args, cancellationTokenSource.Token).ConfigureAwait(false);

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
