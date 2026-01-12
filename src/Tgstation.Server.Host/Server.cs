using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using HotChocolate.Execution;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Tgstation.Server.Common.Extensions;
using Tgstation.Server.Host.Configuration;
using Tgstation.Server.Host.Core;
using Tgstation.Server.Host.IO;
using Tgstation.Server.Host.Utils;

namespace Tgstation.Server.Host
{
	/// <inheritdoc cref="IServer" />
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
		/// The <see cref="IHost"/> of the running server.
		/// </summary>
		internal IHost? Host { get; private set; }

		/// <summary>
		/// The <see cref="IIOManager"/> to use.
		/// </summary>
		readonly IIOManager ioManager;

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
		readonly string? updatePath;

		/// <summary>
		/// <see langword="lock"/> <see cref="object"/> for certain restart related operations.
		/// </summary>
		readonly object restartLock;

		/// <summary>
		/// The <see cref="IOptionsMonitor{TOptions}"/> of <see cref="GeneralConfiguration"/> for the <see cref="Server"/>.
		/// </summary>
		IOptionsMonitor<GeneralConfiguration>? generalConfigurationOptions;

		/// <summary>
		/// The <see cref="ILogger"/> for the <see cref="Server"/>.
		/// </summary>
		ILogger<Server>? logger;

		/// <summary>
		/// The <see cref="cancellationTokenSource"/> for the <see cref="Server"/>.
		/// </summary>
		CancellationTokenSource? cancellationTokenSource;

		/// <summary>
		/// The <see cref="Exception"/> to propagate when the server terminates.
		/// </summary>
		Exception? propagatedException;

		/// <summary>
		/// The <see cref="Task"/> that is used for asynchronously updating the server.
		/// </summary>
		Task? updateTask;

		/// <summary>
		/// If the server is being shut down or restarted.
		/// </summary>
		bool shutdownInProgress;

		/// <summary>
		/// If there is an update in progress and this flag is set, it should stop the server immediately if it fails.
		/// </summary>
		bool terminateIfUpdateFails;

		/// <summary>
		/// Initializes a new instance of the <see cref="Server"/> class.
		/// </summary>
		/// <param name="ioManager">The value of <see cref="ioManager"/>.</param>
		/// <param name="hostBuilder">The value of <see cref="hostBuilder"/>.</param>
		/// <param name="updatePath">The value of <see cref="updatePath"/>.</param>
		public Server(IIOManager ioManager, IHostBuilder hostBuilder, string? updatePath)
		{
			this.ioManager = ioManager ?? throw new ArgumentNullException(nameof(ioManager));
			this.hostBuilder = hostBuilder ?? throw new ArgumentNullException(nameof(hostBuilder));
			this.updatePath = updatePath;

			hostBuilder.ConfigureServices(serviceCollection => serviceCollection.AddSingleton<IServerControl>(this));

			restartHandlers = new List<IRestartHandler>();
			restartLock = new object();
			logger = null;
		}

