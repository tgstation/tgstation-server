using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Tgstation.Server.Host.Configuration;
using Tgstation.Server.Host.Database;
using Tgstation.Server.Host.Extensions;

namespace Tgstation.Server.Host.Core
{
	/// <inheritdoc />
	sealed class PortAllocator : IPortAllocator
	{
		/// <summary>
		/// The <see cref="IServerAddressProvider"/> for the <see cref="PortAllocator"/>.
		/// </summary>
		readonly IServerAddressProvider serverAddressProvider;

		/// <summary>
		/// The <see cref="IDatabaseContext"/> for the <see cref="PortAllocator"/>.
		/// </summary>
		readonly IDatabaseContext databaseContext;

		/// <summary>
		/// The <see cref="ILogger"/> for the <see cref="PortAllocator"/>.
		/// </summary>
		readonly ILogger<PortAllocator> logger;

		/// <summary>
		/// The <see cref="SwarmConfiguration"/> for the <see cref="PortAllocator"/>.
		/// </summary>
		readonly SwarmConfiguration swarmConfiguration;

		/// <summary>
		/// Initializes a new instance of the <see cref="PortAllocator"/> class.
		/// </summary>
		/// <param name="serverAddressProvider">The value of <see cref="serverAddressProvider"/>.</param>
		/// <param name="databaseContext">The value of <see cref="databaseContext"/>.</param>
		/// <param name="swarmConfigurationOptions">The <see cref="IOptions{TOptions}"/> containing the value of <see cref="swarmConfiguration"/>.</param>
		/// <param name="logger">The value of <see cref="logger"/>.</param>
		public PortAllocator(
			IServerAddressProvider serverAddressProvider,
			IDatabaseContext databaseContext,
			IOptions<SwarmConfiguration> swarmConfigurationOptions,
			ILogger<PortAllocator> logger)
		{
			this.serverAddressProvider = serverAddressProvider ?? throw new ArgumentNullException(nameof(serverAddressProvider));
			this.databaseContext = databaseContext ?? throw new ArgumentNullException(nameof(databaseContext));
			swarmConfiguration = swarmConfigurationOptions?.Value ?? throw new ArgumentNullException(nameof(swarmConfigurationOptions));
			this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
		}

		/// <inheritdoc />
		public async Task<ushort?> GetAvailablePort(ushort basePort, bool checkOne, CancellationToken cancellationToken)
		{
			logger.LogTrace("Port allocation >= {0} requested...", basePort);

			var ddPorts = await databaseContext
				.DreamDaemonSettings
				.AsQueryable()
				.Where(x => x.Instance.SwarmIdentifer == swarmConfiguration.Identifier)
				.Select(x => x.Port)
				.ToListAsync(cancellationToken)
				.ConfigureAwait(false);

			var dmPorts = await databaseContext
				.DreamMakerSettings
				.AsQueryable()
				.Where(x => x.Instance.SwarmIdentifer == swarmConfiguration.Identifier)
				.Select(x => x.ApiValidationPort)
				.ToListAsync(cancellationToken)
				.ConfigureAwait(false);

			var exceptions = new List<Exception>();
			ushort port = 0;
			try
			{
				for (port = basePort; port < UInt16.MaxValue; ++port)
				{
					if (checkOne && port != basePort)
						break;

					if (port == serverAddressProvider.AddressEndPoint.Port
						|| ddPorts.Contains(port)
						|| dmPorts.Contains(port))
						continue;

					try
					{
						SocketExtensions.BindTest(port, false);
					}
					catch (Exception ex)
					{
						exceptions.Add(ex);
						continue;
					}

					logger.LogInformation("Allocated port {0}", port);
					return port;
				}

				logger.LogWarning("Unable to allocate port >= {0}!", basePort);
				return null;
			}
			finally
			{
				logger.LogDebug(new AggregateException(exceptions), "Failed to allocate ports {0}-{1}!", basePort, port - 1);
			}
		}
	}
}
