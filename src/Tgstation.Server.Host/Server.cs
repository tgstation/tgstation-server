using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Tgstation.Server.Host.Configuration;
using Tgstation.Server.Host.Core;
using Tgstation.Server.Host.IO;
using Tgstation.Server.Host.Swarm;

namespace Tgstation.Server.Host
{
	/// <inheritdoc />
	sealed class Server : IServer, IServerControl
	{
		/// <inheritdoc />
		public bool RestartRequested { get; private set; }

		/// <inheritdoc />
		public bool UpdateInProgress { get; private set; }

		/// <inheritdoc />
		public bool WatchdogPresent =>
#if WATCHDOG_FREE_RESTART
			true;
#else
			updatePath != null;
#endif

		/// <summary>
		/// The <see cref="IHostBuilder"/> for the <see cref="Server"/>.
		/// </summary>
		readonly IHostBuilder hostBuilder;

		/// <summary>
		/// The <see cref="IRestartHandler"/>s to run when the <see cref="Server"/> restarts.
		/// </summary>
		readonly List<IRestartHandler> restartHandlers;

		/// <summary>
		/// The absolute path to install updates to.
		/// </summary>
		readonly string updatePath;

		/// <summary>
		/// <see langword="lock"/> <see cref="object"/> for certain restart related operations.
		/// </summary>
		readonly object restartLock;

		/// <summary>
		/// The <see cref="ISwarmService"/> for the <see cref="Server"/>.
		/// </summary>
		ISwarmService swarmService;

		/// <summary>
		/// The <see cref="ILogger"/> for the <see cref="Server"/>.
		/// </summary>
		ILogger<Server> logger;

		/// <summary>
		/// The <see cref="GeneralConfiguration"/> for the <see cref="Server"/>.
		/// </summary>
		GeneralConfiguration generalConfiguration;

		/// <summary>
		/// The <see cref="cancellationTokenSource"/> for the <see cref="Server"/>.
		/// </summary>
		CancellationTokenSource cancellationTokenSource;

		/// <summary>
		/// The <see cref="Exception"/> to propagate when the server terminates.
		/// </summary>
		Exception propagatedException;

		/// <summary>
		/// If the server is being shut down or restarted.
		/// </summary>
		bool shutdownInProgress;

		/// <summary>
		/// Initializes a new instance of the <see cref="Server"/> class.
		/// </summary>
		/// <param name="hostBuilder">The value of <see cref="hostBuilder"/>.</param>
		/// <param name="updatePath">The value of <see cref="updatePath"/>.</param>
		public Server(IHostBuilder hostBuilder, string updatePath)
		{
			this.hostBuilder = hostBuilder ?? throw new ArgumentNullException(nameof(hostBuilder));
			this.updatePath = updatePath;

			hostBuilder.ConfigureServices(serviceCollection => serviceCollection.AddSingleton<IServerControl>(this));

			restartHandlers = new List<IRestartHandler>();
			restartLock = new object();
		}

