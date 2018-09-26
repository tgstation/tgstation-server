using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Api.Models.Internal;
using Tgstation.Server.Host.Components.Byond;

namespace Tgstation.Server.Host.Components.Watchdog
{
	/// <summary>
	/// Factory for <see cref="ISessionController"/>s
	/// </summary>
	interface ISessionControllerFactory
	{
		/// <summary>
		/// Create a <see cref="ISessionController"/> from a freshly launch DreamDaemon instance
		/// </summary>
		/// <param name="launchParameters">The <see cref="DreamDaemonLaunchParameters"/> to use. <see cref="DreamDaemonLaunchParameters.SecurityLevel"/> will be updated with the minumum required security level for the launch</param>
		/// <param name="dmbProvider">The <see cref="IDmbProvider"/> to use</param>
		/// <param name="currentByondLock">The current <see cref="IByondExecutableLock"/> if any</param>
		/// <param name="primaryPort">If the <see cref="DreamDaemonLaunchParameters.PrimaryPort"/> of <paramref name="launchParameters"/> should be used</param>
		/// <param name="primaryDirectory">If the <see cref="IDmbProvider.PrimaryDirectory"/> of <paramref name="dmbProvider"/> should be used</param>
		/// <param name="apiValidate">If the <see cref="ISessionController"/> should only validate the DMAPI then exit</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in a new <see cref="ISessionController"/></returns>
		Task<ISessionController> LaunchNew(DreamDaemonLaunchParameters launchParameters, IDmbProvider dmbProvider, IByondExecutableLock currentByondLock, bool primaryPort, bool primaryDirectory, bool apiValidate, CancellationToken cancellationToken);

		/// <summary>
		/// Create a <see cref="ISessionController"/> from an existing DreamDaemon instance
		/// </summary>
		/// <param name="reattachInformation">The <see cref="ReattachInformation"/> to use</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in a new <see cref="ISessionController"/> on success or <see langword="null"/> on failure to reattach</returns>
		Task<ISessionController> Reattach(ReattachInformation reattachInformation, CancellationToken cancellationToken);

		/// <summary>
		/// Creates a <see cref="ISessionController"/> that appears to have started and died with exit code -1
		/// </summary>
		/// <param name="dmbProvider">The <see cref="IDmbProvider"/> for the <see cref="ISessionController"/></param>
		/// <returns>A dead <see cref="ISessionController"/></returns>
		ISessionController CreateDeadSession(IDmbProvider dmbProvider);
	}
}
