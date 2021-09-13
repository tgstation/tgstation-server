using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

namespace Tgstation.Server.Host.Watchdog
{
	/// <summary>
	/// Helpers for launching an <see cref="IWatchdog"/>.
	/// </summary>
	public static class WatchdogHelpers
	{
		/// <summary>
		/// Launch an <see cref="IWatchdog"/> in console mode.
		/// </summary>
		/// <param name="watchdogFactory">The <see cref="IWatchdogFactory"/> to create the <see cref="IWatchdog"/> with.</param>
		/// <param name="loggerFactoryCallback">Optional <see cref="Action{T}"/> with a <see cref="ILoggerFactory"/> to run before the <see cref="IWatchdog"/>.</param>
		/// <param name="args">The command line arguments.</param>
		/// <param name="configureOnly">If the <see cref="IWatchdog"/> should just run the host setup wizard and exit.</param>
		/// <returns>A <see cref="Task"/> representing the running operation.</returns>
		public static async Task RunConsole(
			IWatchdogFactory watchdogFactory,
			Action<ILoggerFactory>? loggerFactoryCallback,
			string[] args,
			bool configureOnly)
		{
			if (watchdogFactory == null)
				throw new ArgumentNullException(nameof(watchdogFactory));

			using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());

			using var cts = new CancellationTokenSource();
			void AppDomainHandler(object? a, EventArgs b) => cts.Cancel();
			AppDomain.CurrentDomain.ProcessExit += AppDomainHandler;
			try
			{
				Console.CancelKeyPress += (a, b) =>
				{
					b.Cancel = true;
					cts.Cancel();
				};

				loggerFactoryCallback?.Invoke(loggerFactory);

				await watchdogFactory.CreateWatchdog(loggerFactory).RunAsync(configureOnly, args, cts.Token).ConfigureAwait(false);
			}
			finally
			{
				AppDomain.CurrentDomain.ProcessExit -= AppDomainHandler;
			}
		}
	}
}
