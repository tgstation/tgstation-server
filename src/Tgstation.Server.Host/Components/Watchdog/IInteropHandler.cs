using System.Threading;
using System.Threading.Tasks;

namespace Tgstation.Server.Host.Components.Watchdog
{
	/// <summary>
	/// Handles <see cref="InteropCommand"/>s
	/// </summary>
	interface IInteropHandler
	{
		/// <summary>
		/// Handle a <paramref name="command"/>
		/// </summary>
		/// <param name="command">The <see cref="InteropCommand"/> to handle</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		Task HandleInterop(InteropCommand command, CancellationToken cancellationToken);
	}
}