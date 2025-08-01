using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Moq;

using Tgstation.Server.Api.Models;
using Tgstation.Server.Api.Models.Internal;
using Tgstation.Server.Common.Http;
using Tgstation.Server.Host.Components.Engine;
using Tgstation.Server.Host.Configuration;
using Tgstation.Server.Host.Extensions;
using Tgstation.Server.Host.IO;
using Tgstation.Server.Host.System;
using Tgstation.Server.Host.Utils;
using Tgstation.Server.Tests.Live;
using Tgstation.Server.Tests.Live.Instance;

namespace Tgstation.Server.Tests
{
	public sealed class CachingFileDownloader : IFileDownloader
	{
		static readonly Dictionary<string, Tuple<string, bool>> cachedPaths = new();
		static readonly SemaphoreSlim cachingSemaphore = new(1);

		readonly ILogger<CachingFileDownloader> logger;

		public CachingFileDownloader(ILogger<CachingFileDownloader> logger)
		{
			this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
			logger.LogTrace("Created");
		}

		public static async Task InitializeAndInjectForLiveTests(CancellationToken cancellationToken)
		{
			using var loggerFactory = LoggerFactory.Create(builder =>
			{
				builder.AddConsole();
				builder.SetMinimumLevel(LogLevel.Trace);
			});
			var logger = loggerFactory.CreateLogger("CachingFileDownloader");

			var cfd = new CachingFileDownloader(loggerFactory.CreateLogger<CachingFileDownloader>());

			// this also will inject the edge version
			var edgeVersion = await EngineTest.GetEdgeVersion(Api.Models.EngineType.Byond, logger, cfd, cancellationToken);

			// predownload the target github release update asset
			var gitHubToken = Environment.GetEnvironmentVariable("TGS_TEST_GITHUB_TOKEN");
			if (string.IsNullOrWhiteSpace(gitHubToken))
				gitHubToken = null;

			// this can fail, try a few times
			var succeeded = false;
			for (var i = 0; i < 10; ++i)
				try
				{
					var url = new Uri($"https://github.com/tgstation/tgstation-server/releases/download/tgstation-server-v{TestLiveServer.TestUpdateVersion}/ServerUpdatePackage.zip");
					await using var stream = await CacheFile(logger, url, gitHubToken, null, cancellationToken);
					succeeded = true;
					break;
				}
				catch (Exception ex)
				{
					logger.Log(
						i == 9
							? LogLevel.Error
							: LogLevel.Warning,
						ex,
						"TEST: FAILED TO CACHE GITHUB RELEASE.");
				}

			Assert.IsTrue(succeeded);

			ServiceCollectionExtensions.UseFileDownloader<CachingFileDownloader>();
		}

		public static async ValueTask InitializeByondVersion(ILogger logger, Version byondVersion, bool windows, CancellationToken cancellationToken, string urlCacheOverrideTemplate = null)
		{
			var version = new EngineVersion
			{
				Engine = Api.Models.EngineType.Byond,
				Version = byondVersion,
			};

			var urlTemplate = TestingUtils.ByondZipDownloadTemplate;

			var url = ByondInstallerBase.GetDownloadZipUrl(byondVersion, urlTemplate, new PlatformIdentifier().IsWindows ? "Windows" : "Linux");
			string path = null;
			string basePath = Environment.GetEnvironmentVariable("TGS_TEST_BYOND_ZIPS_BASE_PATH");
			if (basePath == null && TestingUtils.RunningInGitHubActions)
			{
				// actions is supposed to cache BYOND for us here
				basePath = Path.Combine(
					Environment.GetFolderPath(
						Environment.SpecialFolder.UserProfile,
						Environment.SpecialFolderOption.DoNotVerify),
					"byond-zips-cache");
			}

			if (basePath != null)
			{
				var dir = Path.Combine(
					basePath,
					"live",
					windows ? "windows" : "linux");
				path = Path.Combine(
					dir,
					$"{version.Version.Major}.{version.Version.Minor}",
					$"{version.Version.Major}.{version.Version.Minor}.zip");
			}

			Uri overrideUrl = null;
			if (urlCacheOverrideTemplate != null)
			{
				overrideUrl = url;
				url = ByondInstallerBase.GetDownloadZipUrl(byondVersion, urlCacheOverrideTemplate, new PlatformIdentifier().IsWindows ? "Windows" : "Linux");
			}

			await (await CacheFile(
				logger,
				url,
				null,
				path,
				cancellationToken))
				.DisposeAsync();

			if (overrideUrl != null)
				cachedPaths[overrideUrl.ToString()] = cachedPaths[url.ToString()];
		}

