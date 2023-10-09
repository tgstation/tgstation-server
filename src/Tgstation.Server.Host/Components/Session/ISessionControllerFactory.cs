using System.Threading;
using System.Threading.Tasks;

using Tgstation.Server.Api.Models.Internal;
using Tgstation.Server.Host.Components.Byond;
using Tgstation.Server.Host.Components.Deployment;

namespace Tgstation.Server.Host.Components.Session
{
	/// <summary>
	/// Factory for <see cref="ISessionController"/>s.
	/// </summary>
	interface ISessionControllerFactory
	{
		/// <summary>
		/// Create a <see cref="ISessionController"/> from a freshly launch DreamDaemon instance.
		/// </summary>
		/// <param name="dmbProvider">The <see cref="IDmbProvider"/> to use.</param>
		/// <param name="currentByondLock">The current <see cref="IEngineExecutableLock"/> if any.</param>
		/// <param name="launchParameters">The <see cref="DreamDaemonLaunchParameters"/> to use. <see cref="DreamDaemonLaunchParameters.SecurityLevel"/> will be updated with the minumum required security level for the launch.</param>
		/// <param name="apiValidate">If the <see cref="ISessionController"/> should only validate the DMAPI then exit.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in a new <see cref="ISessionController"/>.</returns>
		ValueTask<ISessionController> LaunchNew(
			IDmbProvider dmbProvider,
			IEngineExecutableLock currentByondLock,
			DreamDaemonLaunchParameters launchParameters,
			bool apiValidate,
			CancellationToken cancellationToken);

		/// <summary>
		/// Create a <see cref="ISessionController"/> from an existing DreamDaemon instance.
		/// </summary>
		/// <param name="reattachInformation">The <see cref="ReattachInformation"/> to use.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in a new <see cref="ISessionController"/> on success or <see langword="null"/> on failure to reattach.</returns>
		ValueTask<ISessionController> Reattach(
			ReattachInformation reattachInformation,
			CancellationToken cancellationToken);
	}
}
