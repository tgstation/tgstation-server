using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Host.Startup;

namespace Tgstation.Server.Host.Watchdog
{
	/// <inheritdoc />
	sealed class Watchdog : IWatchdog
	{
		/// <summary>
		/// The initial <see cref="IServerFactory"/> for the <see cref="Watchdog"/>
		/// </summary>
		readonly IServerFactory initialServerFactory;

		/// <summary>
		/// The <see cref="IActiveAssemblyDeleter"/> for the <see cref="Watchdog"/>
		/// </summary>
		readonly IActiveAssemblyDeleter activeAssemblyDeleter;

		/// <summary>
		/// The <see cref="IIsolatedAssemblyContextFactory"/> for the <see cref="Watchdog"/>
		/// </summary>
		readonly IIsolatedAssemblyContextFactory isolatedAssemblyLoader;

		/// <summary>
		/// Construct a <see cref="Watchdog"/>
		/// </summary>
		/// <param name="initialServerFactory">The value of <see cref="initialServerFactory"/></param>
		/// <param name="activeAssemblyDeleter">The value of <see cref="activeAssemblyDeleter"/></param>
		/// <param name="isolatedAssemblyLoader">The value of <see cref="isolatedAssemblyLoader"/></param>
		public Watchdog(IServerFactory initialServerFactory, IActiveAssemblyDeleter activeAssemblyDeleter, IIsolatedAssemblyContextFactory isolatedAssemblyLoader)
		{
			this.initialServerFactory = initialServerFactory ?? throw new ArgumentNullException(nameof(initialServerFactory));
			this.activeAssemblyDeleter = activeAssemblyDeleter ?? throw new ArgumentNullException(nameof(activeAssemblyDeleter));
			this.isolatedAssemblyLoader = isolatedAssemblyLoader ?? throw new ArgumentNullException(nameof(isolatedAssemblyLoader));
		}

		/// <inheritdoc />
		public async Task RunAsync(string[] args, CancellationToken cancellationToken)
		{
			//first run the host we started with
			var serverFactory = initialServerFactory;
			var assemblyPath = serverFactory.GetType().Assembly.Location;
            do
            {
                var server = serverFactory.CreateServer(args);
                await server.RunAsync(cancellationToken).ConfigureAwait(false);

                if (server.UpdatePath == null)
                    break;

                activeAssemblyDeleter.DeleteActiveAssembly(assemblyPath);
                File.Move(server.UpdatePath, assemblyPath);
                serverFactory = isolatedAssemblyLoader.CreateIsolatedServerFactory(assemblyPath);
            }
            while (!cancellationToken.IsCancellationRequested);
		}
	}
}
