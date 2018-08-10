using System.Threading;
using System.Threading.Tasks;

namespace Tgstation.Server.Host.Components.Interop
{
	/// <summary>
	/// Handles <see cref="CommCommand"/>s
	/// </summary>
	interface ICommHandler
	{
		/// <summary>
		/// Handle a <paramref name="command"/>
		/// </summary>
		/// <param name="command">The <see cref="CommCommand"/> to handle</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		Task HandleInterop(CommCommand command, CancellationToken cancellationToken);
	}
}