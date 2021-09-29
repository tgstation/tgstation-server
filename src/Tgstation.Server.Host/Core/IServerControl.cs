using System;
using System.Threading;
using System.Threading.Tasks;

using Tgstation.Server.Host.IO;

namespace Tgstation.Server.Host.Core
{
	/// <summary>
	/// Represents a service that may take an updated <see cref="Host"/> assembly and run it, stopping the current assembly in the process.
	/// </summary>
	public interface IServerControl
	{
		/// <summary>
		/// <see langword="true"/> if live updates are supported, <see langword="false"/>. <see cref="ApplyUpdate(Version, Uri, IIOManager, IFileDownloader)"/> and <see cref="Restart"/> will fail if this is <see langword="false"/>.
		/// </summary>
		bool WatchdogPresent { get; }

		/// <summary>
		/// Whether or not the server is currently updating.
		/// </summary>
		bool UpdateInProgress { get; }

		/// <summary>
		/// Run a new <see cref="Host"/> assembly and stop the current one. This will likely trigger all active <see cref="CancellationToken"/>s.
		/// </summary>
		/// <param name="version">The <see cref="Version"/> the <see cref="IServerControl"/> is updating to.</param>
		/// <param name="updateZipUrl">The <see cref="Uri"/> that points to the .zip file that contains the new <see cref="Host"/> assembly.</param>
		/// <param name="ioManager">The <see cref="IIOManager"/> for the operation.</param>
		/// <param name="fileDownloader">The <see cref="IFileDownloader"/> for the operation.</param>
		/// <returns><see langword="true"/> if the update started successfully, <see langword="false"/> if there was another update in progress.</returns>
		bool ApplyUpdate(Version version, Uri updateZipUrl, IIOManager ioManager, IFileDownloader fileDownloader);

		/// <summary>
		/// Register a given <paramref name="handler"/> to run before stopping the server for a restart.
		/// </summary>
		/// <param name="handler">The <see cref="IRestartHandler"/> to register.</param>
		/// <returns>A new <see cref="IRestartRegistration"/> representing the scope of the registration.</returns>
		IRestartRegistration RegisterForRestart(IRestartHandler handler);

		/// <summary>
		/// Restarts the <see cref="Host"/>.
		/// </summary>
		/// <returns>A <see cref="Task"/> representing the running operation.</returns>
		Task Restart();

		/// <summary>
		/// Gracefully shutsdown the <see cref="Host"/>.
		/// </summary>
		/// <returns>A <see cref="Task"/> representing the running operation.</returns>
		Task GracefulShutdown();

		/// <summary>
		/// Kill the server with a fatal exception.
		/// </summary>
		/// <param name="exception">The <see cref="Exception"/> to propagate to the watchdog if any.</param>
		/// <returns>A <see cref="Task"/> representing the running operation.</returns>
		Task Die(Exception exception);
	}
}
