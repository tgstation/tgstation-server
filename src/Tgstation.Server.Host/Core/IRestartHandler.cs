using System;
using System.Threading;
using System.Threading.Tasks;

#nullable disable

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
		/// <param name="handlerMayDelayShutdownWithExtremelyLongRunningTasks">If <see langword="false"/> the <see cref="IRestartHandler"/> should aim to complete the <see cref="Task"/> returned from this function ASAP.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask"/> representing the running operation.</returns>
		ValueTask HandleRestart(Version updateVersion, bool handlerMayDelayShutdownWithExtremelyLongRunningTasks, CancellationToken cancellationToken);
	}
}
