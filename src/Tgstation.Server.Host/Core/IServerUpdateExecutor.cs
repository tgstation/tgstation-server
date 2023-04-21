using System.Threading;
using System.Threading.Tasks;

namespace Tgstation.Server.Host.Core
{
	/// <summary>
	/// Executes server update operations.
	/// </summary>
	public interface IServerUpdateExecutor
	{
		/// <summary>
		/// Executes a pending server update by extracting the new server to a given <paramref name="updatePath"/>.
		/// </summary>
		/// <param name="updatePath">The path to extract the update zip to.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for non-critical parts of this operation.</param>
		/// <param name="criticalCancellationToken">The <see cref="CancellationToken"/> for the operation. Must not be trivial as it can desync swarm nodes if fired at an inappropriate time.</param>
		/// <returns>A <see cref="Task"/> representing the running operation.</returns>
		Task<bool> ExecuteUpdate(string updatePath, CancellationToken cancellationToken, CancellationToken criticalCancellationToken);
	}
}
