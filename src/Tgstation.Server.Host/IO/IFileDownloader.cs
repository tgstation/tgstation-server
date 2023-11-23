using System;

#nullable disable

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
		/// <returns>A new <see cref="IFileStreamProvider"/> for the downloaded file.</returns>
		IFileStreamProvider DownloadFile(Uri url, string bearerToken);
	}
}
