using Microsoft.Extensions.Logging;
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
		/// The <see cref="ILogger"/> for the <see cref="Watchdog"/>
		/// </summary>
		readonly ILogger<Watchdog> logger;

		/// <summary>
		/// Construct a <see cref="Watchdog"/>
		/// </summary>
		/// <param name="initialServerFactory">The value of <see cref="initialServerFactory"/></param>
		/// <param name="activeAssemblyDeleter">The value of <see cref="activeAssemblyDeleter"/></param>
		/// <param name="isolatedAssemblyLoader">The value of <see cref="isolatedAssemblyLoader"/></param>
		/// <param name="logger">The value of <see cref="logger"/></param>
		public Watchdog(IServerFactory initialServerFactory, IActiveAssemblyDeleter activeAssemblyDeleter, IIsolatedAssemblyContextFactory isolatedAssemblyLoader, ILogger<Watchdog> logger)
		{
			this.initialServerFactory = initialServerFactory ?? throw new ArgumentNullException(nameof(initialServerFactory));
			this.activeAssemblyDeleter = activeAssemblyDeleter ?? throw new ArgumentNullException(nameof(activeAssemblyDeleter));
			this.isolatedAssemblyLoader = isolatedAssemblyLoader ?? throw new ArgumentNullException(nameof(isolatedAssemblyLoader));
			this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
		}

		/// <inheritdoc />
		public async Task RunAsync(string[] args, CancellationToken cancellationToken)
		{
			logger.LogInformation("Host watchdog starting...");
			try
			{
				//first run the host we started with
				logger.LogTrace("Running with initial server factory...");
				var serverFactory = initialServerFactory;
				logger.LogTrace("Determining location of host assembly...");
				var assemblyPath = serverFactory.GetType().Assembly.Location;
				logger.LogDebug("Path to initial host assembly: {0}", assemblyPath);

				var firstIteration = true;
				do
					using (logger.BeginScope("Host invocation"))
					{
						var server = serverFactory.CreateServer(args);
						logger.LogTrace("Running server...");
						await server.RunAsync(cancellationToken).ConfigureAwait(false);
						logger.LogInformation("Active host exited.");

						if (server.UpdatePath == null)
							break;

						logger.LogInformation("Update path is set to \"{0}\", attempting host assembly hotswap...", server.UpdatePath);
						GC.Collect(Int32.MaxValue, GCCollectionMode.Forced, true, true);
						logger.LogTrace("Deleting old host assembly");
						activeAssemblyDeleter.DeleteActiveAssembly(assemblyPath);
						logger.LogTrace("Moving new host assembly in place...");
						File.Move(server.UpdatePath, assemblyPath);
						logger.LogTrace("Atttempting to create new server factory...");
						serverFactory = isolatedAssemblyLoader.CreateIsolatedServerFactory(assemblyPath);
						firstIteration = false;
					}
				while (!cancellationToken.IsCancellationRequested);
			}
			catch (OperationCanceledException)
			{
				logger.LogDebug("Exiting due to cancellation...");
			}
			catch (Exception e)
			{
				logger.LogCritical("Error running host assembly! Exception: {0}", e);
			}
			logger.LogInformation("Host watchdog exiting...");
		}
	}
}
