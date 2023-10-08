using System;
using System.Threading;
using System.Threading.Tasks;

namespace Tgstation.Server.Host.Watchdog
{
	/// <summary>
	/// For relaying signals received to the host process.
	/// </summary>
	public interface ISignalChecker
	{
		/// <summary>
		/// Relays signals received to the host process.
		/// </summary>
		/// <param name="startChild">An <see cref="Func{TResult}"/> to start the main process. It accepts an optional additional command line argument as a paramter and returns it's <see cref="System.Diagnostics.Process.Id"/> and lifetime <see cref="Task"/>.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask"/> representing the running operation.</returns>
		ValueTask CheckSignals(Func<string, (int, Task)> startChild, CancellationToken cancellationToken);
	}
}
