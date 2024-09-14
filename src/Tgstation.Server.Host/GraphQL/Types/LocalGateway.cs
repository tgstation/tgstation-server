using Tgstation.Server.Host.GraphQL.Interfaces;

namespace Tgstation.Server.Host.GraphQL.Types
{
	/// <summary>
	/// <see cref="IGateway"/> for the <see cref="SwarmNode"/> this query is executing on.
	/// </summary>
	public sealed class LocalGateway : IGateway
	{
		/// <inheritdoc />
		public GatewayInformation Information() => new();
	}
}
