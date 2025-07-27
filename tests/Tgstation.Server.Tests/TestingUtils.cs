using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Moq;

using Tgstation.Server.Host.Components.Engine;
using Tgstation.Server.Host.Configuration;
using Tgstation.Server.Host.IO;
using Tgstation.Server.Host.System;

namespace Tgstation.Server.Tests
{
	static class TestingUtils
	{
		public static bool RunningInGitHubActions { get; } = !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("GITHUB_RUN_ID"));

		public static ILoggerFactory CreateLoggerFactoryForLogger(ILogger logger, out Mock<ILoggerFactory> mockLoggerFactory)
		{
			mockLoggerFactory = new Mock<ILoggerFactory>();
			mockLoggerFactory.Setup(x => x.CreateLogger(It.IsAny<string>())).Returns(() =>
			{
				var temp = logger;
				logger = null;

				Assert.IsNotNull(temp);
				return temp;
			})
			.Verifiable();
			return mockLoggerFactory.Object;
		}

		public static async ValueTask<Stream> ExtractMemoryStreamFromInstallationData(IEngineInstallationData engineInstallationData, CancellationToken cancellationToken)
		{
			if (engineInstallationData is ZipStreamEngineInstallationData zipStreamData)
				return (MemoryStream)zipStreamData.GetType().GetField("zipStream", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(zipStreamData);

			await using var grabby = engineInstallationData;
			var tempFolder = Path.GetTempFileName();
			File.Delete(tempFolder);
			try
			{
				await engineInstallationData.ExtractToPath(tempFolder, cancellationToken);
				var resultStream = new FileStream(
					$"{tempFolder}.zip",
					FileMode.Create,
					FileAccess.ReadWrite,
					FileShare.Read | FileShare.Delete,
					4096,
					FileOptions.Asynchronous);

				File.Delete(resultStream.Name); // now we have a ghost file that will delete when the stream closes
				try
				{
					ZipFile.CreateFromDirectory(tempFolder, resultStream, CompressionLevel.NoCompression, false);
					resultStream.Seek(0, SeekOrigin.Begin);
					return resultStream;
				}
				catch
				{
					await resultStream.DisposeAsync();
					throw;
				}
			}
			finally
			{
				await new DefaultIOManager(new FileSystem()).DeleteDirectory(tempFolder, cancellationToken);
			}
		}

		static string byondZipDownloadTemplate;

		public static string ByondZipDownloadTemplate
		{
			get
			{
				if (byondZipDownloadTemplate == null)
				{
					var envvar = Environment.GetEnvironmentVariable("TGS_TEST_BYOND_ZIP_DOWNLOAD_TEMPLATE");
					if (envvar != null)
						byondZipDownloadTemplate = envvar;
					else
						byondZipDownloadTemplate = GeneralConfiguration.DefaultByondZipDownloadTemplate;
				}

				return byondZipDownloadTemplate;
			}
		}

		static string edgeVersion = null;
		public static async ValueTask<string> GetByondEdgeVersion(ILogger logger, IFileDownloader fileDownloader, CancellationToken cancellationToken)
		{
			if (edgeVersion != null)
				return edgeVersion;

			async ValueTask<string> GetVersionFromResponse(string versionTxt)
			{
				await using var provider = fileDownloader.DownloadFile(new Uri(versionTxt), null);
				var stream = await provider.GetResult(cancellationToken);
				using var reader = new StreamReader(stream, Encoding.UTF8, false, -1, true);
				var text = await reader.ReadToEndAsync(cancellationToken);
				var splits = text.Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

				var targetVersion = splits.Last();

				var badVersionMap = new PlatformIdentifier().IsWindows
					? []
					// linux map also needs updating in CI
					: new Dictionary<string, string>()
					{
						{ "515.1612", "515.1611" }
					};

				badVersionMap.Add("515.1617", "515.1616");

				if (badVersionMap.TryGetValue(targetVersion, out var remappedVersion))
					targetVersion = remappedVersion;

				return targetVersion;
			}

			var mirroredVersionTxt = Environment.GetEnvironmentVariable("TGS_TEST_BYOND_MIRROR_VERSION_TXT");
			try
			{
				// always check byond.com first for latest up-to-date, mirror should ALWAYS have stable versions
				// except byond hates all CI runners now
				const string DefaultMirror = "https://spacestation13.github.io/byond-builds/version.txt";
				edgeVersion = await GetVersionFromResponse(DefaultMirror);

				logger.LogInformation("Downloading edge version from SS13 mirror {edge}", edgeVersion);

				// if we got the result from byond.com, make sure the cache grabs the zip from there as well
				await CachingFileDownloader.InitializeByondVersion(
					logger,
					Version.Parse(edgeVersion),
					new PlatformIdentifier().IsWindows,
					cancellationToken,
					"https://spacestation13.github.io/byond-builds/${Major}/${Major}.${Minor}_byond${Linux:_linux}.zip");
			}
			catch (Exception ex)
			{
				logger.LogWarning(ex, "Cannot download zip from byond.com!");
				if (ByondZipDownloadTemplate == GeneralConfiguration.DefaultByondZipDownloadTemplate || mirroredVersionTxt == null)
					throw;

				// fall back to the mirrored version.txt
				await using var provider = fileDownloader.DownloadFile(new Uri(mirroredVersionTxt), null);
				edgeVersion = await GetVersionFromResponse(mirroredVersionTxt);
			}

			return edgeVersion;
		}
	}
}
