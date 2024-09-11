#pragma warning disable CA1724

namespace Tgstation.Server.Host.GraphQL
{
	/// <summary>
	/// GraphQL query <see cref="global::System.Type"/>.
	/// </summary>
	public sealed class Query
	{
		/// <summary>
		/// Gets the <see cref="Types.Swarm"/>.
		/// </summary>
		/// <returns>A new <see cref="Types.Swarm"/>.</returns>
		public Types.Swarm Swarm() => new();
	}
}
