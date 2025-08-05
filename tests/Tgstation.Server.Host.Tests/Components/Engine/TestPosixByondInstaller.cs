using System;
using System.IO;
using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Moq;

using Tgstation.Server.Api.Models;
using Tgstation.Server.Host.Configuration;
using Tgstation.Server.Host.IO;

namespace Tgstation.Server.Host.Components.Engine.Tests
{
	[TestClass]
	public sealed class TestPosixByondInstaller
	{
		[TestMethod]
		public void TestConstruction()
		{
			Assert.ThrowsExactly<ArgumentNullException>(() => new PosixByondInstaller(null, null, null, null, null));
			var mockPostWriteHandler = new Mock<IPostWriteHandler>();
			Assert.ThrowsExactly<ArgumentNullException>(() => new PosixByondInstaller(mockPostWriteHandler.Object, null, null, null, null));
			var mockIOManager = new Mock<IIOManager>();
			Assert.ThrowsExactly<ArgumentNullException>(() => new PosixByondInstaller(mockPostWriteHandler.Object, mockIOManager.Object, null, null, null));
			var mockFileDownloader = Mock.Of<IFileDownloader>();
			Assert.ThrowsExactly<ArgumentNullException>(() => new PosixByondInstaller(mockPostWriteHandler.Object, mockIOManager.Object, mockFileDownloader, null, null));
			var mockOptions = Mock.Of<IOptionsMonitor<GeneralConfiguration>>();
			Assert.ThrowsExactly<ArgumentNullException>(() => new PosixByondInstaller(mockPostWriteHandler.Object, mockIOManager.Object, mockFileDownloader, mockOptions, null));

			var mockLogger = new Mock<ILogger<PosixByondInstaller>>();
			_ = new PosixByondInstaller(mockPostWriteHandler.Object, mockIOManager.Object, mockFileDownloader, mockOptions, mockLogger.Object);
		}

		[TestMethod]
		public async Task TestCacheClean()
		{
			var mockPostWriteHandler = new Mock<IPostWriteHandler>();
			var mockIOManager = new Mock<IIOManager>();
			var mockLogger = new Mock<ILogger<PosixByondInstaller>>();
			var mockFileDownloader = Mock.Of<IFileDownloader>();
			var mockOptions = Mock.Of<IOptionsMonitor<GeneralConfiguration>>();
			var installer = new PosixByondInstaller(mockPostWriteHandler.Object, mockIOManager.Object, mockFileDownloader, mockOptions, mockLogger.Object);
			await installer.CleanCache(default);
		}


		[TestMethod]
		public async Task TestDownload()
		{
			var mockIOManager = new Mock<IIOManager>();
			var mockPostWriteHandler = new Mock<IPostWriteHandler>();
			var mockLogger = new Mock<ILogger<PosixByondInstaller>>();
			var mockFileDownloader = new Mock<IFileDownloader>();
			var mockOptions = new Mock<IOptionsMonitor<GeneralConfiguration>>();
			const string TestUrl = "https://chumb.is";
			mockOptions.SetupGet(x => x.CurrentValue).Returns(new GeneralConfiguration
			{
				ByondZipDownloadTemplate = TestUrl,
			});

			var installer = new PosixByondInstaller(mockPostWriteHandler.Object, mockIOManager.Object, mockFileDownloader.Object, mockOptions.Object, mockLogger.Object);

			await Assert.ThrowsExactlyAsync<ArgumentNullException>(() => installer.DownloadVersion(null, null, default).AsTask());

			var ourArray = Array.Empty<byte>();
			mockFileDownloader
				.Setup(
					x => x.DownloadFile(
						It.Is<Uri>(uri => uri == new Uri(TestUrl)),
						null))
				.Returns(
					new BufferedFileStreamProvider(
						new MemoryStream(ourArray)))
				.Verifiable();

			var result = ExtractMemoryStreamFromInstallationData(await installer.DownloadVersion(new EngineVersion
			{
				Engine = EngineType.Byond,
				Version = new Version(511, 1385),
			}, null, default));

			Assert.IsTrue(ourArray.SequenceEqual(result.ToArray()));
			mockIOManager.Verify();
		}
		static MemoryStream ExtractMemoryStreamFromInstallationData(IEngineInstallationData engineInstallationData)
		{
			var zipStreamData = (ZipStreamEngineInstallationData)engineInstallationData;
			return (MemoryStream)zipStreamData.GetType().GetField("zipStream", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(zipStreamData);
		}

		[TestMethod]
		public async Task TestInstallByond()
		{
			var mockIOManager = new Mock<IIOManager>();
			mockIOManager.Setup(x => x.FileExists(It.IsNotNull<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
			mockIOManager.Setup(x => x.CreateResolverForSubdirectory(It.IsNotNull<string>())).Returns(mockIOManager.Object);
			mockIOManager.Setup(x => x.ConcatPath(It.IsNotNull<string[]>())).Returns<string[]>(Path.Combine);
			mockIOManager.Setup(x => x.ResolvePath(It.IsNotNull<string>())).Returns<string>(path => path);
			var mockPostWriteHandler = new Mock<IPostWriteHandler>();
			var mockLogger = new Mock<ILogger<PosixByondInstaller>>();
			var mockFileDownloader = Mock.Of<IFileDownloader>();
			var mockOptions = Mock.Of<IOptionsMonitor<GeneralConfiguration>>();
			var installer = new PosixByondInstaller(mockPostWriteHandler.Object, mockIOManager.Object, mockFileDownloader, mockOptions, mockLogger.Object);

			const string FakePath = "fake";
			await Assert.ThrowsExactlyAsync<ArgumentNullException>(() => installer.Install(null, null, false, default).AsTask());

			var byondVersion = new EngineVersion
			{
				Engine = EngineType.Byond,
				Version = new Version(123, 252345),
			};

			await Assert.ThrowsExactlyAsync<ArgumentNullException>(() => installer.Install(byondVersion, null, false, default).AsTask());

			byondVersion.Version = new Version(511, 1385);
			await installer.Install(byondVersion, FakePath, false, default);

			mockPostWriteHandler.Verify(x => x.HandleWrite(It.IsAny<string>()), Times.Exactly(4));
		}
	}
}
