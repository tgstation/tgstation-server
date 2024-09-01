using System;
using System.Threading;
using System.Threading.Tasks;

using Tgstation.Server.Api.Models;
using Tgstation.Server.Api.Models.Internal;
using Tgstation.Server.Host.Components.Events;
using Tgstation.Server.Host.Components.Session;

namespace Tgstation.Server.Host.Components.Watchdog
{
	/// <summary>
	/// Runs and monitors the twin server controllers.
	/// </summary>
	public interface IWatchdog : IComponentService, IAsyncDisposable, IEventConsumer, IRenameNotifyee
	{
		/// <summary>
		/// An incrementing ID for representing current server execution.
		/// </summary>
		long? SessionId { get; }

		/// <summary>
		/// When the current server executions was started.
		/// </summary>
		DateTimeOffset? LaunchTime { get; }

		/// <summary>
		/// The current <see cref="WatchdogStatus"/>.
		/// </summary>
		WatchdogStatus Status { get; }

		/// <summary>
		/// Gets the memory usage of the game server in bytes.
		/// </summary>
		long? MemoryUsage { get; }

		/// <summary>
		/// If the alpha server is the active server.
		/// </summary>
		bool AlphaIsActive { get; }

		/// <summary>
		/// Retrieves the <see cref="Models.CompileJob"/> currently running on the server.
		/// </summary>
		Models.CompileJob? ActiveCompileJob { get; }

		/// <summary>
		/// The <see cref="DreamDaemonLaunchParameters"/> to be applied.
		/// </summary>
		DreamDaemonLaunchParameters ActiveLaunchParameters { get; }

		/// <summary>
		/// The <see cref="DreamDaemonLaunchParameters"/> the active server is using.
		/// </summary>
		/// <remarks>This may not be the exact same as <see cref="ActiveLaunchParameters"/> but still be associated with the same session.</remarks>
		DreamDaemonLaunchParameters? LastLaunchParameters { get; }

		/// <summary>
		/// The <see cref="Session.RebootState"/> of the active server.
		/// </summary>
		RebootState? RebootState { get; }

		/// <summary>
		/// Start the <see cref="IWatchdog"/>.
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask"/> representing the running operation.</returns>
		ValueTask Launch(CancellationToken cancellationToken);

		/// <summary>
		/// Changes the <see cref="ActiveLaunchParameters"/>. If currently running, may trigger a graceful restart.
		/// </summary>
		/// <param name="launchParameters">The new <see cref="DreamDaemonLaunchParameters"/>. May be modified.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in <see langword="true"/> if a reboot is required, <see langword="false"/> otherwise.</returns>
		ValueTask<bool> ChangeSettings(DreamDaemonLaunchParameters launchParameters, CancellationToken cancellationToken);

		/// <summary>
		/// Restarts the watchdog.
		/// </summary>
		/// <param name="graceful">If <see langword="true"/> the restart will be delayed until a reboot is detected in the active server's DMAPI and this function will retrun immediately.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask"/> representing the running operation.</returns>
		ValueTask Restart(bool graceful, CancellationToken cancellationToken);

		/// <summary>
		/// Stops the watchdog.
		/// </summary>
		/// <param name="graceful">If <see langword="true"/> the termination will be delayed until a reboot is detected in the active server's DMAPI and this function will return immediately.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask"/> representing the running operation.</returns>
		ValueTask Terminate(bool graceful, CancellationToken cancellationToken);

		/// <summary>
		/// Cancels pending graceful actions.
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask"/> representing the running operation.</returns>
		ValueTask ResetRebootState(CancellationToken cancellationToken);

		/// <summary>
		/// Attempt to create a process dump for DreamDaemon.
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask"/> representing the running operation.</returns>
		ValueTask CreateDump(CancellationToken cancellationToken);

		/// <summary>
		/// Send a broadcast <paramref name="message"/> to the DMAPI.
		/// </summary>
		/// <param name="message">The message to broadcast.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in <see langword="true"/> if the broadcast succeeded., <see langword="false"/> otherwise.</returns>
		ValueTask<bool> Broadcast(string message, CancellationToken cancellationToken);
	}
}
