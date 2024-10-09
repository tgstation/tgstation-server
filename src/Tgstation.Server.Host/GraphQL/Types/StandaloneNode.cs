using HotChocolate;

using Microsoft.Extensions.Options;

using Tgstation.Server.Host.Configuration;
using Tgstation.Server.Host.GraphQL.Interfaces;

namespace Tgstation.Server.Host.GraphQL.Types
{
	/// <summary>
	/// A <see cref="IServerNode"/> not running as part of a larger <see cref="ServerSwarm"/>.
	/// </summary>
	public sealed class StandaloneNode : IServerNode
	{
		/// <inheritdoc />
		public IGateway Gateway([Service] IOptionsSnapshot<SwarmConfiguration> swarmConfigurationOptions)
			=> new LocalGateway();
	}
}
