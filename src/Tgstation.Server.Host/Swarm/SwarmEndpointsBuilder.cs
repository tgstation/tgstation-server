using System;
using System.Linq;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

using Tgstation.Server.Host.Configuration;
using Tgstation.Server.Host.Controllers;
using Tgstation.Server.Host.Swarm.Grpc;

namespace Tgstation.Server.Host.Swarm
{
	/// <summary>
	/// Setup endpoints for the swarm system.
	/// </summary>
	public static class SwarmEndpointsBuilder
	{
		/// <summary>
		/// Map endpoints.
		/// </summary>
		/// <param name="endpoints">The <see cref="IEndpointRouteBuilder"/>.</param>
		/// <param name="swarmConfiguration">The <see cref="SwarmConfiguration"/>.</param>
		/// <returns>The required hosts <see cref="string"/> <see cref="Array"/> for the swarm service.</returns>
		internal static string[]? Map(
			IEndpointRouteBuilder endpoints,
			SwarmConfiguration swarmConfiguration)
		{
			ArgumentNullException.ThrowIfNull(endpoints);
			ArgumentNullException.ThrowIfNull(swarmConfiguration);

			if (swarmConfiguration.PrivateKey == null)
				return null;

			var swarmHosts = swarmConfiguration.EndPoints.Select(endpoint => endpoint.ParseEndPointSpecification()).ToArray();

			endpoints.MapGrpcService<SwarmSharedService>()
				.RequireHost(swarmHosts);

			if (swarmConfiguration.ControllerAddress != null)
				endpoints.MapGrpcService<SwarmNodeService>()
					.RequireHost(swarmHosts);
			else
				endpoints.MapGrpcService<SwarmControllerService>()
					.RequireHost(swarmHosts);

			// special map the transfer controller download endpoint
			endpoints.MapControllerRoute(
				"SwarmTransferAccess",
				"{controller}/{action}",
				constraints: new
				{
					controller = SwarmConstants.TransferControllerName,
					action = nameof(SwarmTransferController.Download),
				})
				.RequireHost(swarmHosts);

			return swarmHosts;
		}
	}
}
