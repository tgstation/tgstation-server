using Microsoft.Extensions.Hosting;
using System;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Api.Models.Internal;
using Tgstation.Server.Host.Components.Events;
using Tgstation.Server.Host.Components.Session;

namespace Tgstation.Server.Host.Components.Watchdog
{
	/// <summary>
	/// Runs and monitors the twin server controllers
	/// </summary>
	public interface IWatchdog : IHostedService, IDisposable, IEventConsumer, IRenameNotifyee
	{
		/// <summary>
		/// If the watchdog is running
		/// </summary>
		bool Running { get; }

		/// <summary>
		/// If the alpha server is the active server
		/// </summary>
		bool AlphaIsActive { get; }

		/// <summary>
		/// The <see cref="CompileJob"/> currently running on the server
		/// </summary>
		Models.CompileJob ActiveCompileJob { get; }

		/// <summary>
		/// The <see cref="DreamDaemonLaunchParameters"/> the active server is using
		/// </summary>
		DreamDaemonLaunchParameters ActiveLaunchParameters { get; }

		/// <summary>
		/// The <see cref="DreamDaemonLaunchParameters"/> to be applied
		/// </summary>
		DreamDaemonLaunchParameters LastLaunchParameters { get; }

		/// <summary>
		/// The <see cref="Session.RebootState"/> of the active server
		/// </summary>
		RebootState? RebootState { get; }

		/// <summary>
		/// Start the <see cref="IWatchdog"/>
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		Task Launch(CancellationToken cancellationToken);

		/// <summary>
		/// Changes the <see cref="ActiveLaunchParameters"/>. If currently <see cref="Running"/> triggers a graceful restart
		/// </summary>
		/// <param name="launchParameters">The new <see cref="DreamDaemonLaunchParameters"/>. May be modified</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		Task ChangeSettings(DreamDaemonLaunchParameters launchParameters, CancellationToken cancellationToken);

		/// <summary>
		/// Restarts the watchdog
		/// </summary>
		/// <param name="graceful">If <see langword="true"/> the restart will be delayed until a reboot is detected in the active server's DMAPI and this function will retrun immediately</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		Task Restart(bool graceful, CancellationToken cancellationToken);

		/// <summary>
		/// Stops the watchdog
		/// </summary>
		/// <param name="graceful">If <see langword="true"/> the termination will be delayed until a reboot is detected in the active server's DMAPI and this function will return immediately</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		Task Terminate(bool graceful, CancellationToken cancellationToken);

		/// <summary>
		/// Cancels pending graceful actions
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		Task ResetRebootState(CancellationToken cancellationToken);
	}
}
