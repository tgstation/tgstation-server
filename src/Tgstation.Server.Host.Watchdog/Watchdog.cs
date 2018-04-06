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
		/// Construct a <see cref="Watchdog"/>
		/// </summary>
		/// <param name="initialServerFactory">The value of <see cref="initialServerFactory"/></param>
		/// <param name="activeAssemblyDeleter">The value of <see cref="activeAssemblyDeleter"/></param>
		public Watchdog(IServerFactory initialServerFactory, IActiveAssemblyDeleter activeAssemblyDeleter)
		{
			this.initialServerFactory = initialServerFactory ?? throw new ArgumentNullException(nameof(initialServerFactory));
			this.activeAssemblyDeleter = activeAssemblyDeleter ?? throw new ArgumentNullException(nameof(activeAssemblyDeleter));
		}

		/// <inheritdoc />
		public async Task RunAsync(string[] args, CancellationToken cancellationToken)
		{
			//first run the host we started with
			var serverFactory = initialServerFactory;
			var assembly = serverFactory.GetType().Assembly;
			do
			{
				string updatePath;
				using (var server = serverFactory.CreateServer())
				{
					serverFactory = null;
					await server.RunAsync(args, cancellationToken).ConfigureAwait(false);
					updatePath = server.UpdatePath;
				}

				if (updatePath == null)
					break;
				
				activeAssemblyDeleter.DeleteActiveAssembly(assembly);
				File.Move(updatePath, assembly.Location);
				serverFactory = new IsolatedServerFactory(assembly.Location);
			}
			while (!cancellationToken.IsCancellationRequested);
		}
	}
}
