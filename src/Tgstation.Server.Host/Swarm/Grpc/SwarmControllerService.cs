using System;
using System.Threading.Tasks;

using Grpc.Core;

using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;

using Tgstation.Server.Host.Properties;

namespace Tgstation.Server.Host.Swarm.Grpc
{
	/// <summary>
	/// gRPC swarm controller implementation.
	/// </summary>
	[Authorize(Policy = SwarmConstants.AuthenticationSchemeAndPolicy)]
	sealed class SwarmControllerService : GrpcSwarmControllerService.GrpcSwarmControllerServiceBase
	{
		/// <summary>
		/// The <see cref="ISwarmOperations"/> for the <see cref="SwarmControllerService"/>.
		/// </summary>
		readonly ISwarmOperations swarmOperations;

		/// <summary>
		/// The <see cref="ILogger"/> for the <see cref="SwarmControllerService"/>.
		/// </summary>
		readonly ILogger<SwarmControllerService> logger;

		/// <summary>
		/// Initializes a new instance of the <see cref="SwarmControllerService"/> class.
		/// </summary>
		/// <param name="swarmOperations">The value of <see cref="ISwarmOperations"/>.</param>
		/// <param name="logger">The value of <see cref="logger"/>.</param>
		public SwarmControllerService(ISwarmOperations swarmOperations, ILogger<SwarmControllerService> logger)
		{
			this.swarmOperations = swarmOperations ?? throw new ArgumentNullException(nameof(swarmOperations));
			this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
		}

		/// <summary>
		/// Attempt to register a node.
		/// </summary>
		/// <param name="request">The <see cref="RegisterNodeRequest"/>.</param>
		/// <param name="context">The <see cref="ServerCallContext"/>.</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="RegisterNodeResponse"/>.</returns>
		public override async Task<RegisterNodeResponse> RegisterNode(RegisterNodeRequest request, ServerCallContext context)
		{
			ArgumentNullException.ThrowIfNull(request);
			ArgumentNullException.ThrowIfNull(context);

			if (request.RegisteringNode == null)
				throw new RpcException(
					new Status(StatusCode.InvalidArgument, $"{nameof(RegisterNodeRequest.RegisteringNode)} was null!"));

			if (request.SwarmProtocolVersion == null)
				throw new RpcException(
					new Status(StatusCode.InvalidArgument, $"{nameof(RegisterNodeRequest.SwarmProtocolVersion)} was null!"));

			Api.Models.Internal.SwarmServer swarmServer;
			try
			{
				ArgumentException.ThrowIfNullOrWhiteSpace(request.RegisteringNode.Identifier);

				swarmServer = new Api.Models.Internal.SwarmServer
				{
					Address = new Uri(request.RegisteringNode.Address),
					PublicAddress = request.RegisteringNode.PublicAddress == null
						? null
						: new Uri(request.RegisteringNode.PublicAddress),
					Identifier = request.RegisteringNode.Identifier,
				};
			}
			catch (Exception ex)
			{
				throw new RpcException(
					new Status(StatusCode.InvalidArgument, ex.Message));
			}

			var swarmProtocolVersion = Version.Parse(MasterVersionsAttribute.Instance.RawSwarmProtocolVersion);
			if (request.SwarmProtocolVersion.Major != swarmProtocolVersion.Major)
				throw new RpcException(
					new Status(StatusCode.FailedPrecondition, "Provided swarm protocol version is incompatible!"));

			var registrationResult = await swarmOperations.RegisterNode(swarmServer, context.CancellationToken);

			if (request.SwarmProtocolVersion.Minor != swarmProtocolVersion.Minor || request.SwarmProtocolVersion.Patch != swarmProtocolVersion.Build)
				logger.LogWarning("Allowed node {identifier} to register despite having a slightly different swarm protocol version!", request.RegisteringNode.Identifier);

			return registrationResult;
		}

		/// <summary>
		/// Attempt to unregister a node.
		/// </summary>
		/// <param name="request">The <see cref="UnregisterNodeRequest"/>.</param>
		/// <param name="context">The <see cref="ServerCallContext"/>.</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="UnregisterNodeResponse"/>.</returns>
		public override async Task<UnregisterNodeResponse> UnregisterNode(UnregisterNodeRequest request, ServerCallContext context)
		{
			ArgumentNullException.ThrowIfNull(request);
			ArgumentNullException.ThrowIfNull(context);

			if (request.Registration == null)
				throw new RpcException(
					new Status(StatusCode.InvalidArgument, $"{nameof(UnregisterNodeRequest.Registration)} was null!"));

			request.Registration.Validate(swarmOperations);

			await swarmOperations.UnregisterNode(request.Registration, context.CancellationToken);

			return new UnregisterNodeResponse();
		}
	}
}
