using Tgstation.Server.Host.GraphQL.Types;

namespace Tgstation.Server.Host.GraphQL.Interfaces
{
	/// <summary>
	/// Management interface for the parent <see cref="SwarmNode"/>.
	/// </summary>
	public interface IGateway
	{
		/// <summary>
		/// Gets <see cref="GatewayInformation"/>.
		/// </summary>
		/// <returns>The <see cref="GatewayInformation"/> for the <see cref="IGateway"/>.</returns>
		GatewayInformation Information();
	}
}
