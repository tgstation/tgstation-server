#pragma warning disable CA1724 // Dumb conflict with Microsoft.EntityFrameworkCore.Query

using HotChocolate;

namespace Tgstation.Server.Host.GraphQL
{
	/// <summary>
	/// GraphQL query <see cref="global::System.Type"/>.
	/// </summary>
	[GraphQLDescription("Root Query type")]
	public sealed class Query
	{
		/// <summary>
		/// Gets the <see cref="Types.ServerSwarm"/>.
		/// </summary>
		/// <returns>A new <see cref="Types.ServerSwarm"/>.</returns>
		public Types.ServerSwarm Swarm() => new();
	}
}
