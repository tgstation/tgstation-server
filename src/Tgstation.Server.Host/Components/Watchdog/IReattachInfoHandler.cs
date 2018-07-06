using System.Threading;
using System.Threading.Tasks;

namespace Tgstation.Server.Host.Components.Watchdog
{
	/// <summary>
	/// Handles saving and loading <see cref="WatchdogReattachInformation"/>
	/// </summary>
	interface IReattachInfoHandler
	{
		/// <summary>
		/// Save some <paramref name="reattachInformation"/>
		/// </summary>
		/// <param name="reattachInformation">The <see cref="WatchdogReattachInformation"/> to save</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		Task Save(WatchdogReattachInformation reattachInformation, CancellationToken cancellationToken);

		/// <summary>
		/// Load a saved <see cref="WatchdogReattachInformation"/>
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the stored <see cref="WatchdogReattachInformation"/> if any</returns>
		Task<WatchdogReattachInformation> Load(CancellationToken cancellationToken);
	}
}