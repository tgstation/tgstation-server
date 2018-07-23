using System;
using System.Threading;
using System.Threading.Tasks;

using Stream = System.IO.Stream;

namespace Tgstation.Server.Host.Components
{
	/// <summary>
	/// For downloading and installing BYOND extractions for a given system
	/// </summary>
	interface IByondInstaller
	{
		/// <summary>
		/// Get the file name of the DreamDaemon executable
		/// </summary>
		string DreamDaemonName { get; }
		
		/// <summary>
		/// Get the file name of the DreamMaker executable
		/// </summary>
		string DreamMakerName { get; }

		/// <summary>
		/// Download a given BYOND <paramref name="version"/>
		/// </summary>
		/// <param name="version">The <see cref="Version"/> of BYOND to download</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in a <see cref="Stream"/> of the zipfile</returns>
		Task<Stream> DownloadVersion(Version version, CancellationToken cancellationToken);

		/// <summary>
		/// Does actions necessary to get an extracted BYOND installation working
		/// </summary>
		/// <param name="path">The path to the BYOND installation</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns></returns>
		Task InstallByond(string path, CancellationToken cancellationToken);

		/// <summary>
		/// Attempts to cleans the BYOND cache folder for the system
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		Task CleanCache(CancellationToken cancellationToken);
	}
}