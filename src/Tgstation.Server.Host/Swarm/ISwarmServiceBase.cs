using System.Threading;
using System.Threading.Tasks;

namespace Tgstation.Server.Host.Swarm
{
	/// <summary>
	/// For aborting swarm updates.
	/// </summary>
	public interface ISwarmServiceBase
	{
		/// <summary>
		/// Abort an uncommitted update.
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task"/> representing the running operation.</returns>
		Task AbortUpdate(CancellationToken cancellationToken);
	}
}
