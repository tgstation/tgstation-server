using System;
using System.Linq;

using HotChocolate;
using HotChocolate.Types.Relay;

using Tgstation.Server.Host.Swarm;

namespace Tgstation.Server.Host.GraphQL.Types
{
	/// <summary>
	/// Represent a server in the TGS server swarm.
	/// </summary>
	[Node]
	public sealed class NodeInformation
	{
		public NodeInformation? GetNodeInformation(
			string identifier,
			[Service] ISwarmService swarmService)
		{
			ArgumentNullException.ThrowIfNull(identifier);
			ArgumentNullException.ThrowIfNull(swarmService);

			var node = swarmService.GetSwarmServers()
				?.FirstOrDefault(node => node.Identifier == identifier);
			if (node == null)
				return null;

			return new NodeInformation(node);
		}

		/// <summary>
		/// The swarm server ID.
		/// </summary>
		[ID]
		public string Identifier { get; }

		/// <summary>
		/// The swarm server's internal <see cref="Uri"/>.
		/// </summary>
		public Uri Address { get; }

		/// <summary>
		/// The swarm server's optional public address.
		/// </summary>
		public Uri? PublicAddress { get; }

		/// <summary>
		/// Whether or not the server is the swarm's controller.
		/// </summary>
		public bool Controller { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="NodeInformation"/> class.
		/// </summary>
		/// <param name="swarmServerInformation">The <see cref="Api.Models.Internal.SwarmServerInformation"/> to build from.</param>
		public NodeInformation(Api.Models.Internal.SwarmServerInformation swarmServerInformation)
		{
			ArgumentNullException.ThrowIfNull(swarmServerInformation);

			Identifier = swarmServerInformation.Identifier!;
			Address = swarmServerInformation.Address!;
			PublicAddress = swarmServerInformation.PublicAddress;
			Controller = swarmServerInformation.Controller;
		}
	}
}
