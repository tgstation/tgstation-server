using System;

using Tgstation.Server.Host.IO;
using Tgstation.Server.Host.Swarm;

#nullable disable

namespace Tgstation.Server.Host.Core
{
	/// <summary>
	/// Necessary data for performing a server update.
	/// </summary>
	sealed class ServerUpdateOperation
	{
		/// <summary>
		/// The <see cref="IFileStreamProvider"/> that contains the update zip file.
		/// </summary>
		public IFileStreamProvider FileStreamProvider { get; }

		/// <summary>
		/// The <see cref="ISwarmService"/> for the operation.
		/// </summary>
		public ISwarmService SwarmService { get; }

		/// <summary>
		/// The <see cref="Version"/> being updated to.
		/// </summary>
		public Version TargetVersion { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="ServerUpdateOperation"/> class.
		/// </summary>
		/// <param name="fileStreamProvider">The value of <see cref="FileStreamProvider"/>.</param>
		/// <param name="swarmService">The value of <see cref="SwarmService"/>.</param>
		/// <param name="targetVersion">The value of <see cref="TargetVersion"/>.</param>
		public ServerUpdateOperation(IFileStreamProvider fileStreamProvider, ISwarmService swarmService, Version targetVersion)
		{
			FileStreamProvider = fileStreamProvider ?? throw new ArgumentNullException(nameof(fileStreamProvider));
			SwarmService = swarmService ?? throw new ArgumentNullException(nameof(swarmService));
			TargetVersion = targetVersion ?? throw new ArgumentNullException(nameof(targetVersion));
		}
	}
}
