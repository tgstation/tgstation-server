using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Tgstation.Server.Host.IO
{
	/// <summary>
	/// File download helper.
	/// </summary>
	public interface IFileDownloader
	{
		/// <summary>
		/// Downloads a file from <paramref name="url"/>.
		/// </summary>
		/// <param name="url">The URL to download.</param>
		/// <param name="cancellationToken">A <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="MemoryStream"/> of the downloaded file.</returns>
		Task<MemoryStream> DownloadFile(Uri url, CancellationToken cancellationToken);
	}
}
