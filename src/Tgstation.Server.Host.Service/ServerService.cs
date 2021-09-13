using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using Tgstation.Server.Helpers;
using Tgstation.Server.Host.Watchdog;

namespace Tgstation.Server.Host.Service
{
	/// <summary>
	/// .NET <see cref="IHostedService"/> which runs inside the service controller.
	/// </summary>
	sealed class ServerService : IHostedService, IAsyncDisposable
	{
		/// <summary>
		/// The <see cref="IWatchdog"/> for the <see cref="ServerService"/>.
		/// </summary>
		readonly IWatchdog watchdog;

		/// <summary>
		/// The <see cref="ILogger"/> for the <see cref="ServerService"/>.
		/// </summary>
		readonly ILogger<ServerService> logger;

		/// <summary>
		/// <see langword="lock"/> <see cref="object"/> for <see cref="activeTask"/>.
		/// </summary>
		readonly object activeTaskLock;

		/// <summary>
		/// The <see cref="CancellableTask"/> for the <see cref="watchdog"/>.
		/// </summary>
		CancellableTask? activeTask;

		/// <summary>
		/// Initializes a new instance of the <see cref="ServerService"/> class.
		/// </summary>
		/// <param name="watchdog">The value of <see cref="watchdog"/>.</param>
		/// <param name="logger">The value of <see cref="logger"/>.</param>
		public ServerService(IWatchdog watchdog, ILogger<ServerService> logger)
		{
			this.watchdog = watchdog ?? throw new ArgumentNullException(nameof(watchdog));
			this.logger = logger ?? throw new ArgumentNullException(nameof(logger));

			activeTaskLock = new object();
		}

		/// <inheritdoc />
		public async ValueTask DisposeAsync()
		{
			if (activeTask != null)
			{
				await activeTask.DisposeAsync().ConfigureAwait(false);
				activeTask = null;
			}
		}

		/// <inheritdoc />
		public Task StartAsync(CancellationToken cancellationToken)
		{
			lock (activeTaskLock)
			{
				if (activeTask != null)
					throw new InvalidOperationException("Service already running!");

				activeTask = new CancellableTask(token => watchdog.RunAsync(false, Environment.GetCommandLineArgs(), token));
				return Task.CompletedTask;
			}
		}

		/// <inheritdoc />
		public Task StopAsync(CancellationToken cancellationToken)
		{
			CancellableTask? localActiveTask;
			lock (activeTaskLock)
			{
				localActiveTask = activeTask;
				if (localActiveTask == null)
					throw new InvalidOperationException("Service not running!");

				activeTask = null;
			}

			return localActiveTask.DisposeAsync().AsTask();
		}
	}
}
