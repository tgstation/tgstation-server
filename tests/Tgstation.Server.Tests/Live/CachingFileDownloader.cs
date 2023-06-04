using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Tgstation.Server.Common;
using Tgstation.Server.Host.Extensions;
using Tgstation.Server.Host.IO;
using Tgstation.Server.Host.System;
using Tgstation.Server.Tests.Live.Instance;

namespace Tgstation.Server.Tests.Live
{
	sealed class CachingFileDownloader : IFileDownloader, IDisposable
	{
		static readonly Dictionary<Uri, Tuple<string, bool>> cachedPaths = new Dictionary<Uri, Tuple<string, bool>>();

		readonly ILogger<CachingFileDownloader> logger;

		public CachingFileDownloader(ILogger<CachingFileDownloader> logger)
		{
			this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
			logger.LogTrace("Created");
		}

		public static async Task InitializeAndInject(CancellationToken cancellationToken)
		{
			if (LiveTestUtils.RunningInGitHubActions)
			{
				// actions caches BYOND for us
				var url = new Uri(
					$"https://secure.byond.com/download/build/{ByondTest.TestVersion.Major}/{ByondTest.TestVersion.Major}.{ByondTest.TestVersion.Minor}_byond{(!new PlatformIdentifier().IsWindows ? "_linux" : String.Empty)}.zip");

				var path = $"~/BYOND-{ByondTest.TestVersion.Major}.{ByondTest.TestVersion.Minor}/byond.zip";
				Assert.IsTrue(File.Exists(path));
				System.Console.WriteLine($"CACHE PREWARMED: {url}");
				cachedPaths.Add(url, Tuple.Create(path, false));
			}

			// predownload the target github release update asset
			var gitHubToken = Environment.GetEnvironmentVariable("TGS_TEST_GITHUB_TOKEN");
			if (String.IsNullOrWhiteSpace(gitHubToken))
				gitHubToken = null;

			// this can fail, try a few times
			var succeeded = false;
			using var loggerFactory = LoggerFactory.Create(builder =>
			{
				builder.AddConsole();
				builder.SetMinimumLevel(LogLevel.Trace);
			});
			var logger = loggerFactory.CreateLogger("CachingFileDownloader");
			for (var i = 0; i < 10; ++i)
				try
				{
					var url = new Uri($"https://github.com/tgstation/tgstation-server/releases/download/tgstation-server-v{TestLiveServer.TestUpdateVersion}/ServerUpdatePackage.zip");
					await using var stream = await CacheFile(logger, url, gitHubToken, !LiveTestUtils.RunningInGitHubActions, cancellationToken);
					succeeded = true;
					break;
				}
				catch (Exception ex)
				{
					logger.LogWarning(ex, $"TEST: FAILED TO CACHE GITHUB RELEASE.");
				}

			Assert.IsTrue(succeeded);

			ServiceCollectionExtensions.UseFileDownloader<CachingFileDownloader>();
		}

		public void Dispose()
		{
			logger.LogTrace("Dispose");
			lock (cachedPaths)
			{
				foreach (var pathAndDelete in cachedPaths.Values)
					if (pathAndDelete.Item2)
					{
						logger.LogTrace("Deleting {path}", pathAndDelete.Item1);
						try
						{
							File.Delete(pathAndDelete.Item1);
						}
						catch (Exception ex)
						{
							logger.LogWarning(ex, "Deletion failed");
						}
					}

				cachedPaths.Clear();
			}
		}

		public async Task<MemoryStream> DownloadFile(Uri url, string bearerToken, CancellationToken cancellationToken)
		{
			Tuple<string, bool> tuple;
			lock (cachedPaths)
				if (!cachedPaths.TryGetValue(url, out tuple))
				{
					logger.LogInformation("Cache miss: {url}", url);
					tuple = null;
				}

			if (tuple == null)
				return await CacheFile(logger, url, bearerToken, true, cancellationToken);

			logger.LogTrace("Cache hit: {url}", url);
			var bytes = await new DefaultIOManager().ReadAllBytes(tuple.Item1, cancellationToken);
			return new MemoryStream(bytes);
		}

		static async Task<MemoryStream> CacheFile(ILogger logger, Uri url, string bearerToken, bool temporal, CancellationToken cancellationToken)
		{
			var downloader = new FileDownloader(
				new HttpClientFactory(
					new AssemblyInformationProvider().ProductInfoHeaderValue),
				new Logger<FileDownloader>(
					LiveTestUtils.CreateLoggerFactoryForLogger(
						logger,
						out _)));
			var download = await downloader.DownloadFile(url, bearerToken, cancellationToken);
			try
			{
				var path = Path.GetTempFileName();
				try
				{
					await using var fs = new DefaultIOManager().CreateAsyncSequentialWriteStream(path);
					await download.CopyToAsync(fs, cancellationToken);

					lock (cachedPaths)
						cachedPaths.Add(url, Tuple.Create(path, temporal));

					logger.LogTrace("Cached to {path}", path);
				}
				catch
				{
					File.Delete(path);
					throw;
				}

				download.Seek(0, SeekOrigin.Begin);
				return download;
			}
			catch
			{
				await download.DisposeAsync();
				throw;
			}
		}
	}
}
