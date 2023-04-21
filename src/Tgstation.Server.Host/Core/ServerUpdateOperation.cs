using System;

using Tgstation.Server.Host.Swarm;

namespace Tgstation.Server.Host.Core
{
	/// <summary>
	/// Necessary data for performing a server update.
	/// </summary>
	sealed class ServerUpdateOperation
	{
		/// <summary>
		/// The <see cref="Version"/> being updated to.
		/// </summary>
		public Version TargetVersion { get; init; }

		/// <summary>
		/// The <see cref="Uri"/> pointing to the update data.
		/// </summary>
		public Uri UpdateZipUrl { get; init; }

		/// <summary>
		/// The <see cref="ISwarmService"/> for the operation.
		/// </summary>
		public ISwarmService SwarmService { get; init; }
	}
}
