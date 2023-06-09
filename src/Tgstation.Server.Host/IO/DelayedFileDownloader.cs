using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Tgstation.Server.Host.IO
{
	/// <summary>
	/// Implements a <see cref="IFileStreamProvider"/> over a <see cref="IFileDownloader"/>.
	/// </summary>
	sealed class DelayedFileDownloader : IFileStreamProvider
	{
		/// <summary>
		/// The <see cref="IFileDownloader"/> for the <see cref="DelayedFileDownloader"/>.
		/// </summary>
		readonly IFileDownloader fileDownloader;

		/// <summary>
		/// The <see cref="Uri"/> of the file to download.
		/// </summary>
		readonly Uri downloadUrl;

		/// <summary>
		/// The optional bearer token to use when downloading the file.
		/// </summary>
		readonly string bearerToken;

		/// <summary>
		/// The <see cref="CancellationTokenSource"/> used to abort the download.
		/// </summary>
		readonly CancellationTokenSource downloadCts;

		/// <summary>
		/// The <see cref="Task{TResult}"/> resulting in the downloaded <see cref="MemoryStream"/>.
		/// </summary>
		Task<MemoryStream> downloadTask;

		/// <summary>
		/// Initializes a new instance of the <see cref="DelayedFileDownloader"/> class.
		/// </summary>
		/// <param name="fileDownloader">The value of <see cref="fileDownloader"/>.</param>
		/// <param name="downloadUrl">The value of <see cref="downloadUrl"/>.</param>
		/// <param name="bearerToken">The optional value of <see cref="bearerToken"/>.</param>
		public DelayedFileDownloader(IFileDownloader fileDownloader, Uri downloadUrl, string bearerToken)
		{
			this.fileDownloader = fileDownloader ?? throw new ArgumentNullException(nameof(fileDownloader));
			this.downloadUrl = downloadUrl ?? throw new ArgumentNullException(nameof(fileDownloader));
			this.bearerToken = bearerToken;

			downloadCts = new CancellationTokenSource();
		}

		/// <inheritdoc />
		public async ValueTask DisposeAsync()
		{
			Task<MemoryStream> localDownloadTask;
			lock (downloadCts)
			{
				if (downloadTask == null)
					return;

				localDownloadTask = downloadTask;
				downloadTask = null;
			}

			downloadCts.Cancel();
			downloadCts.Dispose();

			MemoryStream result;
			try
			{
				result = await localDownloadTask;
			}
			catch
			{
				// Unsightly yes, but, if we're here, that means someone called GetResult. So, either they handled the error or decided to ignore it
				return;
			}

			await result.DisposeAsync();
		}

		/// <inheritdoc />
		public async Task<Stream> GetResult(CancellationToken cancellationToken)
		{
			Task<MemoryStream> localTask;
			using (cancellationToken.Register(() => downloadCts.Cancel()))
			{
				lock (downloadCts)
				{
					downloadTask ??= fileDownloader.DownloadFile(downloadUrl, bearerToken, downloadCts.Token);
					localTask = downloadTask;
				}

				return await localTask;
			}
		}
	}
}
