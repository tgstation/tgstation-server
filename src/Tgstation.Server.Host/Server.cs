using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
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
	sealed class Server : IServer, IServerControl
	{
		/// <inheritdoc />
		public bool RestartRequested { get; private set; }

		/// <summary>
		/// The <see cref="IWebHostBuilder"/> for the <see cref="Server"/>
		/// </summary>
		readonly IWebHostBuilder webHostBuilder;

		/// <summary>
		/// The <see cref="SemaphoreSlim"/> for the <see cref="Server"/>
		/// </summary>
		readonly SemaphoreSlim semaphore;

		/// <summary>
		/// The absolute path to install updates to
		/// </summary>
		readonly string updatePath;
		
		/// <summary>
		/// The <see cref="IRestartHandler"/>s to run when the <see cref="Server"/> restarts
		/// </summary>
		readonly List<IRestartHandler> restartHandlers;

		/// <summary>
		/// If a server update has been applied
		/// </summary>
		bool updated;

		/// <summary>
		/// The <see cref="cancellationTokenSource"/> for the <see cref="Server"/>
		/// </summary>
		CancellationTokenSource cancellationTokenSource;

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
			semaphore = new SemaphoreSlim(1);
			updated = false;
			RestartRequested = false;
		}

		/// <inheritdoc />
		public void Dispose() => semaphore.Dispose();

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
					.ConfigureServices((serviceCollection) => serviceCollection.AddSingleton<IServerControl>(this))
					.Build()
				)
					await webHost.RunAsync(cancellationTokenSource.Token).ConfigureAwait(false);
			}
		}

		/// <inheritdoc />
		public async Task<bool> ApplyUpdate(Version version, byte[] updateZipData, IIOManager ioManager, CancellationToken cancellationToken)
		{
			if (version == null)
				throw new ArgumentNullException(nameof(version));
			if (updateZipData == null)
				throw new ArgumentNullException(nameof(updateZipData));
			if (ioManager == null)
				throw new ArgumentNullException(nameof(ioManager));

			if (updatePath == null)
				return false;
			using (await SemaphoreSlimContext.Lock(semaphore, cancellationToken).ConfigureAwait(false))
			{
				if (updated)
					throw new InvalidOperationException("ApplyUpdate has already been called!");
				updated = true;
				try
				{
					await ioManager.ZipToDirectory(updatePath, updateZipData, cancellationToken).ConfigureAwait(false);
				}
				catch (Exception e)
				{
					updated = false;
					try
					{
						//important to not leave this directory around if possible
						await ioManager.DeleteDirectory(updatePath, default).ConfigureAwait(false);
					}
					catch (Exception e2)
					{
						throw new AggregateException(e, e2);
					}
					throw;
				}
				await Restart(version).ConfigureAwait(false);
				return true;
			}
		}

		/// <inheritdoc />
		public IRestartRegistration RegisterForRestart(IRestartHandler handler)
		{
			if (handler == null)
				throw new ArgumentNullException(nameof(handler));
			if (cancellationTokenSource == null)
				throw new InvalidOperationException("Tried to register an update action on a non-running Server!");
			lock (this)
				if (!RestartRequested)
				{
					restartHandlers.Add(handler);
					return new RestartRegistration(() =>
					{
						lock (this)
							if(!RestartRequested)
								restartHandlers.Remove(handler);
					});
				}
			return new RestartRegistration(() => { });
		}

		/// <inheritdoc />
		public Task<bool> Restart() => Restart(null);

		/// <summary>
		/// Implements <see cref="Restart()"/>
		/// </summary>
		/// <param name="newVersion">The <see cref="Version"/> of any potential updates being applied</param>
		/// <returns></returns>
		async Task<bool> Restart(Version newVersion)
		{
			if (updatePath == null)
				return false;
			if (cancellationTokenSource == null)
				throw new InvalidOperationException("Tried to restart a non-running Server!");
			lock (this)
			{
				if (RestartRequested)
					return true;
				RestartRequested = true;
			}

			using (var cts = new CancellationTokenSource())
			{
				var cancellationToken = cts.Token;
				var eventsTask = Task.WhenAll(restartHandlers.Select(x => x.HandleRestart(newVersion, cancellationToken)).ToList());
				//YA GOT 10 SECONDS
				var expiryTask = Task.Delay(TimeSpan.FromSeconds(10));
				await Task.WhenAny(eventsTask, expiryTask).ConfigureAwait(false);
				cts.Cancel();
				try
				{
					await eventsTask.ConfigureAwait(false);
				}
				catch (OperationCanceledException) { }
			}

			cancellationTokenSource.Cancel();
			return true;
		}
	}
}
