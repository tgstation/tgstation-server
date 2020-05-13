using System.Threading;
using System.Threading.Tasks;

namespace Tgstation.Server.Host.Components.Session
{
	/// <summary>
	/// Handles saving and loading <see cref="DualReattachInformation"/>
	/// </summary>
	public interface IReattachInfoHandler
	{
		/// <summary>
		/// Save some <paramref name="reattachInformation"/>
		/// </summary>
		/// <param name="reattachInformation">The <see cref="DualReattachInformation"/> to save</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		Task Save(DualReattachInformation reattachInformation, CancellationToken cancellationToken);

		/// <summary>
		/// Load a saved <see cref="DualReattachInformation"/>
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the stored <see cref="DualReattachInformation"/> if any</returns>
		Task<DualReattachInformation> Load(CancellationToken cancellationToken);
	}
}