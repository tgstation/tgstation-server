using System;
using System.Diagnostics.CodeAnalysis;
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
		/// The <see cref="IWatchdog"/> for the <see cref="ServerService"/>
		/// </summary>
		IWatchdog watchdog;

		/// <summary>
		/// The <see cref="Task"/> recieved from <see cref="IWatchdog.RunAsync(string[], CancellationToken)"/> of <see cref="watchdog"/>
		/// </summary>
		Task watchdogTask;

		/// <summary>
		/// The <see cref="cancellationTokenSource"/> for the <see cref="ServerService"/>
		/// </summary>
		CancellationTokenSource cancellationTokenSource;

        /// <summary>
        /// Construct a <see cref="ServerService"/>
        /// </summary>
        /// <param name="watchdogFactory">The <see cref="IWatchdogFactory"/> to create <see cref="watchdog"/> with</param>
        public ServerService(IWatchdogFactory watchdogFactory)
		{
			if (watchdogFactory == null)
				throw new ArgumentNullException(nameof(watchdogFactory));
			ServiceName = Name;
            watchdog = watchdogFactory.CreateWatchdog();
		}

		/// <inheritdoc />
		[SuppressMessage("Microsoft.Usage", "CA2213:DisposableFieldsShouldBeDisposed", MessageId = "cancellationTokenSource")]
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
			watchdogTask = watchdog.RunAsync(args, cancellationTokenSource.Token);
		}

		/// <inheritdoc />
		protected override void OnStop()
		{
			cancellationTokenSource.Cancel();
			watchdogTask.GetAwaiter().GetResult();
		}
	}
}