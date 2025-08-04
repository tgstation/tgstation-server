using System;
using System.Net.Http;
using System.Net.Http.Headers;

using Microsoft.Extensions.Logging;

using Tgstation.Server.Api;

namespace Tgstation.Server.Host.IO
{
	/// <inheritdoc />
	public sealed class FileDownloader : IFileDownloader
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
		public IFileStreamProvider DownloadFile(Uri url, string? bearerToken)
		{
			ArgumentNullException.ThrowIfNull(url);

			logger.LogDebug("Starting download of {url}...", url);
			var httpClient = httpClientFactory.CreateClient();
			try
			{
				var request = new HttpRequestMessage(
					HttpMethod.Get,
					url);
				try
				{
					if (bearerToken != null)
						request.Headers.Authorization = new AuthenticationHeaderValue(ApiHeaders.BearerAuthenticationScheme, bearerToken);

					return new RequestFileStreamProvider(httpClient, request);
				}
				catch
				{
					request.Dispose();
					throw;
				}
			}
			catch
			{
				httpClient.Dispose();
				throw;
			}
		}
	}
}
