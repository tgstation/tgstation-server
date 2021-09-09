using System;
using System.IO;
using System.Runtime.Versioning;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

using Tgstation.Server.Host.IO;

namespace Tgstation.Server.Host.Components.Byond.Tests
{
	[TestClass]
	[UnsupportedOSPlatform("windows")]
	public sealed class TestPosixByondInstaller
	{
		[TestMethod]
		public void TestConstruction()
		{
			Assert.ThrowsException<ArgumentNullException>(() => new PosixByondInstaller(null, null, null, null));
			var mockIOManager = new Mock<IIOManager>();
			Assert.ThrowsException<ArgumentNullException>(() => new PosixByondInstaller(null, mockIOManager.Object, null, null));
			var mockFileDownloader = new Mock<IFileDownloader>();
			Assert.ThrowsException<ArgumentNullException>(() => new PosixByondInstaller(null, mockIOManager.Object, mockFileDownloader.Object, null));
			var mockPostWriteHandler = new Mock<IPostWriteHandler>();
			Assert.ThrowsException<ArgumentNullException>(() => new PosixByondInstaller(mockPostWriteHandler.Object, mockIOManager.Object, mockFileDownloader.Object, null));

			var mockLogger = new Mock<ILogger<PosixByondInstaller>>();
			new PosixByondInstaller(mockPostWriteHandler.Object, mockIOManager.Object, mockFileDownloader.Object, mockLogger.Object);
		}

		[TestMethod]
		public async Task TestCacheClean()
		{
			var mockPostWriteHandler = new Mock<IPostWriteHandler>();
			var mockIOManager = new Mock<IIOManager>();
			var mockLogger = new Mock<ILogger<PosixByondInstaller>>();
			var installer = new PosixByondInstaller(mockPostWriteHandler.Object, mockIOManager.Object, Mock.Of<IFileDownloader>(), mockLogger.Object);
			await installer.CleanCache(default);
		}


		[TestMethod]
		public async Task TestDownload()
		{
			var mockIOManager = new Mock<IIOManager>();
			var mockFileDownloader = new Mock<IFileDownloader>();
			var mockPostWriteHandler = new Mock<IPostWriteHandler>();
			var mockLogger = new Mock<ILogger<PosixByondInstaller>>();
			var installer = new PosixByondInstaller(mockPostWriteHandler.Object, mockIOManager.Object, mockFileDownloader.Object, mockLogger.Object);

			await Assert.ThrowsExceptionAsync<ArgumentNullException>(() => installer.DownloadVersion(null, default)).ConfigureAwait(false);

			var ourArray = new MemoryStream();
			mockFileDownloader.Setup(x => x.DownloadFile(It.Is<Uri>(uri => uri == new Uri("https://secure.byond.com/download/build/511/511.1385_byond_linux.zip")), default)).Returns(Task.FromResult(ourArray)).Verifiable();

			var result = await installer.DownloadVersion(new Version(511, 1385), default).ConfigureAwait(false);

			Assert.AreSame(ourArray, result);
			mockIOManager.Verify();
		}

		[TestMethod]
		public async Task TestInstallByond()
		{
			var mockIOManager = new Mock<IIOManager>();
			var mockPostWriteHandler = new Mock<IPostWriteHandler>();
			var mockLogger = new Mock<ILogger<PosixByondInstaller>>();
			var installer = new PosixByondInstaller(mockPostWriteHandler.Object, mockIOManager.Object, Mock.Of<IFileDownloader>(), mockLogger.Object);

			const string FakePath = "fake";
			await Assert.ThrowsExceptionAsync<ArgumentNullException>(() => installer.InstallByond(null, null, default)).ConfigureAwait(false);
			await Assert.ThrowsExceptionAsync<ArgumentNullException>(() => installer.InstallByond(FakePath, null, default)).ConfigureAwait(false);

			await installer.InstallByond(FakePath, new Version(511, 1385), default).ConfigureAwait(false);

			mockPostWriteHandler.Verify(x => x.HandleWrite(It.IsAny<string>()), Times.Exactly(4));
		}
	}
}
