using Microsoft.Extensions.Hosting;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Api.Models;
using Tgstation.Server.Api.Models.Internal;

namespace Tgstation.Server.Host.Components
{
	/// <summary>
	/// For managing DreamDaemon
	/// </summary>
	interface IDreamDaemon : IHostedService
	{
		/// <summary>
		/// If DreamDaemon is running
		/// </summary>
		bool Running { get; }

		/// <summary>
		/// If DreamDaemon is going to soft reboot
		/// </summary>
		bool SoftRebooting { get; }

		/// <summary>
		/// If DreamDaemon is going to soft stop
		/// </summary>
		bool SoftStopping { get; }

		/// <summary>
		/// The port DreamDaemon is currently running on
		/// </summary>
		ushort? CurrentPort { get; }

		/// <summary>
		/// The current <see cref="DreamDaemonSecurity"/> of <see cref="DreamDaemon"/>
		/// </summary>
		DreamDaemonSecurity? CurrentSecurity { get; }

		/// <summary>
		/// The access token used for communication with the DMAPI
		/// </summary>
		string AccessToken { get; }

		/// <summary>
		/// Launch DreamDaemon
		/// </summary>
		/// <param name="launchParameters">The <see cref="DreamDaemonLaunchParameters"/> for the launch</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		Task Launch(DreamDaemonLaunchParameters launchParameters, CancellationToken cancellationToken);

		/// <summary>
		/// Changes the <see cref="DreamDaemonLaunchParameters"/> if currently <see cref="Running"/>. Triggers a graceful restart
		/// </summary>
		/// <param name="launchParameters">The new <see cref="DreamDaemonLaunchParameters"/></param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		Task ChangeSettings(DreamDaemonLaunchParameters launchParameters, CancellationToken cancellationToken);

		/// <summary>
		/// Restarts DreamDaemon
		/// </summary>
		/// <param name="graceful">If <see langword="true"/> the restart will be delayed until a reboot is detected in the DMAPI and this function will retrun immediately. If the DMAPI isn't installed, this parameter is ignored</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		Task Restart(bool graceful, CancellationToken cancellationToken);

		/// <summary>
		/// Terminates DreamDaemon
		/// </summary>
		/// <param name="graceful">If <see langword="true"/> the termination will be delayed until a reboot is detected in the DMAPI and this function will return immediately. If the DMAPI isn't installed, this parameter is ignored</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		Task Terminate(bool graceful, CancellationToken cancellationToken);

		/// <summary>
		/// Cancels any pending graceful reboots or terminations
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		Task CancelGracefulActions(CancellationToken cancellationToken);
	}
}