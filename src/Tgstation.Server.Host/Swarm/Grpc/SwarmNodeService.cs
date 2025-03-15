using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Grpc.Core;

using Microsoft.AspNetCore.Authorization;

namespace Tgstation.Server.Host.Swarm.Grpc
{
	/// <summary>
	/// gRPC swarm node implementation.
	/// </summary>
	[Authorize(Policy = SwarmConstants.AuthenticationSchemeAndPolicy)]
	sealed class SwarmNodeService : GrpcSwarmNodeService.GrpcSwarmNodeServiceBase
	{
		/// <summary>
		/// The <see cref="ISwarmOperations"/> for the <see cref="SwarmNodeService"/>.
		/// </summary>
		readonly ISwarmOperations swarmOperations;

		/// <summary>
		/// Initializes a new instance of the <see cref="SwarmNodeService"/> class.
		/// </summary>
		/// <param name="swarmOperations">The value of <see cref="ISwarmOperations"/>.</param>
		public SwarmNodeService(ISwarmOperations swarmOperations)
		{
			this.swarmOperations = swarmOperations ?? throw new ArgumentNullException(nameof(swarmOperations));
		}

		/// <summary>
		/// Health check endpoint.
		/// </summary>
		/// <param name="request">The <see cref="PrepareUpdateRequest"/>.</param>
		/// <param name="context">The <see cref="ServerCallContext"/>.</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="HealthCheckResponse"/>.</returns>
		public override Task<HealthCheckResponse> HealthCheck(HealthCheckRequest request, ServerCallContext context)
		{
			ArgumentNullException.ThrowIfNull(request);
			ArgumentNullException.ThrowIfNull(context);

			request.Registration.Validate(swarmOperations);
			return Task.FromResult(new HealthCheckResponse());
		}

		/// <summary>
		/// Node list update endpoint.
		/// </summary>
		/// <param name="request">The <see cref="PrepareUpdateRequest"/>.</param>
		/// <param name="context">The <see cref="ServerCallContext"/>.</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="UpdateNodeListResponse"/>.</returns>
		public override Task<UpdateNodeListResponse> UpdateNodeList(UpdateNodeListRequest request, ServerCallContext context)
		{
			ArgumentNullException.ThrowIfNull(request);
			ArgumentNullException.ThrowIfNull(context);

			request.Registration.Validate(swarmOperations);

			List<Api.Models.Internal.SwarmServerInformation> apiSwarmServers;
			try
			{
				apiSwarmServers = request.NodeList.Select(
					info => new Api.Models.Internal.SwarmServerInformation
					{
						Controller = info.Controller,
						Identifier = info.SwarmServer.Identifier,
						Address = new Uri(info.SwarmServer.Address),
						PublicAddress = new Uri(info.SwarmServer.PublicAddress),
					})
					.ToList();
			}
			catch (Exception ex)
			{
				throw new RpcException(
					new Status(StatusCode.InvalidArgument, ex.Message));
			}

			swarmOperations.UpdateSwarmServersList(apiSwarmServers);

			return Task.FromResult(new UpdateNodeListResponse());
		}
	}
}
