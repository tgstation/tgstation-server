using System;
using System.Threading;
using System.Threading.Tasks;

namespace Tgstation.Server.Host.Core
{
	/// <summary>
	/// Handler for server restarts.
	/// </summary>
	public interface IRestartHandler
	{
		/// <summary>
		/// Handle a restart of the server.
		/// </summary>
		/// <param name="updateVersion">The <see cref="Version"/> being updated to, <see langword="null"/> if not being changed.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task"/> representing the running operation.</returns>
		Task HandleRestart(Version updateVersion, CancellationToken cancellationToken);
	}
}
