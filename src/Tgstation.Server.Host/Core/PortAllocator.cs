using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Host.Database;
using Tgstation.Server.Host.Extensions;

namespace Tgstation.Server.Host.Core
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
		/// Initializes a new instance of the <see cref="PortAllocator"/> <see langword="class"/>.
		/// </summary>
		/// <param name="serverPortProvider">The value of <see cref="serverPortProvider"/>.</param>
		/// <param name="databaseContext">The value of <see cref="databaseContext"/>.</param>
		/// <param name="logger">The value of <see cref="logger"/>.</param>
		public PortAllocator(IServerPortProvider serverPortProvider, IDatabaseContext databaseContext, ILogger<PortAllocator> logger)
		{
			this.serverPortProvider = serverPortProvider ?? throw new ArgumentNullException(nameof(serverPortProvider));
			this.databaseContext = databaseContext ?? throw new ArgumentNullException(nameof(databaseContext));
			this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
		}

		/// <inheritdoc />
		public async Task<ushort?> GetAvailablePort(ushort basePort, bool checkOne, CancellationToken cancellationToken)
		{
			logger.LogTrace("Port allocation >= {0} requested...", basePort);

			var ddPorts = await databaseContext
				.DreamDaemonSettings
				.AsQueryable()
				.Select(x => x.Port)
				.ToListAsync(cancellationToken)
				.ConfigureAwait(false);

			var dmPorts = await databaseContext
				.DreamMakerSettings
				.AsQueryable()
				.Select(x => x.ApiValidationPort)
				.ToListAsync(cancellationToken)
				.ConfigureAwait(false);

			var exceptions = new List<Exception>();
			ushort I = 0;
			try
			{
				for (I = basePort; I < UInt16.MaxValue; ++I)
				{
					if (checkOne && I != basePort)
						break;

					if (I == serverPortProvider.HttpApiPort
						|| ddPorts.Contains(I)
						|| dmPorts.Contains(I))
						continue;

					try
					{
						SocketExtensions.BindTest(I, false);
					}
					catch (Exception ex)
					{
						exceptions.Add(ex);
						continue;
					}

					logger.LogInformation("Allocated port {0}", I);
					return I;
				}

				logger.LogWarning("Unable to allocate port >= {0}!", basePort);
				return null;
			}
			finally
			{
				logger.LogDebug(new AggregateException(exceptions), "Failed to allocate ports {0}-{1}!", basePort, I - 1);
			}
		}
	}
}
