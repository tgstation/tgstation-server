using System.Threading;
using System.Threading.Tasks;

namespace Tgstation.Server.Api.Hubs
{
	/// <summary>
	/// Hub for handling communication errors.
	/// </summary>
	public interface IErrorHandlingHub
	{
		/// <summary>
		/// Called if a hub connection or call is attempted with an invalid or unauthorized token. After calling this, the connection is aborted.
		/// </summary>
		/// <param name="reason">The <see cref="ConnectionAbortReason"/>.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask"/> representing the running operation.</returns>
		Task AbortingConnection(ConnectionAbortReason reason, CancellationToken cancellationToken);
	}
}
