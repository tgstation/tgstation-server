using Microsoft.Extensions.Hosting;
using System;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Api.Models.Internal;

namespace Tgstation.Server.Host.Components.Watchdog
{
	public interface IWatchdog : IHostedService, IDisposable
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
		/// The latest <see cref="LaunchResult"/> of the twin servers
		/// </summary>
		LaunchResult LastLaunchResult { get; }

		/// <summary>
		/// The <see cref="Models.CompileJob"/> that is currently live
		/// </summary>
		Models.CompileJob LiveCompileJob { get; }

		/// <summary>
		/// The <see cref="Models.CompileJob"/> that is staged to go live
		/// </summary>
		Models.CompileJob StagedCompileJob { get; }

		/// <summary>
		/// The <see cref="DreamDaemonLaunchParameters"/> the active server is using
		/// </summary>
		DreamDaemonLaunchParameters ActiveLaunchParameters { get; }

		/// <summary>
		/// The <see cref="DreamDaemonLaunchParameters"/> to be applied
		/// </summary>
		DreamDaemonLaunchParameters LastLaunchParameters { get; }

		/// <summary>
		/// The <see cref="Components.Watchdog.RebootState"/> of the active server
		/// </summary>
		RebootState? RebootState { get; }

		/// <summary>
		/// Start the <see cref="IWatchdog"/>
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="WatchdogLaunchResult"/> or <see langword="null"/> if it was already running</returns>
		Task<WatchdogLaunchResult> Launch(CancellationToken cancellationToken);

		/// <summary>
		/// Changes the <see cref="ActiveLaunchParameters"/>. If currently <see cref="Running"/> triggers a graceful restart
		/// </summary>
		/// <param name="launchParameters">The new <see cref="DreamDaemonLaunchParameters"/></param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		Task ChangeSettings(DreamDaemonLaunchParameters launchParameters, CancellationToken cancellationToken);

		/// <summary>
		/// Restarts the watchdog
		/// </summary>
		/// <param name="graceful">If <see langword="true"/> the restart will be delayed until a reboot is detected in the active server's DMAPI and this function will retrun immediately</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="WatchdogLaunchResult"/> or <see langword="null"/> if it was already running or <paramref name="graceful"/> is <see langword="true"/> and <see cref="Running"/> is <see langword="false"/></returns>
		Task<WatchdogLaunchResult> Restart(bool graceful, CancellationToken cancellationToken);

		/// <summary>
		/// Stops the watchdog
		/// </summary>
		/// <param name="graceful">If <see langword="true"/> the termination will be delayed until a reboot is detected in the active server's DMAPI and this function will return immediately</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		Task Terminate(bool graceful, CancellationToken cancellationToken);
	}
}
