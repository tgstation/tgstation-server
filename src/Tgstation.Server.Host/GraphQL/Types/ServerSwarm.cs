using System;
using System.Collections.Generic;

using HotChocolate;
using Tgstation.Server.Api.Models.Internal;
using Tgstation.Server.Host.Core;
using Tgstation.Server.Host.Swarm;
using Tgstation.Server.Host.System;

namespace Tgstation.Server.Host.GraphQL.Types
{
	/// <summary>
	/// Represents a tgstation-server swarm.
	/// </summary>
	public sealed class ServerSwarm
	{
		/// <summary>
		/// Gets the <see cref="SwarmMetadata"/> for the swarm.
		/// </summary>
		/// <param name="assemblyInformationProvider">The <see cref="IAssemblyInformationProvider"/> to use.</param>
		/// <param name="serverControl">The <see cref="IServerControl"/> to use.</param>
		/// <returns>A new <see cref="SwarmMetadata"/>.</returns>
		public SwarmMetadata Metadata(
			[Service] IAssemblyInformationProvider assemblyInformationProvider,
			[Service] IServerControl serverControl)
		{
			ArgumentNullException.ThrowIfNull(assemblyInformationProvider);
			ArgumentNullException.ThrowIfNull(serverControl);
			return new SwarmMetadata(assemblyInformationProvider, serverControl.UpdateInProgress);
		}

		/// <summary>
		/// Gets the local <see cref="Types.LocalServer"/>.
		/// </summary>
		/// <returns>A new <see cref="Types.LocalServer"/>.</returns>
		public LocalServer LocalServer() => new();

		/// <summary>
		/// Gets the <see cref="SwarmServerInformation"/> for all servers in a swarm.
		/// </summary>
		/// <param name="swarmService">The <see cref="ISwarmService"/> to use.</param>
		/// <returns>A <see cref="List{T}"/> of <see cref="SwarmServerInformation"/>s if the local server is part of a swarm, <see langword="null"/> otherwise.</returns>
		public List<SwarmServerInformation>? Servers(
			[Service] ISwarmService swarmService)
		{
			ArgumentNullException.ThrowIfNull(swarmService);
			return swarmService.GetSwarmServers();
		}
	}
}
