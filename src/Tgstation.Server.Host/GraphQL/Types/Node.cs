using System;

using HotChocolate;

using Microsoft.Extensions.Options;

using Tgstation.Server.Host.Configuration;

namespace Tgstation.Server.Host.GraphQL.Types
{
	/// <summary>
	/// Represents a node server in a swarm.
	/// </summary>
	public sealed class Node
	{
		/// <summary>
		/// Gets the <see cref="NodeInformation"/>.
		/// </summary>
		public NodeInformation? Info { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="Node"/> class.
		/// </summary>
		/// <param name="info">The value of <see cref="Info"/>.</param>
		public Node(NodeInformation? info)
		{
			Info = info;
		}

		/// <summary>
		/// Gets the <see cref="Node"/>'s <see cref="IGateway"/>.
		/// </summary>
		/// <param name="swarmConfigurationOptions">The <see cref="IOptionsSnapshot{TOptions}"/> containing the current <see cref="SwarmConfiguration"/>.</param>
		/// <returns>A new <see cref="IGateway"/>.</returns>
		/// <remarks>The <see cref="Node"/>'s <see cref="IGateway"/>.</remarks>
		public IGateway? Gateway([Service] IOptionsSnapshot<SwarmConfiguration> swarmConfigurationOptions)
		{
			ArgumentNullException.ThrowIfNull(swarmConfigurationOptions);

			bool local = Info == null || Info.Identifier == swarmConfigurationOptions.Value.Identifier;
			if (local)
				return new LocalGateway();

			return new RemoteGateway();
		}
	}
}
