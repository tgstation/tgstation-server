using HotChocolate;

using Microsoft.Extensions.Options;

using Tgstation.Server.Host.Configuration;

namespace Tgstation.Server.Host.GraphQL.Interfaces
{
	/// <summary>
	/// Represents a tgstation-server installation.
	/// </summary>
	public interface IServerNode
	{
		/// <summary>
		/// Access the <see cref="IGateway"/> for the <see cref="IServerNode"/>.
		/// </summary>
		/// <param name="swarmConfigurationOptions">The <see cref="IOptionsSnapshot{TOptions}"/> of <see cref="SwarmConfiguration"/>.</param>
		/// <returns>The <see cref="IGateway"/> for the <see cref="IServerNode"/>.</returns>
		public IGateway Gateway([Service] IOptionsSnapshot<SwarmConfiguration> swarmConfigurationOptions);
	}
}
