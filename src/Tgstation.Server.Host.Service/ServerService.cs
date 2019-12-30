using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.EventLog;
using System;
using System.Diagnostics;
using System.Globalization;
using System.ServiceProcess;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Host.Watchdog;

namespace Tgstation.Server.Host.Service
{
	/// <summary>
	/// Represents a <see cref="IWatchdog"/> as a <see cref="ServiceBase"/>
	/// </summary>
	sealed class ServerService : ServiceBase
	{
		/// <summary>
		/// The canonical windows service name
		/// </summary>
		public const string Name = "tgstation-server-4";

		/// <summary>
		/// The <see cref="IWatchdogFactory"/> for the <see cref="ServerService"/>
		/// </summary>
		readonly IWatchdogFactory watchdogFactory;

		/// <summary>
		/// The <see cref="Func{TResult}"/> used to retrieve a configured <see cref="ILoggerFactory"/>.
		/// </summary>
		readonly Func<ILoggerFactory> getLoggerFactory;

		/// <summary>
		/// The <see cref="Task"/> recieved from <see cref="IWatchdog.RunAsync(bool, string[], CancellationToken)"/>.
		/// </summary>
		Task watchdogTask;

		/// <summary>
		/// The <see cref="cancellationTokenSource"/> for the <see cref="ServerService"/>
		/// </summary>
		CancellationTokenSource cancellationTokenSource;

		/// <summary>
		/// Construct a <see cref="ServerService"/>
		/// </summary>
		/// <param name="watchdogFactory">The value of <see cref="watchdogFactory"/>.</param>
		/// <param name="loggingBuilder">The <see cref="ILoggingBuilder"/> to configure.</param>
		/// <param name="getLoggerFactory">The <see cref="Func{TResult}"/> used to retrieve a <see cref="ILoggerFactory"/> based on the <paramref name="loggingBuilder"/> configuration.</param>
		/// <param name="minumumLogLevel">The minimum <see cref="Microsoft.Extensions.Logging.LogLevel"/> to record in the event log</param>
		public ServerService(IWatchdogFactory watchdogFactory, ILoggingBuilder loggingBuilder, Func<ILoggerFactory> getLoggerFactory, LogLevel minumumLogLevel)
		{
			this.watchdogFactory = watchdogFactory ?? throw new ArgumentNullException(nameof(watchdogFactory));
			if (loggingBuilder == null)
				throw new ArgumentNullException(nameof(loggingBuilder));
			this.getLoggerFactory = getLoggerFactory ?? throw new ArgumentNullException(nameof(getLoggerFactory));

			ServiceName = Name;

			loggingBuilder.AddEventLog(new EventLogSettings
			{
				LogName = EventLog.Log,
				MachineName = EventLog.MachineName,
				SourceName = EventLog.Source,
				Filter = (message, logLevel) => logLevel >= minumumLogLevel
			});
		}

		/// <summary>
		/// Creates and executes the watchdog stopping the service if it exits
		/// </summary>
		/// <param name="args">The arguments for the watchdog.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		async Task RunWatchdog(string[] args, CancellationToken cancellationToken)
		{
			var watchdog = watchdogFactory.CreateWatchdog(getLoggerFactory());

			await watchdog.RunAsync(false, args, cancellationToken).ConfigureAwait(false);

			void StopServiceAsync()
			{
				try
				{
					Task.Run(Stop, cancellationToken);
				}
				catch (OperationCanceledException) { }
				catch (Exception e)
				{
					EventLog.WriteEntry(String.Format(CultureInfo.InvariantCulture, "Error stopping service! Exception: {0}", e));
				}
			}

			StopServiceAsync();
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
	}
}