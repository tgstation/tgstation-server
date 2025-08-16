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
using Tgstation.Server.Host.System;

namespace Tgstation.Server.Host.Utils
{
	/// <inheritdoc />
	sealed class PortAllocator : IPortAllocator, IDisposable
	{
		/// <summary>
		/// The <see cref="IServerPortProvider"/> for the <see cref="PortAllocator"/>.
		/// </summary>
		readonly IServerPortProvider serverPortProvider;

		/// <summary>
		/// The <see cref="IDatabaseContext"/> for the <see cref="PortAllocator"/>.
		/// </summary>
		readonly IDatabaseContextFactory databaseContextFactory;

		/// <summary>
		/// The <see cref="IPlatformIdentifier"/> for the <see cref="PortAllocator"/>.
		/// </summary>
		readonly IPlatformIdentifier platformIdentifier;

		/// <summary>
		/// The <see cref="ILogger"/> for the <see cref="PortAllocator"/>.
		/// </summary>
		readonly ILogger<PortAllocator> logger;

		/// <summary>
		/// The <see cref="SwarmConfiguration"/> for the <see cref="PortAllocator"/>.
		/// </summary>
		readonly SwarmConfiguration swarmConfiguration;

		/// <summary>
		/// The <see cref="SemaphoreSlim"/> used to serialized port requisition requests.
		/// </summary>
		readonly SemaphoreSlim allocatorLock;

		/// <summary>
		/// Initializes a new instance of the <see cref="PortAllocator"/> class.
		/// </summary>
		/// <param name="serverPortProvider">The value of <see cref="serverPortProvider"/>.</param>
		/// <param name="databaseContextFactory">The value of <see cref="databaseContextFactory"/>.</param>
		/// <param name="platformIdentifier">The value of <see cref="platformIdentifier"/>.</param>
		/// <param name="swarmConfigurationOptions">The <see cref="IOptions{TOptions}"/> containing the value of <see cref="swarmConfiguration"/>.</param>
		/// <param name="logger">The value of <see cref="logger"/>.</param>
		public PortAllocator(
			IServerPortProvider serverPortProvider,
			IDatabaseContextFactory databaseContextFactory,
			IPlatformIdentifier platformIdentifier,
			IOptions<SwarmConfiguration> swarmConfigurationOptions,
			ILogger<PortAllocator> logger)
		{
			this.serverPortProvider = serverPortProvider ?? throw new ArgumentNullException(nameof(serverPortProvider));
			this.databaseContextFactory = databaseContextFactory ?? throw new ArgumentNullException(nameof(databaseContextFactory));
			this.platformIdentifier = platformIdentifier ?? throw new ArgumentNullException(nameof(platformIdentifier));
			swarmConfiguration = swarmConfigurationOptions?.Value ?? throw new ArgumentNullException(nameof(swarmConfigurationOptions));
			this.logger = logger ?? throw new ArgumentNullException(nameof(logger));

			allocatorLock = new SemaphoreSlim(1);
		}

		/// <inheritdoc />
		public void Dispose() => allocatorLock.Dispose();

		/// <inheritdoc />
		public async ValueTask<ushort?> GetAvailablePort(ushort basePort, bool checkOne, CancellationToken cancellationToken)
		{
			ushort? result = null;
			using (await SemaphoreSlimContext.Lock(allocatorLock, cancellationToken))
				await databaseContextFactory.UseContext(
					async databaseContext => result = await GetAvailablePort(databaseContext, basePort, checkOne, cancellationToken));
			return result;
		}

		/// <summary>
		/// Gets a port not currently in use by TGS.
		/// </summary>
		/// <param name="databaseContext">The <see cref="IDatabaseContext"/> to use.</param>
		/// <param name="basePort">The port to check first. Will not allocate a port lower than this.</param>
		/// <param name="checkOne">If only <paramref name="basePort"/> should be checked and no others.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in the first available port on success, <see langword="null"/> on failure.</returns>
		async ValueTask<ushort?> GetAvailablePort(IDatabaseContext databaseContext, ushort basePort, bool checkOne, CancellationToken cancellationToken)
		{
			logger.LogTrace("Port allocation >= {basePort} requested...", basePort);
			var ddPorts = await databaseContext
				.DreamDaemonSettings
				.Where(x => x.Instance!.SwarmIdentifer == swarmConfiguration.Identifier)
				.Select(x => new
				{
					Port = x.Port!.Value,
					x.InstanceId,
				})
				.ToListAsync(cancellationToken);

			var dmPorts = await databaseContext
				.DreamMakerSettings
				.Where(x => x.Instance!.SwarmIdentifer == swarmConfiguration.Identifier)
				.Select(x => new
				{
					ApiValidationPort = x.ApiValidationPort!.Value,
					x.InstanceId,
				})
				.ToListAsync(cancellationToken);

			var exceptions = new List<Exception>();
			ushort port = 0;
			try
			{
				for (port = basePort; port < ushort.MaxValue; ++port)
				{
					if (checkOne && port != basePort)
						break;

					if (port == serverPortProvider.HttpApiPort)
					{
						logger.LogWarning("Cannot allocate port {port} as it is the TGS API port!", port);
						continue;
					}

					var reservedGamePortData = ddPorts.Where(data => data.Port == port).ToList();
					if (reservedGamePortData.Count > 0)
					{
						logger.LogWarning(
							"Cannot allocate port {port} as it in use by the game server of instance(s): {instanceId}!",
							port,
							String.Join(
								", ",
								reservedGamePortData.Select(data => data.InstanceId)));
						continue;
					}

					var reservedApiValidationPortData = dmPorts.Where(data => data.ApiValidationPort == port).ToList();
					if (reservedApiValidationPortData.Count > 0)
					{
						logger.LogWarning(
							"Cannot allocate port {port} as it in use by the API validation server of instance(s): {instanceId}!",
							port,
							String.Join(
								", ",
								reservedApiValidationPortData.Select(data => data.InstanceId)));
						continue;
					}

					try
					{
						SocketExtensions.BindTest(platformIdentifier, port, false, true);
						SocketExtensions.BindTest(platformIdentifier, port, false, false);
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
