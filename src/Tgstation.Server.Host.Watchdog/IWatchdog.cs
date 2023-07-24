using System;
using System.Threading;
using System.Threading.Tasks;

namespace Tgstation.Server.Host.Watchdog
{
	/// <summary>
	/// The watchdog for a <see cref="Host"/>.
	/// </summary>
	public interface IWatchdog
	{
		/// <summary>
		/// Gets the current version of the host process. Set once <see cref="RunAsync(bool, string[], CancellationToken)"/> begins and doesn't immediately return <see langword="false"/>.
		/// </summary>
		Version InitialHostVersion { get; }

		/// <summary>
		/// Run the <see cref="IWatchdog"/>.
		/// </summary>
		/// <param name="runConfigure">If the <see cref="IWatchdog"/> should just run the host configuration wizard and exit.</param>
		/// <param name="args">The arguments for the <see cref="IWatchdog"/>.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in <see langword="true"/> if there were no errors, <see langword="false"/> otherwise.</returns>
		Task<bool> RunAsync(bool runConfigure, string[] args, CancellationToken cancellationToken);
	}
}