		/// <inheritdoc />
		public async ValueTask Run(CancellationToken cancellationToken)
		{
			var updateDirectory = updatePath != null ? ioManager.GetDirectoryName(updatePath) : null;
			using (cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
			using (var fsWatcher = updateDirectory != null ? new FileSystemWatcher(updateDirectory) : null)
			{
				if (fsWatcher != null)
				{
					// If ever there is a NECESSARY update to the Host Watchdog, change this to use a pipe
					// I don't know why I'm only realizing this in 2023 when this is 2019 code
					// As it stands, FSWatchers use async I/O on Windows and block a new thread on Linux
					// That's an acceptable, if saddening, resource loss for now
					fsWatcher.Created += WatchForShutdownFileCreation;
					fsWatcher.EnableRaisingEvents = true;
				}

				try
				{
					using (Host = hostBuilder.Build())
					{
						logger = Host.Services.GetRequiredService<ILogger<Server>>();
						try
						{
							using (cancellationToken.Register(() => logger.LogInformation("Server termination requested!")))
							{
								if (await DumpGraphQLSchemaIfRequested(Host.Services, cancellationToken))
									return;

								generalConfigurationOptions = Host.Services.GetRequiredService<IOptionsMonitor<GeneralConfiguration>>();
								await Host.RunAsync(cancellationTokenSource.Token);
							}

							if (updateTask != null)
								await updateTask;
						}
						catch (OperationCanceledException ex)
						{
							logger.LogDebug(ex, "Server run cancelled!");
						}
						catch (Exception ex)
						{
							CheckExceptionPropagation(ex);
							throw;
						}
						finally
						{
							logger = null;
						}
					}
				}
				finally
				{
					Host = null;
				}
			}

			CheckExceptionPropagation(null);
		}

		/// <inheritdoc />
		public bool TryStartUpdate(IServerUpdateExecutor updateExecutor, Version newVersion)
		{
			ArgumentNullException.ThrowIfNull(updateExecutor);
			ArgumentNullException.ThrowIfNull(newVersion);

			CheckSanity(true);

			if (updatePath == null)
				throw new InvalidOperationException("Tried to start update when server was initialized without an updatePath set!");

			var logger = this.logger!;
			logger.LogTrace("Begin ApplyUpdate...");

			CancellationToken criticalCancellationToken;
			lock (restartLock)
			{
				if (UpdateInProgress || shutdownInProgress)
				{
					logger.LogDebug("Aborted update due to concurrency conflict!");
					return false;
				}

				if (cancellationTokenSource == null)
					throw new InvalidOperationException("Tried to update a non-running Server!");

				criticalCancellationToken = cancellationTokenSource.Token;
				UpdateInProgress = true;
			}

			async Task RunUpdate()
			{
				var updateExecutedSuccessfully = false;
				try
				{
					updateExecutedSuccessfully = await updateExecutor.ExecuteUpdate(updatePath, criticalCancellationToken, criticalCancellationToken);
				}
				catch (OperationCanceledException ex)
				{
					logger.LogDebug(ex, "Update cancelled!");
					UpdateInProgress = false;
				}
				catch (Exception ex)
				{
					logger.LogError(ex, "Update errored!");
					UpdateInProgress = false;
				}

				if (updateExecutedSuccessfully)
				{
					logger.LogTrace("Update complete!");
					await RestartImpl(newVersion, null, true, true);
				}
				else if (terminateIfUpdateFails)
				{
					logger.LogTrace("Stopping host due to termination request...");
					cancellationTokenSource.Cancel();
				}
				else
				{
					logger.LogTrace("Update failed!");
					UpdateInProgress = false;
				}
			}

			updateTask = RunUpdate();
			return true;
		}

		/// <inheritdoc />
		public IRestartRegistration RegisterForRestart(IRestartHandler handler)
		{
			ArgumentNullException.ThrowIfNull(handler);

			CheckSanity(false);

			var logger = this.logger!;
			lock (restartLock)
				if (!shutdownInProgress)
				{
					logger.LogTrace("Registering restart handler {handlerImplementationName}...", handler);
					restartHandlers.Add(handler);
					return new RestartRegistration(
						new DisposeInvoker(() =>
						{
							lock (restartLock)
								if (!shutdownInProgress)
									restartHandlers.Remove(handler);
						}));
				}

			logger.LogWarning("Restart handler {handlerImplementationName} register after a shutdown had begun!", handler);
			return new RestartRegistration(null);
		}

		/// <inheritdoc />
		public ValueTask Restart() => RestartImpl(null, null, true, true);

		/// <inheritdoc />
		public ValueTask GracefulShutdown(bool detach) => RestartImpl(null, null, false, detach);

		/// <inheritdoc />
		public ValueTask Die(Exception? exception)
		{
			if (exception != null)
				return RestartImpl(null, exception, false, true);

			StopServerImmediate();
			return ValueTask.CompletedTask;
		}

		/// <summary>
		/// Checks if <see cref="InternalConfiguration.DumpGraphQLApiPath"/> is set and dumps the GraphQL API Schema to it if so.
		/// </summary>
		/// <param name="services">The <see cref="IServiceProvider"/> to resolve services from.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in <see langword="true"/> if the GraphQL API was dumped, <see langword="false"/> otherwise.</returns>
		async ValueTask<bool> DumpGraphQLSchemaIfRequested(IServiceProvider services, CancellationToken cancellationToken)
		{
			var internalConfigurationOptions = services.GetRequiredService<IOptions<InternalConfiguration>>();
			var apiDumpPath = internalConfigurationOptions.Value.DumpGraphQLApiPath;
			if (String.IsNullOrWhiteSpace(apiDumpPath))
				return false;

			logger!.LogInformation("Dumping GraphQL API spec to {path} and exiting...", apiDumpPath);

			// https://github.com/ChilliCream/graphql-platform/discussions/5885
			var resolver = services.GetRequiredService<IRequestExecutorResolver>();
			var executor = await resolver.GetRequestExecutorAsync(cancellationToken: cancellationToken);
			var sdl = executor.Schema.Print();

			var ioManager = services.GetRequiredService<IIOManager>();
			await ioManager.WriteAllBytes(apiDumpPath, Encoding.UTF8.GetBytes(sdl), cancellationToken);
			return true;
		}

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
		void CheckExceptionPropagation(Exception? otherException)
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
		/// <param name="completeAsap">If the restart should wait for extremely long running tasks to complete (Like the current DreamDaemon world).</param>
		/// <returns>A <see cref="ValueTask"/> representing the running operation.</returns>
		async ValueTask RestartImpl(Version? newVersion, Exception? exception, bool requireWatchdog, bool completeAsap)
		{
			CheckSanity(requireWatchdog);

			// if the watchdog isn't required and there's no issue, this is just a graceful shutdown
			bool isGracefulShutdown = !requireWatchdog && exception == null;
			var logger = this.logger!;
			logger.LogTrace(
				"Begin {restartType}...",
				isGracefulShutdown
					? completeAsap
						? "semi-graceful shutdown"
						: "graceful shutdown"
					: "restart");

			lock (restartLock)
			{
				if ((UpdateInProgress && newVersion == null) || shutdownInProgress)
				{
					logger.LogTrace("Aborted restart due to concurrency conflict!");
					return;
				}

				RestartRequested = !isGracefulShutdown;
				propagatedException ??= exception;
			}

			if (exception == null)
			{
				var giveHandlersTimeToWaitAround = isGracefulShutdown && !completeAsap;
				logger.LogInformation("Stopping server...");
				using var cts = new CancellationTokenSource(
					TimeSpan.FromMinutes(
						giveHandlersTimeToWaitAround
							? generalConfigurationOptions!.CurrentValue.ShutdownTimeoutMinutes
							: generalConfigurationOptions!.CurrentValue.RestartTimeoutMinutes));
				var cancellationToken = cts.Token;
				try
				{
					ValueTask eventsTask;
					lock (restartLock)
						eventsTask = ValueTaskExtensions.WhenAll(
							restartHandlers
								.Select(
									x => x.HandleRestart(newVersion, giveHandlersTimeToWaitAround, cancellationToken))
								.ToList());

					logger.LogTrace("Joining restart handlers...");
					await eventsTask;
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

			StopServerImmediate();
		}

		/// <summary>
		/// Event handler for the <see cref="updatePath"/>'s <see cref="FileSystemWatcher"/>. Triggers shutdown if requested by host watchdog.
		/// </summary>
		/// <param name="sender">The <see cref="object"/> that sent the event.</param>
		/// <param name="eventArgs">The <see cref="FileSystemEventArgs"/>.</param>
		async void WatchForShutdownFileCreation(object sender, FileSystemEventArgs eventArgs)
		{
			logger?.LogTrace("FileSystemWatcher triggered.");

			// DCT: None available
			if (eventArgs.FullPath == ioManager.ResolvePath(updatePath!) && await ioManager.FileExists(eventArgs.FullPath, CancellationToken.None))
			{
				logger?.LogInformation("Host watchdog appears to be requesting server termination!");
				lock (restartLock)
				{
					if (!UpdateInProgress)
					{
						StopServerImmediate();
						return;
					}

					terminateIfUpdateFails = true;
				}

				logger?.LogInformation("An update is in progress, we will wait for that to complete...");
			}
		}

		/// <summary>
		/// Fires off the <see cref="cancellationTokenSource"/> without any checks, shutting down everything.
		/// </summary>
		void StopServerImmediate()
		{
			shutdownInProgress = true;
			logger!.LogDebug("Stopping host...");
			cancellationTokenSource!.Cancel();
		}
	}
}
