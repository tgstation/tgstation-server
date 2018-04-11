using System;
using System.Threading;
using System.Threading.Tasks;

namespace Tgstation.Server.Host.Startup
{
	/// <summary>
	/// Represents the host
	/// </summary>
	public interface IServer
	{
        /// <summary>
        /// The path to the updated assembly to run if any. Populated once <see cref="RunAsync(CancellationToken)"/> returns
        /// </summary>
        string UpdatePath { get; }

		/// <summary>
		/// Runs the <see cref="IServer"/>
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		Task RunAsync(CancellationToken cancellationToken);
	}
}
