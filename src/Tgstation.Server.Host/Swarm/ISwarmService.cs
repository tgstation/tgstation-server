using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Tgstation.Server.Api.Models.Internal;
using Tgstation.Server.Host.IO;

namespace Tgstation.Server.Host.Swarm
{
	/// <summary>
	/// Used for swarm operations. Functions may be no-op based on configuration.
	/// </summary>
	public interface ISwarmService : ISwarmUpdateAborter
	{
		/// <summary>
		/// Gets a value indicating if the expected amount of nodes are connected to the swarm.
		/// </summary>
		bool ExpectedNumberOfNodesConnected { get; }

		/// <summary>
		/// Signal to the swarm that an update is requested.
		/// </summary>
		/// <param name="fileStreamProvider">The <see cref="ISeekableFileStreamProvider"/> to relay to other nodes.</param>
		/// <param name="version">The <see cref="Version"/> to update to.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in <see langword="true"/> if the update should proceed, <see langword="false"/> otherwise.</returns>
		ValueTask<SwarmPrepareResult> PrepareUpdate(ISeekableFileStreamProvider fileStreamProvider, Version version, CancellationToken cancellationToken);

		/// <summary>
		/// Signal to the swarm that an update is ready to be applied.
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in the <see cref="SwarmCommitResult"/>.</returns>
		ValueTask<SwarmCommitResult> CommitUpdate(CancellationToken cancellationToken);

		/// <summary>
		/// Gets the list of <see cref="SwarmServerInformation"/>s in the swarm, including the current one.
		/// </summary>
		/// <returns>A <see cref="List{T}"/> of <see cref="SwarmServerInformation"/>s in the swarm. If the server is not part of a swarm, <see langword="null"/> will be returned.</returns>
		List<SwarmServerInformation>? GetSwarmServers();
	}
}
