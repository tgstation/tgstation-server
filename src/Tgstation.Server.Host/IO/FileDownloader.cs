using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using Tgstation.Server.Api;
using Tgstation.Server.Common;

namespace Tgstation.Server.Host.IO
{
	/// <inheritdoc />
	public sealed class FileDownloader : IFileDownloader
	{
		/// <summary>
		/// The <see cref="IAbstractHttpClientFactory"/> for the <see cref="FileDownloader"/>.
		/// </summary>
		readonly IAbstractHttpClientFactory httpClientFactory;

		/// <summary>
		/// The <see cref="ILogger"/> for the <see cref="FileDownloader"/>.
		/// </summary>
		readonly ILogger<FileDownloader> logger;

		/// <summary>
		/// Initializes a new instance of the <see cref="FileDownloader"/> class.
		/// </summary>
		/// <param name="httpClientFactory">The value of <see cref="httpClientFactory"/>.</param>
		/// <param name="logger">The value of <see cref="logger"/>.</param>
		public FileDownloader(IAbstractHttpClientFactory httpClientFactory, ILogger<FileDownloader> logger)
		{
			this.httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
			this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
		}

		/// <inheritdoc />
		public async Task<Stream> DownloadFile(Uri url, string bearerToken, CancellationToken cancellationToken)
		{
			if (url == null)
				throw new ArgumentNullException(nameof(url));

			logger.LogDebug("Starting download of {url}...", url);
			using var httpClient = httpClientFactory.CreateClient();
			using var request = new HttpRequestMessage(
				HttpMethod.Get,
				url);

			if (bearerToken != null)
				request.Headers.Authorization = new AuthenticationHeaderValue(ApiHeaders.BearerAuthenticationScheme, bearerToken);

			var webRequestTask = httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
			var response = await webRequestTask;
			try
			{
				response.EnsureSuccessStatusCode();
			}
			catch
			{
				response.Dispose();
				throw;
			}

			return await CachedResponseStream.Create(response);
		}
	}
}
