using System.ServiceProcess;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Host.Watchdog;

namespace Tgstation.Server.Host.Service
{
	/// <summary>
	/// Represents a <see cref="IServer"/> as a <see cref="ServiceBase"/>
	/// </summary>
	sealed class ServerService : ServiceBase
	{
		/// <summary>
		/// The <see cref="IWatchdog"/> for the <see cref="ServerService"/>
		/// </summary>
		IWatchdog watchdog;

		/// <summary>
		/// The <see cref="Task"/> recieved from <see cref="IServer.RunAsync(string[], CancellationToken)"/> of <see cref="server"/>
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
			ServiceName = "tgstation-server";
            watchdog = watchdogFactory.CreateWatchdog();
		}

		/// <inheritdoc />
		protected override void Dispose(bool disposing)
		{
			cancellationTokenSource.Dispose();
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