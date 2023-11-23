using System.Threading;
using System.Threading.Tasks;

#nullable disable

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
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in <see langword="true"/> if the update was successful, <see langword="false"/> otherwise.</returns>
		ValueTask<bool> ExecuteUpdate(string updatePath, CancellationToken cancellationToken, CancellationToken criticalCancellationToken);
	}
}
