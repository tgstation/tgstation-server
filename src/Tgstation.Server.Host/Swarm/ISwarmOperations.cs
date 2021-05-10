using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Tgstation.Server.Api.Models.Response;

namespace Tgstation.Server.Host.Swarm
{
	/// <summary>
	/// Swarm service operations for the <see cref="Controllers.SwarmController"/>.
	/// </summary>
	public interface ISwarmOperations
	{
		/// <summary>
		/// Pass in an updated list of <paramref name="swarmServers"/> to the node.
		/// </summary>
		/// <param name="swarmServers">An <see cref="IEnumerable{T}"/> of the updated <see cref="SwarmServerResponse"/>s.</param>
		void UpdateSwarmServersList(IEnumerable<SwarmServerResponse> swarmServers);

		/// <summary>
		/// Notify the node of an update request from the controller.
		/// </summary>
		/// <param name="version">The <see cref="Version"/> of TGS to update to.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in <see langword="true"/> if the node is able to update, <see langword="false"/> otherwise.</returns>
		Task<bool> PrepareUpdateFromController(Version version, CancellationToken cancellationToken);

		/// <summary>
		/// Validate a given <paramref name="registrationId"/>.
		/// </summary>
		/// <param name="registrationId">The registration <see cref="Guid"/> to validate.</param>
		/// <returns><see langword="true"/> if the registration is valid, <see langword="false"/> otherwise.</returns>
		bool ValidateRegistration(Guid registrationId);

		/// <summary>
		/// Attempt to register a given <paramref name="node"/> with the controller.
		/// </summary>
		/// <param name="node">The <see cref="SwarmServerResponse"/> that is registering.</param>
		/// <param name="registrationId">The registration <see cref="Guid"/>.</param>
		/// <returns><see langword="true"/> if the registration was successful, <see langword="false"/> otherwise.</returns>
		bool RegisterNode(Api.Models.Internal.SwarmServer node, Guid registrationId);

		/// <summary>
		/// Attempt to unregister a node with a given <paramref name="registrationId"/> with the controller.
		/// </summary>
		/// <param name="registrationId">The registration <see cref="Guid"/>.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task"/> representing the running operation.</returns>
		Task UnregisterNode(Guid registrationId, CancellationToken cancellationToken);

		/// <summary>
		/// Notify the controller that the node with the given <paramref name="registrationId"/> is ready to commit or notify the node of the controller telling it to commit.
		/// </summary>
		/// <param name="registrationId">The registration <see cref="Guid"/>.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task"/> representing the running operation.</returns>
		Task<bool> RemoteCommitRecieved(Guid registrationId, CancellationToken cancellationToken);

		/// <summary>
		/// Remotely abort an uncommitted update.
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task"/> representing the running operation.</returns>
		Task RemoteAbortUpdate(CancellationToken cancellationToken);
	}
}
