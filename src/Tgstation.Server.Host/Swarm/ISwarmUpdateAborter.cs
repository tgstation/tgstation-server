using System.Threading.Tasks;

namespace Tgstation.Server.Host.Swarm
{
	/// <summary>
	/// Allows aborting a swarm distributed update operation.
	/// </summary>
	public interface ISwarmUpdateAborter
	{
		/// <summary>
		/// Attempt to abort an uncommitted update.
		/// </summary>
		/// <returns>A <see cref="Task"/> representing the running operation.</returns>
		/// <remarks>This method does not accept a <see cref="global::System.Threading.CancellationToken"/> because aborting an update should never be cancelled.</remarks>
		Task AbortUpdate();
	}
}