		public static void Cleanup()
		{
			lock (cachedPaths)
			{
				foreach (var pathAndDelete in cachedPaths.Values)
					if (pathAndDelete.Item2)
						try
						{
							File.Delete(pathAndDelete.Item1);
						}
						catch
						{
						}

				cachedPaths.Clear();
			}
		}

		class ProviderPackage : IFileStreamProvider
		{
			readonly ILogger logger;
			readonly Uri url;
			readonly string bearerToken;

			public ProviderPackage(ILogger logger, Uri url, string bearerToken)
			{
				this.logger = logger;
				this.url = url;
				this.bearerToken = bearerToken;
			}

			public ValueTask DisposeAsync() => ValueTask.CompletedTask;

			public async ValueTask<Stream> GetResult(CancellationToken cancellationToken)
				=> await CacheFile(logger, url, bearerToken, null, cancellationToken);
		}

		public IFileStreamProvider DownloadFile(Uri url, string bearerToken) => new ProviderPackage(logger, url, bearerToken);

		public static FileDownloader CreateRealDownloader(ILogger logger)
		{
			var mockHttpClientFactory = new Mock<IHttpClientFactory>();
			mockHttpClientFactory.Setup(x => x.CreateClient(String.Empty)).Returns(
				() =>
				{
					var client = new HttpClient();
					client.DefaultRequestHeaders.UserAgent.Add(new AssemblyInformationProvider().ProductInfoHeaderValue);
					return client;
				});

			return new (
				mockHttpClientFactory.Object,
				logger != null
					? new Logger<FileDownloader>(
						TestingUtils.CreateLoggerFactoryForLogger(
							logger,
							out _))
					: Mock.Of<ILogger<FileDownloader>>());
		}

		static async Task<MemoryStream> CacheFile(ILogger logger, Uri url, string bearerToken, string path, CancellationToken cancellationToken)
		{
			using (await SemaphoreSlimContext.Lock(cachingSemaphore, cancellationToken))
			{
				if (cachedPaths.TryGetValue(url.ToString(), out var tuple))
				{
					logger.LogTrace("Cache hit: {url}", url);
					var bytes = await File.ReadAllBytesAsync(tuple.Item1, cancellationToken);
					return new MemoryStream(bytes);
				}

				var temporal = path == null;
				if(!temporal && File.Exists(path))
				{
					cachedPaths.Add(url.ToString(), Tuple.Create(path, false));
					logger.LogTrace("Cache pre-warmed: {url}", url);
					var bytes = await File.ReadAllBytesAsync(path, cancellationToken);
					return new MemoryStream(bytes);
				}

				logger.LogTrace("Cache miss: {url}", url);

				var downloader = CreateRealDownloader(logger);
				await using var download = downloader.DownloadFile(url, bearerToken);
				try
				{
					await using var buffer = new BufferedFileStreamProvider(
						await download.GetResult(cancellationToken));

					var ms = await buffer.GetOwnedResult(cancellationToken);
					try
					{
						ms.Seek(0, SeekOrigin.Begin);

						path ??= Path.GetTempFileName();
						try
						{
							Directory.CreateDirectory(Path.GetDirectoryName(path));
							await using var fs = new DefaultIOManager(new FileSystem()).CreateAsyncSequentialWriteStream(path);
							await ms.CopyToAsync(fs, cancellationToken);

							cachedPaths.Add(url.ToString(), Tuple.Create(path, temporal));

							logger.LogTrace("Cached to {path}", path);
						}
						catch
						{
							File.Delete(path);
							throw;
						}

						ms.Seek(0, SeekOrigin.Begin);
						return ms;
					}
					catch
					{
						await ms.DisposeAsync();
						throw;
					}
				}
				catch
				{
					await download.DisposeAsync();
					throw;
				}
			}
		}
	}
}
