using System.Threading;
using System.Threading.Tasks;

namespace Tgstation.Server.Host.Swarm
{
	/// <summary>
	/// Start and stop controllers for a swarm service.
	/// </summary>
	interface ISwarmServiceController
	{
		/// <summary>
		/// Attempt to register with the swarm controller if not one, sets up the database otherwise.
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="SwarmRegistrationResult"/>.</returns>
		Task<SwarmRegistrationResult> Initialize(CancellationToken cancellationToken);

		/// <summary>
		/// Deregister with the swarm controller or put clients into querying state.
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task"/> representing the running operation.</returns>
		Task Shutdown(CancellationToken cancellationToken);
	}
}
