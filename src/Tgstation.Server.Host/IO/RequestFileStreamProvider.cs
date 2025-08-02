using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

using Tgstation.Server.Common.Http;

namespace Tgstation.Server.Host.IO
{
	/// <summary>
	/// A <see cref="IFileStreamProvider"/> that represents the response of <see cref="HttpRequestMessage"/>s.
	/// </summary>
	sealed class RequestFileStreamProvider : IFileStreamProvider
	{
		/// <summary>
		/// The <see cref="HttpClient"/> for the <see cref="RequestFileStreamProvider"/>.
		/// </summary>
		readonly HttpClient httpClient;

		/// <summary>
		/// The <see cref="IFileDownloader"/> for the <see cref="RequestFileStreamProvider"/>.
		/// </summary>
		readonly HttpRequestMessage requestMessage;

		/// <summary>
		/// The <see cref="CancellationTokenSource"/> used to abort the download.
		/// </summary>
		readonly CancellationTokenSource downloadCts;

		/// <summary>
		/// The <see cref="Task{TResult}"/> resulting in the downloaded <see cref="MemoryStream"/>.
		/// </summary>
		Task<CachedResponseStream>? downloadTask;

		/// <summary>
		/// If <see cref="DisposeAsync"/> has been called.
		/// </summary>
		volatile bool disposed;

		/// <summary>
		/// Initializes a new instance of the <see cref="RequestFileStreamProvider"/> class.
		/// </summary>
		/// <param name="httpClient">The value of <see cref="httpClient"/>.</param>
		/// <param name="requestMessage">The value of <see cref="requestMessage"/>.</param>
		public RequestFileStreamProvider(HttpClient httpClient, HttpRequestMessage requestMessage)
		{
			this.httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
			this.requestMessage = requestMessage ?? throw new ArgumentNullException(nameof(requestMessage));

			downloadCts = new CancellationTokenSource();
		}

		/// <inheritdoc />
		public async ValueTask DisposeAsync()
		{
			Task<CachedResponseStream>? localDownloadTask;
			lock (downloadCts)
			{
				if (disposed)
					return;

				disposed = true;

				localDownloadTask = downloadTask;
				downloadTask = null;
			}

			downloadCts.Cancel();
			downloadCts.Dispose();

			if (localDownloadTask != null)
			{
				CachedResponseStream result;
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

			requestMessage.Dispose();
			httpClient.Dispose();
		}

		/// <inheritdoc />
		public async ValueTask<Stream> GetResult(CancellationToken cancellationToken)
		{
			if (disposed)
				throw new ObjectDisposedException(nameof(RequestFileStreamProvider));

			Task<CachedResponseStream> localTask;
			using (cancellationToken.Register(() => downloadCts.Cancel()))
			{
				lock (downloadCts)
				{
					downloadTask ??= InitiateDownload(downloadCts.Token);
					localTask = downloadTask;
				}

				return await localTask;
			}
		}

		/// <summary>
		/// Initiate the download.
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the generated <see cref="CachedResponseStream"/>.</returns>
		async Task<CachedResponseStream> InitiateDownload(CancellationToken cancellationToken)
		{
			var response = await httpClient.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
			try
			{
				response.EnsureSuccessStatusCode();
				return await CachedResponseStream.Create(response);
			}
			catch
			{
				response.Dispose();
				throw;
			}
		}
	}
}
