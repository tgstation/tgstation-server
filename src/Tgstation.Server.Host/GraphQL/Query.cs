#pragma warning disable CA1724

namespace Tgstation.Server.Host.GraphQL.Types
{
	/// <summary>
	/// GraphQL query <see cref="global::System.Type"/>.
	/// </summary>
	public sealed class Query
	{
		/// <summary>
		/// Gets the <see cref="Types.ServerSwarm"/>.
		/// </summary>
		/// <returns>A new <see cref="Types.ServerSwarm"/>.</returns>
		public ServerSwarm Swarm() => new();
	}
}
