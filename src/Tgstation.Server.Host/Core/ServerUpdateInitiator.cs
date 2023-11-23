using System;
using System.Threading;
using System.Threading.Tasks;

using Tgstation.Server.Host.IO;
using Tgstation.Server.Host.Swarm;

#nullable disable

namespace Tgstation.Server.Host.Core
{
	/// <inheritdoc />
	sealed class ServerUpdateInitiator : IServerUpdateInitiator
	{
		/// <summary>
		/// The <see cref="ISwarmService"/> for the <see cref="ServerUpdateInitiator"/>.
		/// </summary>
		readonly ISwarmService swarmService;

		/// <summary>
		/// The <see cref="IServerUpdater"/> for the <see cref="ServerUpdateInitiator"/>.
		/// </summary>
		readonly IServerUpdater serverUpdater;

		/// <summary>
		/// Initializes a new instance of the <see cref="ServerUpdateInitiator"/> class.
		/// </summary>
		/// <param name="swarmService">The value of <see cref="swarmService"/>.</param>
		/// <param name="serverUpdater">The value of <see cref="serverUpdater"/>.</param>
		public ServerUpdateInitiator(ISwarmService swarmService, IServerUpdater serverUpdater)
		{
			this.swarmService = swarmService ?? throw new ArgumentNullException(nameof(swarmService));
			this.serverUpdater = serverUpdater ?? throw new ArgumentNullException(nameof(serverUpdater));
		}

		/// <inheritdoc />
		public ValueTask<ServerUpdateResult> InitiateUpdate(IFileStreamProvider fileStreamProvider, Version version, CancellationToken cancellationToken)
			=> serverUpdater.BeginUpdate(swarmService, fileStreamProvider, version, cancellationToken);
	}
}
