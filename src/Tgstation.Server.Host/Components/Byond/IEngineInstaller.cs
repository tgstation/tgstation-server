using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Tgstation.Server.Api.Models.Internal;

namespace Tgstation.Server.Host.Components.Byond
{
	/// <summary>
	/// For downloading and installing game engines for a given system.
	/// </summary>
	interface IEngineInstaller
	{
		/// <summary>
		/// Get the file name of the compiler executable.
		/// </summary>
		string CompilerName { get; }

		/// <summary>
		/// The path to the folder for the user's data.
		/// </summary>
		string PathToUserFolder { get; }

		/// <summary>
		/// Get the file name of the DreamDaemon executable.
		/// </summary>
		/// <param name="version">The <see cref="ByondVersion"/> of BYOND to select the executable name for.</param>
		/// <param name="supportsCli">Whether or not the returned path supports being run as a command-line application.</param>
		/// <param name="supportsMapThreads">Whether or not the returned path supports the '-map-threads' parameter.</param>
		/// <returns>The file name of the DreamDaemon executable.</returns>
		string GetDreamDaemonName(ByondVersion version, out bool supportsCli, out bool supportsMapThreads);

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
		/// <param name="path">The path to the BYOND installation.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask"/> representing the running operation.</returns>
		ValueTask Install(ByondVersion version, string path, CancellationToken cancellationToken);

		/// <summary>
		/// Does actions necessary to get upgrade a BYOND version installed by a previous version of TGS.
		/// </summary>
		/// <param name="version">The <see cref="ByondVersion"/> being installed.</param>
		/// <param name="path">The path to the BYOND installation.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask"/> representing the running operation.</returns>
		ValueTask UpgradeInstallation(ByondVersion version, string path, CancellationToken cancellationToken);

		/// <summary>
		/// Attempts to cleans the engine's cache folder for the system.
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task"/> representing the running operation.</returns>
		Task CleanCache(CancellationToken cancellationToken);
	}
}
