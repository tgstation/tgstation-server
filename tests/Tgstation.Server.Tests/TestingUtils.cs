using System;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

using Tgstation.Server.Host.Components.Engine;
using Tgstation.Server.Host.IO;

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

		public static async ValueTask<MemoryStream> ExtractMemoryStreamFromInstallationData(IEngineInstallationData engineInstallationData, CancellationToken cancellationToken)
		{
			if (engineInstallationData is ZipStreamEngineInstallationData zipStreamData)
				return (MemoryStream)zipStreamData.GetType().GetField("zipStream", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(zipStreamData);

			await using var repoData = (RepositoryEngineInstallationData)engineInstallationData;
			var tempFolder = Path.GetTempFileName();
			File.Delete(tempFolder);
			try
			{
				await repoData.ExtractToPath(tempFolder, cancellationToken);
				var resultStream = new MemoryStream();
				try
				{
					ZipFile.CreateFromDirectory(tempFolder, resultStream, CompressionLevel.NoCompression, false);
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
				await new DefaultIOManager().DeleteDirectory(tempFolder, cancellationToken);
			}
		}
	}
}
