using System.Threading;
using System.Threading.Tasks;

namespace Tgstation.Server.Host.Components.Interop.Bridge
{
	/// <summary>
	/// Handler for <see cref="BridgeParameters"/>.
	/// </summary>
	public interface IBridgeDispatcher
	{
		/// <summary>
		/// Handle a set of bridge <paramref name="parameters"/>.
		/// </summary>
		/// <param name="parameters">The <see cref="BridgeParameters"/> to handle.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="BridgeResponse"/> for the request or <see langword="null"/> if the request could not be dispatched.</returns>
		Task<BridgeResponse?> ProcessBridgeRequest(BridgeParameters parameters, CancellationToken cancellationToken);
	}
}
