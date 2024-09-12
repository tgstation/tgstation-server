using System;

using HotChocolate;

using Microsoft.Extensions.Options;

using Tgstation.Server.Api.Models;
using Tgstation.Server.Host.Configuration;
using Tgstation.Server.Host.GraphQL.Interfaces;

namespace Tgstation.Server.Host.GraphQL.Types
{
	/// <summary>
	/// Represents a node server in a swarm.
	/// </summary>
	public sealed class SwarmNode
	{
		/// <summary>
		/// Gets the <see cref="NodeInformation"/>.
		/// </summary>
		public NodeInformation? Info { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="SwarmNode"/> class.
		/// </summary>
		/// <param name="info">The value of <see cref="Info"/>.</param>
		public SwarmNode(NodeInformation? info)
		{
			Info = info;
		}

		/// <summary>
		/// Gets the <see cref="SwarmNode"/>'s <see cref="IGateway"/>.
		/// </summary>
		/// <param name="swarmConfigurationOptions">The <see cref="IOptionsSnapshot{TOptions}"/> containing the current <see cref="SwarmConfiguration"/>.</param>
		/// <returns>A new <see cref="IGateway"/>.</returns>
		/// <remarks>The <see cref="SwarmNode"/>'s <see cref="IGateway"/>.</remarks>
		public IGateway? Gateway([Service] IOptionsSnapshot<SwarmConfiguration> swarmConfigurationOptions)
		{
			ArgumentNullException.ThrowIfNull(swarmConfigurationOptions);

			bool local = Info == null || Info.Identifier == swarmConfigurationOptions.Value.Identifier;
			if (local)
				return new LocalGateway();

			throw new ErrorMessageException(ErrorCode.RemoteGatewaysNotImplemented);

			// return new RemoteGateway();
		}
	}
}
