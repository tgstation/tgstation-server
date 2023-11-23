using System;
using System.Threading.Tasks;

namespace Tgstation.Server.Host.Core
{
	/// <summary>
	/// Represents a service that may take an updated <see cref="Host"/> assembly and run it, stopping the current assembly in the process.
	/// </summary>
	public interface IServerControl
	{
		/// <summary>
		/// <see langword="true"/> if live updates are supported, <see langword="false"/>. <see cref="TryStartUpdate(IServerUpdateExecutor, Version)"/> and <see cref="Restart"/> will fail if this is <see langword="false"/>.
		/// </summary>
		bool WatchdogPresent { get; }

		/// <summary>
		/// Whether or not the server is currently updating.
		/// </summary>
		bool UpdateInProgress { get; }

		/// <summary>
		/// Attempt to update with a given <paramref name="updateExecutor"/>.
		/// </summary>
		/// <param name="updateExecutor">The <see cref="IServerUpdateExecutor"/> to use for the update.</param>
		/// <param name="newVersion">The <see cref="Version"/> the <see cref="IServerControl"/> is updating to.</param>
		/// <returns><see langword="true"/> if the update started successfully, <see langword="false"/> if there was another update in progress.</returns>
		bool TryStartUpdate(IServerUpdateExecutor updateExecutor, Version newVersion);

		/// <summary>
		/// Register a given <paramref name="handler"/> to run before stopping the server for a restart.
		/// </summary>
		/// <param name="handler">The <see cref="IRestartHandler"/> to register.</param>
		/// <returns>A new <see cref="IRestartRegistration"/> representing the scope of the registration.</returns>
		IRestartRegistration RegisterForRestart(IRestartHandler handler);

		/// <summary>
		/// Restarts the <see cref="Host"/>.
		/// </summary>
		/// <returns>A <see cref="ValueTask"/> representing the running operation.</returns>
		ValueTask Restart();

		/// <summary>
		/// Gracefully shutsdown the <see cref="Host"/>.
		/// </summary>
		/// <param name="detach">If the graceful shutdown should detach any running watchdog. If <see langword="false"/> the server will wait for the next TgsReboot() or world exit before shutting down.</param>
		/// <returns>A <see cref="ValueTask"/> representing the running operation.</returns>
		ValueTask GracefulShutdown(bool detach);

		/// <summary>
		/// Kill the server with a fatal exception.
		/// </summary>
		/// <param name="exception">The <see cref="Exception"/> to propagate to the watchdog if any.</param>
		/// <returns>A <see cref="Task"/> representing the running operation.</returns>
		ValueTask Die(Exception? exception);
	}
}
