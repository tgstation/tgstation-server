using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Host.Startup;

namespace Tgstation.Server.Host.Watchdog
{
	/// <inheritdoc />
	sealed class Watchdog : IWatchdog
	{

		/// <summary>
		/// The <see cref="IActiveLibraryDeleter"/> for the <see cref="Watchdog"/>
		/// </summary>
		readonly IActiveLibraryDeleter activeLibraryDeleter;

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
		/// <param name="activeLibraryDeleter">The value of <see cref="activeLibraryDeleter"/></param>
		/// <param name="isolatedAssemblyLoader">The value of <see cref="isolatedAssemblyLoader"/></param>
		/// <param name="logger">The value of <see cref="logger"/></param>
		public Watchdog(IActiveLibraryDeleter activeLibraryDeleter, IIsolatedAssemblyContextFactory isolatedAssemblyLoader, ILogger<Watchdog> logger)
		{
			this.activeLibraryDeleter = activeLibraryDeleter ?? throw new ArgumentNullException(nameof(activeLibraryDeleter));
			this.isolatedAssemblyLoader = isolatedAssemblyLoader ?? throw new ArgumentNullException(nameof(isolatedAssemblyLoader));
			this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
		}

		/// <inheritdoc />
		public async Task RunAsync(string[] args, CancellationToken cancellationToken)
		{
			const string DefaultAssemblyPath = "Default";

			var assemblyStoragePath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "lib");
			var assemblyName = String.Join(".", nameof(Tgstation), nameof(Server), nameof(Host), "dll");

			logger.LogInformation("Host watchdog starting...");

			var nextAssemblyPath = Path.GetFullPath(Path.Combine(assemblyStoragePath, DefaultAssemblyPath));
			string lastAssemblyPath = null;
			try
			{
				while (!cancellationToken.IsCancellationRequested)
					using (logger.BeginScope("Host invocation"))
					{
						logger.LogTrace("Atttempting to create new server factory...");
						Guid updateGuid;
						{   //forces serverFactory out of the picture once the scope ends
							var serverFactory = isolatedAssemblyLoader.CreateIsolatedServerFactory(Path.Combine(nextAssemblyPath, assemblyName));
							using (var server = serverFactory.CreateServer(args, assemblyStoragePath))
							{
								logger.LogTrace("Running server...");
								await server.RunAsync(cancellationToken).ConfigureAwait(false);
								logger.LogInformation("Active host exited.");

								if (!server.UpdateGuid.HasValue)
									break;
								updateGuid = server.UpdateGuid.Value;
							}
						}

						logger.LogInformation("Update path is set to \"{0}\", attempting host assembly hotswap...", updateGuid);
						GC.Collect(Int32.MaxValue, GCCollectionMode.Forced, true, true);

						activeLibraryDeleter.DeleteActiveLibrary(nextAssemblyPath);

						nextAssemblyPath = Path.Combine(assemblyStoragePath, updateGuid.ToString());
					}
			}
			catch (OperationCanceledException)
			{
				logger.LogDebug("Exiting due to cancellation...");
			}
			catch (Exception e)
			{
				logger.LogCritical("Error running host assembly! Exception: {0}", e);
				nextAssemblyPath = lastAssemblyPath ?? DefaultAssemblyPath;	//don't wanna save a critfailed assembly
			}
			if (nextAssemblyPath != DefaultAssemblyPath)
			{
				logger.LogInformation("Setting next default host assembly path to {0}...", nextAssemblyPath);
			}
			logger.LogInformation("Host watchdog exiting...");
		}
	}
}
