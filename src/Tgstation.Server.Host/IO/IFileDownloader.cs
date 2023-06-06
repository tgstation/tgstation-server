using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Tgstation.Server.Host.IO
{
	/// <summary>
	/// Downloads files.
	/// </summary>
	interface IFileDownloader
	{
		/// <summary>
		/// Downloads a file from a given <paramref name="url"/>.
		/// </summary>
		/// <param name="url">The URL to download.</param>
		/// <param name="bearerToken">Optional <see cref="string"/> to use as the "Bearer" value in the optional "Authorization" header for the request.</param>
		/// <param name="cancellationToken">A <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in a <see cref="MemoryStream"/> of the downloaded file.</returns>
		Task<MemoryStream> DownloadFile(Uri url, string bearerToken, CancellationToken cancellationToken);
	}
}
