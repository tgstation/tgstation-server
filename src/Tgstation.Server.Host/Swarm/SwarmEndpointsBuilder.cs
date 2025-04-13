using System;
using System.Net;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

using Tgstation.Server.Host.Configuration;
using Tgstation.Server.Host.Controllers;
using Tgstation.Server.Host.Core;
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
		/// <param name="serverPortProvider">The <see cref="IServerPortProvider"/>.</param>
		/// <param name="swarmConfiguration">The <see cref="SwarmConfiguration"/>.</param>
		internal static void Map(
			IEndpointRouteBuilder endpoints,
			IServerPortProvider serverPortProvider,
			SwarmConfiguration swarmConfiguration)
		{
			ArgumentNullException.ThrowIfNull(endpoints);
			ArgumentNullException.ThrowIfNull(serverPortProvider);
			ArgumentNullException.ThrowIfNull(swarmConfiguration);

			if (swarmConfiguration.PrivateKey == null)
				return;

			var hostingIP = swarmConfiguration.HostingIP;
			if (hostingIP == null || IPAddress.Parse(hostingIP) == IPAddress.Any)
			{
				hostingIP = "*";
			}

			var swarmRequiredHost = $"{hostingIP}:{swarmConfiguration.HostingPort ?? serverPortProvider.HttpApiPort}";

			endpoints.MapGrpcService<SwarmSharedService>()
				.RequireHost(swarmRequiredHost);

			if (swarmConfiguration.ControllerAddress != null)
				endpoints.MapGrpcService<SwarmNodeService>()
					.RequireHost(swarmRequiredHost);
			else
				endpoints.MapGrpcService<SwarmControllerService>()
					.RequireHost(swarmRequiredHost);

			// special map the transfer controller download endpoint
			endpoints.MapControllerRoute(
				"SwarmTransferAccess",
				"{controller}/{action}",
				constraints: new
				{
					controller = SwarmConstants.TransferControllerName,
					action = nameof(SwarmTransferController.Download),
				})
				.RequireHost(swarmRequiredHost);
		}
	}
}
