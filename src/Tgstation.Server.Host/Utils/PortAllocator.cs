using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Tgstation.Server.Host.Configuration;
using Tgstation.Server.Host.Core;
using Tgstation.Server.Host.Database;
using Tgstation.Server.Host.Extensions;

namespace Tgstation.Server.Host.Utils
{
	/// <inheritdoc />
	sealed class PortAllocator : IPortAllocator
	{
		/// <summary>
		/// The <see cref="IServerPortProvider"/> for the <see cref="PortAllocator"/>.
		/// </summary>
		readonly IServerPortProvider serverPortProvider;

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
		/// <param name="serverPortProvider">The value of <see cref="serverPortProvider"/>.</param>
		/// <param name="databaseContext">The value of <see cref="databaseContext"/>.</param>
		/// <param name="swarmConfigurationOptions">The <see cref="IOptions{TOptions}"/> containing the value of <see cref="swarmConfiguration"/>.</param>
		/// <param name="logger">The value of <see cref="logger"/>.</param>
		public PortAllocator(
			IServerPortProvider serverPortProvider,
			IDatabaseContext databaseContext,
			IOptions<SwarmConfiguration> swarmConfigurationOptions,
			ILogger<PortAllocator> logger)
		{
			this.serverPortProvider = serverPortProvider ?? throw new ArgumentNullException(nameof(serverPortProvider));
			this.databaseContext = databaseContext ?? throw new ArgumentNullException(nameof(databaseContext));
			swarmConfiguration = swarmConfigurationOptions?.Value ?? throw new ArgumentNullException(nameof(swarmConfigurationOptions));
			this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
		}

		/// <inheritdoc />
		public async Task<ushort?> GetAvailablePort(ushort basePort, bool checkOne, CancellationToken cancellationToken)
		{
			logger.LogTrace("Port allocation >= {basePort} requested...", basePort);

			var ddPorts = await databaseContext
				.DreamDaemonSettings
				.AsQueryable()
				.Where(x => x.Instance.SwarmIdentifer == swarmConfiguration.Identifier)
				.Select(x => x.Port)
				.ToListAsync(cancellationToken);

			var dmPorts = await databaseContext
				.DreamMakerSettings
				.AsQueryable()
				.Where(x => x.Instance.SwarmIdentifer == swarmConfiguration.Identifier)
				.Select(x => x.ApiValidationPort)
				.ToListAsync(cancellationToken);

			var exceptions = new List<Exception>();
			ushort port = 0;
			try
			{
				for (port = basePort; port < ushort.MaxValue; ++port)
				{
					if (checkOne && port != basePort)
						break;

					if (port == serverPortProvider.HttpApiPort
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

					logger.LogInformation("Allocated port {port}", port);
					return port;
				}

				logger.LogWarning("Unable to allocate port >= {basePort}!", basePort);
				return null;
			}
			finally
			{
				if (port != basePort)
				{
					logger.LogDebug(
						exceptions.Count == 1
							? exceptions.First()
							: new AggregateException(exceptions),
						"Failed to allocate ports {basePort}-{lastCheckedPort}!",
						basePort,
						port - 1);
				}
			}
		}
	}
}
