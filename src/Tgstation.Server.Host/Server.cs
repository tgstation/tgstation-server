using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Host.Core;
using Tgstation.Server.Host.IO;

namespace Tgstation.Server.Host
{
	/// <inheritdoc />
	sealed class Server : IServer, IServerUpdater
	{
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

			semaphore = new SemaphoreSlim(1);
			updated = false;
		}

		/// <inheritdoc />
		public void Dispose() => semaphore.Dispose();

		/// <inheritdoc />
        [ExcludeFromCodeCoverage]
		public async Task RunAsync(CancellationToken cancellationToken)
		{
			using (cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
			using (var fsWatcher = updatePath != null ? new FileSystemWatcher(Path.GetDirectoryName(updatePath)) : null)
			{
				if (fsWatcher != null)
				{
					fsWatcher.Created += (a, b) =>
					{
						if (b.Name == updatePath && File.Exists(b.FullPath))
							cancellationTokenSource.Cancel();
					};
					fsWatcher.EnableRaisingEvents = true;
				}
				using (var webHost = webHostBuilder
					.UseStartup<Application>()
					.ConfigureServices((serviceCollection) => serviceCollection.AddSingleton<IServerUpdater>(this))
					.Build()
				)
					await webHost.RunAsync(cancellationTokenSource.Token).ConfigureAwait(false);
			}
		}

		/// <inheritdoc />
		public async Task<bool> ApplyUpdate(byte[] updateZipData, IIOManager ioManager, CancellationToken cancellationToken)
		{
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
				catch
				{
					try
					{
						//important to not leave this directory around if possible
						await ioManager.DeleteDirectory(updatePath, default).ConfigureAwait(false);
					}
					catch { }
					updated = false;
					throw;
				}
				Restart();
				return true;
			}
		}

		/// <inheritdoc />
		public void RegisterForUpdate(Action action)
		{
			if (cancellationTokenSource == null)
				throw new InvalidOperationException("Tried to register an update action on a non-running Server!");
			cancellationTokenSource.Token.Register(action);
		}

		/// <inheritdoc />
		public void Restart()
		{
			if (cancellationTokenSource == null)
				throw new InvalidOperationException("Tried to restart a non-running Server!");
			cancellationTokenSource.Cancel();
		}
	}
}