		/// <inheritdoc />
		public async Task Run(CancellationToken cancellationToken)
		{
			using (cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
			using (var fsWatcher = updatePath != null ? new FileSystemWatcher(Path.GetDirectoryName(updatePath)) : null)
			{
				if (fsWatcher != null)
				{
					fsWatcher.Created += (a, b) =>
					{
						if (b.FullPath == updatePath && File.Exists(b.FullPath))
						{
							if (logger != null)
								logger.LogInformation("Host watchdog appears to be requesting server termination!");
							cancellationTokenSource.Cancel();
						}
					};
					fsWatcher.EnableRaisingEvents = true;
				}

				using var host = hostBuilder.Build();
				try
				{
					swarmService = host.Services.GetRequiredService<ISwarmService>();
					logger = host.Services.GetRequiredService<ILogger<Server>>();
					using (cancellationToken.Register(() => logger.LogInformation("Server termination requested!")))
					{
						var generalConfigurationOptions = host.Services.GetRequiredService<IOptions<GeneralConfiguration>>();
						generalConfiguration = generalConfigurationOptions.Value;
						await host.RunAsync(cancellationTokenSource.Token).ConfigureAwait(false);
					}
				}
				catch (Exception ex)
				{
					CheckExceptionPropagation(ex);
					throw;
				}
			}

			CheckExceptionPropagation(null);
		}

		/// <inheritdoc />
		public bool ApplyUpdate(Version version, Uri updateZipUrl, IIOManager ioManager)
		{
			if (version == null)
				throw new ArgumentNullException(nameof(version));
			if (updateZipUrl == null)
				throw new ArgumentNullException(nameof(updateZipUrl));
			if (ioManager == null)
				throw new ArgumentNullException(nameof(ioManager));

			CheckSanity(true);

			logger.LogTrace("Begin ApplyUpdate...");

			lock (restartLock)
			{
				if (UpdateInProgress || shutdownInProgress)
				{
					logger.LogDebug("Aborted update due to concurrency conflict!");
					return false;
				}

				UpdateInProgress = true;
			}

			async void RunUpdate()
			{
				try
				{
					logger.LogInformation("Updating server to version {0} ({1})...", version, updateZipUrl);

					if (cancellationTokenSource == null)
						throw new InvalidOperationException("Tried to update a non-running Server!");
					var cancellationToken = cancellationTokenSource.Token;

					var updatePrepareResult = await swarmService.PrepareUpdate(version, cancellationToken).ConfigureAwait(false);
					if (!updatePrepareResult)
						return;

					MemoryStream updateZipData;
					try
					{
						logger.LogTrace("Downloading zip package...");
						updateZipData = new MemoryStream(
							await ioManager.DownloadFile(
								updateZipUrl,
								cancellationToken)
							.ConfigureAwait(false));
					}
					catch (Exception e1)
					{
						try
						{
							await swarmService.AbortUpdate(cancellationToken).ConfigureAwait(false);
						}
						catch (Exception e2)
						{
							throw new AggregateException(e1, e2);
						}

						throw;
					}

					using (updateZipData)
					{
						var updateCommitResult = await swarmService.CommitUpdate(cancellationToken).ConfigureAwait(false);
						if (!updateCommitResult)
						{
							logger.LogError("Swarm distributed commit failed, not applying update!");
							return;
						}

						try
						{
							logger.LogTrace("Extracting zip package to {0}...", updatePath);
							await ioManager.ZipToDirectory(updatePath, updateZipData, cancellationToken).ConfigureAwait(false);
						}
						catch (Exception e)
						{
							UpdateInProgress = false;
							try
							{
								// important to not leave this directory around if possible
								await ioManager.DeleteDirectory(updatePath, default).ConfigureAwait(false);
							}
							catch (Exception e2)
							{
								throw new AggregateException(e, e2);
							}

							throw;
						}
					}

					await Restart(version, null, true).ConfigureAwait(false);
				}
				catch (OperationCanceledException)
				{
					logger.LogInformation("Server update cancelled!");
				}
				catch (Exception e)
				{
					logger.LogError(e, "Error updating server!");
				}
				finally
				{
					UpdateInProgress = false;
				}
			}

			RunUpdate();
			return true;
		}

		/// <inheritdoc />
		public IRestartRegistration RegisterForRestart(IRestartHandler handler)
		{
			if (handler == null)
				throw new ArgumentNullException(nameof(handler));

			CheckSanity(false);

			lock (restartLock)
				if (!shutdownInProgress)
				{
					logger.LogTrace("Registering restart handler {0}...", handler);
					restartHandlers.Add(handler);
					return new RestartRegistration(() =>
					{
						lock (restartLock)
							if (!shutdownInProgress)
								restartHandlers.Remove(handler);
					});
				}

			return new RestartRegistration(() => { });
		}

		/// <inheritdoc />
		public Task Restart() => Restart(null, null, true);

		/// <inheritdoc />
		public Task GracefulShutdown() => Restart(null, null, false);

		/// <inheritdoc />
		public Task Die(Exception exception) => Restart(null, exception, false);

		/// <summary>
		/// Throws an <see cref="InvalidOperationException"/> if the <see cref="IServerControl"/> cannot be used.
		/// </summary>
		/// <param name="checkWatchdog">If <see cref="WatchdogPresent"/> should be checked.</param>
		void CheckSanity(bool checkWatchdog)
		{
			if (checkWatchdog && !WatchdogPresent && propagatedException == null)
				throw new InvalidOperationException("Server restarts are not supported");

			if (cancellationTokenSource == null || logger == null)
				throw new InvalidOperationException("Tried to control a non-running Server!");
		}

		/// <summary>
		/// Re-throw <see cref="propagatedException"/> if it exists.
		/// </summary>
		/// <param name="otherException">An existing <see cref="Exception"/> that should be thrown as well, but not by itself.</param>
		void CheckExceptionPropagation(Exception otherException)
		{
			if (propagatedException == null)
				return;

			if (otherException != null)
				throw new AggregateException(propagatedException, otherException);

			throw propagatedException;
		}

		/// <summary>
		/// Implements <see cref="Restart()"/>.
		/// </summary>
		/// <param name="newVersion">The <see cref="Version"/> of any potential updates being applied.</param>
		/// <param name="exception">The potential value of <see cref="propagatedException"/>.</param>
		/// <param name="requireWatchdog">If the host watchdog is required for this "restart".</param>
		/// <returns>A <see cref="Task"/> representing the running operation.</returns>
		async Task Restart(Version newVersion, Exception exception, bool requireWatchdog)
		{
			CheckSanity(requireWatchdog);

			// if the watchdog isn't required and there's no issue, this is just a graceful shutdown
			bool isGracefulShutdown = !requireWatchdog && exception == null;
			logger.LogTrace(
				"Begin {0}...",
				isGracefulShutdown
					? "graceful shutdown"
					: "restart");

			lock (restartLock)
			{
				if ((UpdateInProgress && newVersion == null) || shutdownInProgress)
				{
					logger.LogTrace("Aborted restart due to concurrency conflict!");
					return;
				}

				shutdownInProgress = true;
				RestartRequested = !isGracefulShutdown;
				propagatedException ??= exception;
			}

			if (exception == null)
			{
				logger.LogInformation("Stopping server...");
				using var cts = new CancellationTokenSource(
					TimeSpan.FromMinutes(
						isGracefulShutdown
							? generalConfiguration.ShutdownTimeoutMinutes
							: generalConfiguration.RestartTimeoutMinutes));
				var cancellationToken = cts.Token;
				var eventsTask = Task.WhenAll(
					restartHandlers.Select(
						x => x.HandleRestart(newVersion, isGracefulShutdown, cancellationToken))
					.ToList());

				logger.LogTrace("Joining restart handlers...");
				try
				{
					await eventsTask.ConfigureAwait(false);
				}
				catch (OperationCanceledException ex)
				{
					if (isGracefulShutdown)
						logger.LogWarning(ex, "Graceful shutdown timeout hit! Existing DreamDaemon processes will be terminated!");
					else
						logger.LogError(
							ex,
							"Restart timeout hit! Existing DreamDaemon processes will be lost and must be killed manually before being restarted with TGS!");
				}
				catch (Exception e)
				{
					logger.LogError(e, "Restart handlers error!");
				}
			}

			logger.LogTrace("Stopping host...");
			cancellationTokenSource.Cancel();
		}
	}
}
