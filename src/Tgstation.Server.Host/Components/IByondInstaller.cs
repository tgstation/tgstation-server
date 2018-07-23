using System;
using System.Threading;
using System.Threading.Tasks;

using Stream = System.IO.Stream;

namespace Tgstation.Server.Host.Components
{
	/// <summary>
	/// For downloading and installing BYOND extractions
	/// </summary>
	interface IByondInstaller
	{
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
	}
}