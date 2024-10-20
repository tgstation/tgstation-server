using System;
using System.Collections.Generic;
using System.Linq;

using HotChocolate;

using Microsoft.Extensions.Options;

using Tgstation.Server.Host.Configuration;
using Tgstation.Server.Host.GraphQL.Interfaces;
using Tgstation.Server.Host.Properties;
using Tgstation.Server.Host.Security;
using Tgstation.Server.Host.Swarm;

namespace Tgstation.Server.Host.GraphQL.Types
{
	/// <summary>
	/// Represents a tgstation-server swarm.
	/// </summary>
	public sealed class ServerSwarm
	{
		/// <summary>
		/// Gets the swarm protocol major version in use.
		/// </summary>
		[TgsGraphQLAuthorize]
		public int ProtocolMajorVersion => Version.Parse(MasterVersionsAttribute.Instance.RawSwarmProtocolVersion).Major;

		/// <summary>
		/// Gets the swarm's <see cref="Types.Users"/>.
		/// </summary>
		/// <returns>A new <see cref="Types.Users"/>.</returns>
		[TgsGraphQLAuthorize]
		public Users Users() => new();

		/// <summary>
		/// Gets the connected <see cref="SwarmNode"/> server.
		/// </summary>
		/// <param name="swarmConfigurationOptions">The <see cref="IOptionsSnapshot{TOptions}"/> containing the current <see cref="SwarmConfiguration"/>.</param>
		/// <param name="swarmService">The <see cref="ISwarmService"/> to use.</param>
		/// <returns>A new <see cref="SwarmNode"/> for the local node if it is part of a swarm, <see langword="null"/> otherwise.</returns>
		public IServerNode CurrentNode(
			[Service] IOptionsSnapshot<SwarmConfiguration> swarmConfigurationOptions,
			[Service] ISwarmService swarmService)
		{
			ArgumentNullException.ThrowIfNull(swarmConfigurationOptions);
			ArgumentNullException.ThrowIfNull(swarmService);

			var ourIdentifier = swarmConfigurationOptions.Value.Identifier;
			if (ourIdentifier == null)
				return new StandaloneNode();

			return (IServerNode?)SwarmNode.GetSwarmNode(ourIdentifier, swarmService) ?? new StandaloneNode();
		}

		/// <summary>
		/// Gets all <see cref="SwarmNode"/> servers in the swarm.
		/// </summary>
		/// <param name="swarmService">The <see cref="ISwarmService"/> to use.</param>
		/// <returns>A <see cref="List{T}"/> of <see cref="SwarmNode"/>s if the local node is part of a swarm, <see langword="null"/> otherwise.</returns>
		[TgsGraphQLAuthorize]
		public List<SwarmNode>? Nodes(
			[Service] ISwarmService swarmService)
		{
			ArgumentNullException.ThrowIfNull(swarmService);
			return swarmService.GetSwarmServers()?.Select(x => new SwarmNode(x)).ToList();
		}

		/// <summary>
		/// Gets the <see cref="Types.UpdateInformation"/> for the swarm.
		/// </summary>
		/// <returns>A new <see cref="Types.UpdateInformation"/>.</returns>
		[TgsGraphQLAuthorize]
		public UpdateInformation UpdateInformation() => new();
	}
}
