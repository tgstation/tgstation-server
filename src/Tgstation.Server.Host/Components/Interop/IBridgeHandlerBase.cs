using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Host.Components.Interop.Bridge;

namespace Tgstation.Server.Host.Components.Interop
{
	/// <summary>
	/// Handler for <see cref="BridgeParameters"/>.
	/// </summary>
	public interface IBridgeHandlerBase
	{
		/// <summary>
		/// Handle a set of bridge <paramref name="parameters"/>.
		/// </summary>
		/// <param name="parameters">The <see cref="BridgeParameters"/> to handle.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task"/> representing the running operation.</returns>
		Task<BridgeResponse> ProcessBridgeRequest(BridgeParameters parameters, CancellationToken cancellationToken);
	}
}