using System.Threading;
using System.Threading.Tasks;

using Tgstation.Server.Api.Models;
using Tgstation.Server.Host.Jobs;

namespace Tgstation.Server.Host.Components.Engine
{
	/// <summary>
	/// For downloading and installing game engines for a given system.
	/// </summary>
	interface IEngineInstaller
	{
		/// <summary>
		/// Creates an <see cref="IEngineInstallation"/> for a given <paramref name="version"/>.
		/// </summary>
		/// <param name="version">The <see cref="EngineVersion"/> of the installation.</param>
		/// <param name="path">The path to the installation.</param>
		/// <param name="installationTask">The <see cref="Task"/> representing the installation process for the installation.</param>
		/// <returns>The <see cref="IEngineInstallation"/>.</returns>
		IEngineInstallation CreateInstallation(EngineVersion version, string path, Task installationTask);

		/// <summary>
		/// Download a given engine <paramref name="version"/>.
		/// </summary>
		/// <param name="version">The <see cref="EngineVersion"/> of the engine to download.</param>
		/// <param name="jobProgressReporter">The <see cref="JobProgressReporter"/> for the operation.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in the <see cref="IEngineInstallationData"/> for the download.</returns>
		ValueTask<IEngineInstallationData> DownloadVersion(EngineVersion version, JobProgressReporter jobProgressReporter, CancellationToken cancellationToken);

		/// <summary>
		/// Does actions necessary to get an extracted installation working.
		/// </summary>
		/// <param name="version">The <see cref="EngineVersion"/> being installed.</param>
		/// <param name="path">The path to the installation.</param>
		/// <param name="deploymentPipelineProcesses">If the operation should consider processes it launches to be part of the deployment pipeline.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask"/> representing the running operation.</returns>
		ValueTask Install(EngineVersion version, string path, bool deploymentPipelineProcesses, CancellationToken cancellationToken);

		/// <summary>
		/// Does actions necessary to get upgrade a version installed by a previous version of TGS.
		/// </summary>
		/// <param name="version">The <see cref="EngineVersion"/> being installed.</param>
		/// <param name="path">The path to the installation.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask"/> representing the running operation.</returns>
		ValueTask UpgradeInstallation(EngineVersion version, string path, CancellationToken cancellationToken);

		/// <summary>
		/// Add a given <paramref name="fullDmbPath"/> to the trusted DMBs list in BYOND's config.
		/// </summary>
		/// <param name="version">The <see cref="EngineVersion"/> being used.</param>
		/// <param name="fullDmbPath">Full path to the .dmb that should be trusted.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask"/> representing the running operation.</returns>
		ValueTask TrustDmbPath(EngineVersion version, string fullDmbPath, CancellationToken cancellationToken);

		/// <summary>
		/// Attempts to cleans the engine's cache folder for the system.
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task"/> representing the running operation.</returns>
		Task CleanCache(CancellationToken cancellationToken);
	}
}
