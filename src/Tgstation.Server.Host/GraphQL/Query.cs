#pragma warning disable CA1724

using Tgstation.Server.Host.GraphQL.Types;

namespace Tgstation.Server.Host.GraphQL
{
	/// <summary>
	/// GraphQL query <see cref="global::System.Type"/>.
	/// </summary>
	public sealed class Query
	{
		/// <summary>
		/// Gets the <see cref="ServerSwarm"/>.
		/// </summary>
		/// <returns>A new <see cref="ServerSwarm"/>.</returns>
		public ServerSwarm Swarm() => new();
	}
}
