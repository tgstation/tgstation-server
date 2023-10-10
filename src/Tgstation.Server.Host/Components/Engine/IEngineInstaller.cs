using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Tgstation.Server.Api.Models.Internal;

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
		/// <param name="version">The <see cref="ByondVersion"/> of the installation.</param>
		/// <param name="installationTask">The <see cref="Task"/> representing the installation process for the installation.</param>
		/// <returns>The <see cref="IEngineInstallation"/>.</returns>
		IEngineInstallation CreateInstallation(ByondVersion version, Task installationTask);

		/// <summary>
		/// Download a given engine <paramref name="version"/>.
		/// </summary>
		/// <param name="version">The <see cref="ByondVersion"/> of BYOND to download.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in a <see cref="MemoryStream"/> of the zipfile.</returns>
		ValueTask<MemoryStream> DownloadVersion(ByondVersion version, CancellationToken cancellationToken);

		/// <summary>
		/// Does actions necessary to get an extracted installation working.
		/// </summary>
		/// <param name="version">The <see cref="ByondVersion"/> being installed.</param>
		/// <param name="path">The path to the installation.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask"/> representing the running operation.</returns>
		ValueTask Install(ByondVersion version, string path, CancellationToken cancellationToken);

		/// <summary>
		/// Does actions necessary to get upgrade a version installed by a previous version of TGS.
		/// </summary>
		/// <param name="version">The <see cref="ByondVersion"/> being installed.</param>
		/// <param name="path">The path to the installation.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask"/> representing the running operation.</returns>
		ValueTask UpgradeInstallation(ByondVersion version, string path, CancellationToken cancellationToken);

		/// <summary>
		/// Add a given <paramref name="fullDmbPath"/> to the trusted DMBs list in BYOND's config.
		/// </summary>
		/// <param name="fullDmbPath">Full path to the .dmb that should be trusted.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask"/> representing the running operation.</returns>
		ValueTask TrustDmbPath(string fullDmbPath, CancellationToken cancellationToken);

		/// <summary>
		/// Attempts to cleans the engine's cache folder for the system.
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task"/> representing the running operation.</returns>
		Task CleanCache(CancellationToken cancellationToken);
	}
}
