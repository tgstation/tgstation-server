using System;
using System.Threading.Tasks;

using Grpc.Core;

using Microsoft.AspNetCore.Authorization;

namespace Tgstation.Server.Host.Swarm.Grpc
{
	/// <summary>
	/// gRPC swarm implementation shared by nodes and controllers.
	/// </summary>
	[Authorize(Policy = SwarmConstants.AuthenticationSchemeAndPolicy)]
	sealed class SwarmSharedService : GrpcSwarmSharedService.GrpcSwarmSharedServiceBase
	{
		/// <summary>
		/// The <see cref="ISwarmOperations"/> for the <see cref="GrpcSwarmControllerService"/>.
		/// </summary>
		readonly ISwarmOperations swarmOperations;

		/// <summary>
		/// Initializes a new instance of the <see cref="SwarmSharedService"/> class.
		/// </summary>
		/// <param name="swarmOperations">The value of <see cref="ISwarmOperations"/>.</param>
		public SwarmSharedService(ISwarmOperations swarmOperations)
		{
			this.swarmOperations = swarmOperations ?? throw new ArgumentNullException(nameof(swarmOperations));
		}

		/// <summary>
		/// Update abort endpoint.
		/// </summary>
		/// <param name="request">The <see cref="AbortUpdateRequest"/>.</param>
		/// <param name="context">The <see cref="ServerCallContext"/>.</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="AbortUpdateResponse"/>.</returns>
		public override async Task<AbortUpdateResponse> AbortUpdate(AbortUpdateRequest request, ServerCallContext context)
		{
			ArgumentNullException.ThrowIfNull(request);
			ArgumentNullException.ThrowIfNull(context);

			request.Registration.Validate(swarmOperations);

			await swarmOperations.AbortUpdate();

			return new AbortUpdateResponse();
		}

		/// <summary>
		/// Update initiation endpoint.
		/// </summary>
		/// <param name="request">The <see cref="PrepareUpdateRequest"/>.</param>
		/// <param name="context">The <see cref="ServerCallContext"/>.</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="PrepareUpdateResponse"/>.</returns>
		public override async Task<PrepareUpdateResponse> PrepareUpdate(PrepareUpdateRequest request, ServerCallContext context)
		{
			ArgumentNullException.ThrowIfNull(request);
			ArgumentNullException.ThrowIfNull(context);

			request.Registration.Validate(swarmOperations);

			if (!await swarmOperations.PrepareUpdateFromController(request, context.CancellationToken))
				throw new RpcException(
					new Status(StatusCode.Aborted, "Could not prepare for update!"));

			return new PrepareUpdateResponse();
		}

		/// <summary>
		/// Update (ready-)commit endpoint.
		/// </summary>
		/// <param name="request">The <see cref="PrepareUpdateRequest"/>.</param>
		/// <param name="context">The <see cref="ServerCallContext"/>.</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="CommitUpdateResponse"/>.</returns>
		public override async Task<CommitUpdateResponse> CommitUpdate(CommitUpdateRequest request, ServerCallContext context)
		{
			ArgumentNullException.ThrowIfNull(request);
			ArgumentNullException.ThrowIfNull(context);

			request.Registration.Validate(swarmOperations);

			var result = await swarmOperations.RemoteCommitReceived(request.Registration, context.CancellationToken);
			if (!result)
				throw new RpcException(
					new Status(StatusCode.Aborted, "Could not commit update!"));

			return new CommitUpdateResponse();
		}
	}
}
