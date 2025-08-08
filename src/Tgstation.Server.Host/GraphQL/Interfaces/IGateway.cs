using System.Linq;

using HotChocolate.Authorization;

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

		/// <summary>
		/// Queries all <see cref="Instance"/>s in the <see cref="IGateway"/>.
		/// </summary>
		/// <returns>Queryable <see cref="Instance"/>s in the <see cref="IGateway"/>.</returns>
		[Authorize]
		IQueryable<Instance> Instances();
	}
}
