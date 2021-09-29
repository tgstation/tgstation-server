using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

namespace Tgstation.Server.Host.IO
{
	/// <inheritdoc />
	sealed class FileDownloader : IFileDownloader
	{
		/// <summary>
		/// The <see cref="IHttpClientFactory"/> for the <see cref="FileDownloader"/>.
		/// </summary>
		readonly IHttpClientFactory httpClientFactory;

		/// <summary>
		/// The <see cref="ILogger"/> for the <see cref="FileDownloader"/>.
		/// </summary>
		readonly ILogger<FileDownloader> logger;

		/// <summary>
		/// Initializes a new instance of the <see cref="FileDownloader"/> class.
		/// </summary>
		/// <param name="httpClientFactory">The value of <see cref="httpClientFactory"/>.</param>
		/// <param name="logger">The value of <see cref="logger"/>.</param>
		public FileDownloader(IHttpClientFactory httpClientFactory, ILogger<FileDownloader> logger)
		{
			this.httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
			this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
		}

		/// <inheritdoc />
		public async Task<MemoryStream> DownloadFile(Uri url, CancellationToken cancellationToken)
		{
			if (url == null)
				throw new ArgumentNullException(nameof(url));

			logger.LogDebug("Downloading file {url}...", url);

			using var client = httpClientFactory.CreateClient();
			using var response = await client.GetAsync(url, cancellationToken);

			response.EnsureSuccessStatusCode();

			using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
			var copyStream = new MemoryStream();
			try
			{
				await responseStream.CopyToAsync(copyStream, cancellationToken).ConfigureAwait(false);
				copyStream.Seek(0, SeekOrigin.Begin);
				return copyStream;
			}
			catch
			{
				await copyStream.DisposeAsync();
				throw;
			}
		}
	}
}
