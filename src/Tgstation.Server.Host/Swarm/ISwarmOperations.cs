using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Tgstation.Server.Host.Swarm.Grpc;

namespace Tgstation.Server.Host.Swarm
{
	/// <summary>
	/// Swarm service operations to be called over RPC.
	/// </summary>
	public interface ISwarmOperations : ISwarmUpdateAborter
	{
		/// <summary>
		/// Pass in an updated list of <paramref name="swarmServers"/> to the node.
		/// </summary>
		/// <param name="swarmServers">An <see cref="IEnumerable{T}"/> of the updated <see cref="Api.Models.Internal.SwarmServerInformation"/>s.</param>
		void UpdateSwarmServersList(IEnumerable<Api.Models.Internal.SwarmServerInformation> swarmServers);

		/// <summary>
		/// Notify the node of an update request from the controller.
		/// </summary>
		/// <param name="updateRequest">The <see cref="PrepareUpdateRequest"/>.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in <see langword="true"/> if the node is able to update, <see langword="false"/> otherwise.</returns>
		ValueTask<bool> PrepareUpdateFromController(PrepareUpdateRequest updateRequest, CancellationToken cancellationToken);

		/// <summary>
		/// Validate a given <paramref name="registration"/>.
		/// </summary>
		/// <param name="registration">The <see cref="SwarmRegistration"/> to validate.</param>
		/// <returns><see langword="true"/> if the registration is valid, <see langword="false"/> otherwise.</returns>
		bool ValidateRegistration(SwarmRegistration registration);

		/// <summary>
		/// Attempt to register a given <paramref name="node"/> with the controller.
		/// </summary>
		/// <param name="node">The <see cref="SwarmServer"/> that is registering.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in a <see cref="RegisterNodeResponse"/>.</returns>
		ValueTask<RegisterNodeResponse> RegisterNode(Api.Models.Internal.SwarmServer node, CancellationToken cancellationToken);

		/// <summary>
		/// Attempt to unregister a node with a given <paramref name="registration"/> with the controller.
		/// </summary>
		/// <param name="registration">The <see cref="SwarmRegistration"/>.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask"/> representing the running operation.</returns>
		ValueTask UnregisterNode(SwarmRegistration registration, CancellationToken cancellationToken);

		/// <summary>
		/// Notify the controller that the node with the given <paramref name="registration"/> is ready to commit or notify the node of the controller telling it to commit.
		/// </summary>
		/// <param name="registration">The <see cref="SwarmRegistration"/>.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask"/> representing the running operation.</returns>
		ValueTask<bool> RemoteCommitReceived(SwarmRegistration registration, CancellationToken cancellationToken);
	}
}
