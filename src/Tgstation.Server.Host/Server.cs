using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Host.Core;
using Tgstation.Server.Host.IO;
using Tgstation.Server.Host.Startup;

namespace Tgstation.Server.Host
{
	/// <inheritdoc />
	sealed class Server : IServer, IServerUpdater
	{
		/// <inheritdoc />
		public Guid? UpdateGuid { get; private set; }

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
			this.updatePath = updatePath ?? throw new ArgumentNullException(nameof(updatePath));

			semaphore = new SemaphoreSlim(1);
		}

		/// <inheritdoc />
		public void Dispose() => semaphore.Dispose();

		/// <inheritdoc />
        [ExcludeFromCodeCoverage]
		public async Task RunAsync(CancellationToken cancellationToken)
		{
			using (cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
			using (var webHost = webHostBuilder
				.UseStartup<Application>()
				.ConfigureServices((serviceCollection) => serviceCollection.AddSingleton<IServerUpdater>(this))
				.Build()
			)
				await webHost.RunAsync(cancellationTokenSource.Token).ConfigureAwait(false);
		}

		/// <inheritdoc />
		public async Task ApplyUpdate(byte[] updateZipData, IIOManager ioManager, CancellationToken cancellationToken)
		{
			using(await SemaphoreSlimContext.Lock(semaphore, cancellationToken).ConfigureAwait(false))
			{
				if (UpdateGuid != null)
					throw new InvalidOperationException("ApplyUpdate has already been called!");
				UpdateGuid = Guid.NewGuid();
				try
				{
					await ioManager.ZipToDirectory(updatePath, updateZipData, cancellationToken).ConfigureAwait(false);
				}
				catch
				{
					UpdateGuid = null;
					throw;
				}
				Restart();
			}
		}

		/// <inheritdoc />
		public void RegisterForUpdate(Action action) => cancellationTokenSource.Token.Register(action);

		/// <inheritdoc />
		public void Restart() => cancellationTokenSource.Cancel();
	}
}
