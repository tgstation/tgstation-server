using System;
using System.Collections.Generic;
using System.Linq;

using HotChocolate;

using Microsoft.Extensions.Options;

using Tgstation.Server.Host.Configuration;
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
		/// Gets the swarm's <see cref="Types.Users"/>.
		/// </summary>
		/// <returns>A new <see cref="Types.Users"/>.</returns>
		public Users Users() => new();

		/// <summary>
		/// Gets the connected <see cref="SwarmNode"/> server.
		/// </summary>
		/// <param name="swarmService">The <see cref="ISwarmService"/> to use.</param>
		/// <param name="swarmConfigurationOptions">The <see cref="IOptionsSnapshot{TOptions}"/> containing the current <see cref="SwarmConfiguration"/>.</param>
		/// <returns>A new <see cref="SwarmNode"/>.</returns>
		public SwarmNode CurrentNode(
			[Service] ISwarmService swarmService,
			[Service] IOptionsSnapshot<SwarmConfiguration> swarmConfigurationOptions)
		{
			ArgumentNullException.ThrowIfNull(swarmService);
			ArgumentNullException.ThrowIfNull(swarmConfigurationOptions);

			var nodeInfos = Nodes(swarmService);
			if (nodeInfos != null)
				return nodeInfos.First(x => x.Info!.Identifier == swarmConfigurationOptions.Value.Identifier);

			return new SwarmNode(null);
		}

		/// <summary>
		/// Gets all <see cref="SwarmNode"/> servers in the swarm.
		/// </summary>
		/// <param name="swarmService">The <see cref="ISwarmService"/> to use.</param>
		/// <returns>A <see cref="List{T}"/> of <see cref="SwarmNode"/>s if the local server is part of a swarm, <see langword="null"/> otherwise.</returns>
		public List<SwarmNode>? Nodes(
			[Service] ISwarmService swarmService)
		{
			ArgumentNullException.ThrowIfNull(swarmService);
			return swarmService.GetSwarmServers()?.Select(x => new SwarmNode(new NodeInformation(x))).ToList();
		}
	}
}
