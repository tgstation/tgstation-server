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
	public sealed class Swarm
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
		/// Gets the connected <see cref="Node"/> server.
		/// </summary>
		/// <param name="swarmService">The <see cref="ISwarmService"/> to use.</param>
		/// <param name="swarmConfigurationOptions">The <see cref="IOptionsSnapshot{TOptions}"/> containing the current <see cref="SwarmConfiguration"/>.</param>
		/// <returns>A new <see cref="Node"/>.</returns>
		public Node CurrentNode(
			[Service] ISwarmService swarmService,
			[Service] IOptionsSnapshot<SwarmConfiguration> swarmConfigurationOptions)
		{
			ArgumentNullException.ThrowIfNull(swarmService);
			ArgumentNullException.ThrowIfNull(swarmConfigurationOptions);

			var nodeInfos = Nodes(swarmService);
			if (nodeInfos != null)
				return nodeInfos.First(x => x.Info!.Identifier == swarmConfigurationOptions.Value.Identifier);

			return new Node(null);
		}

		/// <summary>
		/// Gets all <see cref="Node"/> servers in the swarm.
		/// </summary>
		/// <param name="swarmService">The <see cref="ISwarmService"/> to use.</param>
		/// <returns>A <see cref="List{T}"/> of <see cref="Node"/>s if the local server is part of a swarm, <see langword="null"/> otherwise.</returns>
		public List<Node>? Nodes(
			[Service] ISwarmService swarmService)
		{
			ArgumentNullException.ThrowIfNull(swarmService);
			return swarmService.GetSwarmServers()?.Select(x => new Node(new NodeInformation(x))).ToList();
		}
	}
}
