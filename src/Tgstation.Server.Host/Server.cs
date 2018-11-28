using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Host.Core;
using Tgstation.Server.Host.IO;

namespace Tgstation.Server.Host
{
	/// <inheritdoc />
#pragma warning disable CA1001 // Types that own disposable fields should be disposable
	sealed class Server : IServer, IServerControl
#pragma warning restore CA1001 // Types that own disposable fields should be disposable
	{
		/// <inheritdoc />
		public bool RestartRequested { get; private set; }

		/// <inheritdoc />
		public bool WatchdogPresent => updatePath != null;

		/// <summary>
		/// The <see cref="IWebHostBuilder"/> for the <see cref="Server"/>
		/// </summary>
		readonly IWebHostBuilder webHostBuilder;

		/// <summary>
		/// The <see cref="IRestartHandler"/>s to run when the <see cref="Server"/> restarts
		/// </summary>
		readonly List<IRestartHandler> restartHandlers;

		/// <summary>
		/// The absolute path to install updates to
		/// </summary>
		readonly string updatePath;

		/// <summary>
		/// The <see cref="ILogger"/> for the <see cref="Server"/>
		/// </summary>
		ILogger<Server> logger;

		/// <summary>
		/// The <see cref="cancellationTokenSource"/> for the <see cref="Server"/>
		/// </summary>
		CancellationTokenSource cancellationTokenSource;

		/// <summary>
		/// If a server update has been or is being applied
		/// </summary>
		bool updating;

		/// <summary>
		/// Construct a <see cref="Server"/>
		/// </summary>
		/// <param name="webHostBuilder">The value of <see cref="webHostBuilder"/></param>
		/// <param name="updatePath">The value of <see cref="updatePath"/></param>
		public Server(IWebHostBuilder webHostBuilder, string updatePath)
		{
			this.webHostBuilder = webHostBuilder ?? throw new ArgumentNullException(nameof(webHostBuilder));
			this.updatePath = updatePath;

			restartHandlers = new List<IRestartHandler>();
		}

		/// <summary>
		/// Throws an <see cref="InvalidOperationException"/> if the <see cref="IServerControl"/> cannot be used
		/// </summary>
		/// <param name="checkWatchdog">If <see cref="WatchdogPresent"/> should be checked</param>
		void CheckSanity(bool checkWatchdog)
		{
			if (checkWatchdog && !WatchdogPresent)
				throw new InvalidOperationException("Server restarts are not supported");

			if (cancellationTokenSource == null || logger == null)
				throw new InvalidOperationException("Tried to control a non-running Server!");
		}

		/// <inheritdoc />
		public async Task RunAsync(CancellationToken cancellationToken)
		{
			using (cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
			using (var fsWatcher = updatePath != null ? new FileSystemWatcher(Path.GetDirectoryName(updatePath)) : null)
			{
				if (fsWatcher != null)
				{
					fsWatcher.Created += (a, b) =>
					{
						if (b.FullPath == updatePath && File.Exists(b.FullPath))
							cancellationTokenSource.Cancel();
					};
					fsWatcher.EnableRaisingEvents = true;
				}

				using (var webHost = webHostBuilder
					.UseStartup<Application>()
					.ConfigureServices(serviceCollection => serviceCollection.AddSingleton<IServerControl>(this))
					.Build())
				{
					logger = webHost.Services.GetRequiredService<ILogger<Server>>();
					await webHost.RunAsync(cancellationTokenSource.Token).ConfigureAwait(false);
				}
			}
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

			lock (this)
			{
				if (updating || RestartRequested)
				{
					logger.LogTrace("Aborted due to concurrency conflict!");
					return false;
				}

				updating = true;
			}

			async void RunUpdate()
			{
				try
				{
					logger.LogInformation("Updating server to version {0} ({1})...", version, updateZipUrl);

					if (cancellationTokenSource == null)
						throw new InvalidOperationException("Tried to update a non-running Server!");
					var cancellationToken = cancellationTokenSource.Token;

					logger.LogTrace("Downloading zip package...");
					var updateZipData = await ioManager.DownloadFile(updateZipUrl, cancellationToken).ConfigureAwait(false);

					try
					{
						logger.LogTrace("Exctracting zip package to {0}...", updatePath);
						await ioManager.ZipToDirectory(updatePath, updateZipData, cancellationToken).ConfigureAwait(false);
					}
					catch (Exception e)
					{
						updating = false;
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

					await Restart(version).ConfigureAwait(false);
				}
				catch (OperationCanceledException)
				{
					logger.LogInformation("Server update cancelled!");
				}
				catch (Exception e)
				{
					logger.LogError("Error updating server! Exception: {0}", e);
				}
				finally
				{
					updating = false;
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

			lock (this)
				if (!RestartRequested)
				{
					logger.LogTrace("Registering restart handler {0}...", handler);
					restartHandlers.Add(handler);
					return new RestartRegistration(() =>
					{
						lock (this)
							if (!RestartRequested)
								restartHandlers.Remove(handler);
					});
				}

			return new RestartRegistration(() => { });
		}

		/// <inheritdoc />
		public Task Restart() => Restart(null);

		/// <summary>
		/// Implements <see cref="Restart()"/>
		/// </summary>
		/// <param name="newVersion">The <see cref="Version"/> of any potential updates being applied</param>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		async Task Restart(Version newVersion)
		{
			CheckSanity(true);

			logger.LogTrace("Begin Restart...");

			lock (this)
			{
				if ((updating && newVersion == null) || RestartRequested)
				{
					logger.LogTrace("Aborted due to concurrency conflict!");
					return;
				}

				RestartRequested = true;
			}

			logger.LogInformation("Restarting server...");

			using (var cts = new CancellationTokenSource())
			{
				logger.LogTrace("Running restart handlers...");
				var cancellationToken = cts.Token;
				var eventsTask = Task.WhenAll(restartHandlers.Select(x => x.HandleRestart(newVersion, cancellationToken)).ToList());

				// YA GOT 10 SECONDS
				var expiryTask = Task.Delay(TimeSpan.FromSeconds(10));
				await Task.WhenAny(eventsTask, expiryTask).ConfigureAwait(false);
				logger.LogTrace("Joining restart handlers...");
				cts.Cancel();
				try
				{
					await eventsTask.ConfigureAwait(false);
				}
				catch (OperationCanceledException) { }
				catch (Exception e)
				{
					logger.LogError("Restart handlers error! Exception: {0}", e);
				}
			}

			logger.LogTrace("Stopping host...");
			cancellationTokenSource.Cancel();
		}
	}
}
