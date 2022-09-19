using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.IO;
using System.Threading.Tasks;

using Tgstation.Server.Host.IO;

namespace Tgstation.Server.Host.Components.Byond.Tests
{
	[TestClass]
	public sealed class TestPosixByondInstaller
	{
		[TestMethod]
		public void TestConstruction()
		{
			Assert.ThrowsException<ArgumentNullException>(() => new PosixByondInstaller(null, null, null));
			var mockPostWriteHandler = new Mock<IPostWriteHandler>();
			Assert.ThrowsException<ArgumentNullException>(() => new PosixByondInstaller(mockPostWriteHandler.Object, null, null));
			var mockIOManager = new Mock<IIOManager>();
			Assert.ThrowsException<ArgumentNullException>(() => new PosixByondInstaller(mockPostWriteHandler.Object, mockIOManager.Object, null));

			var mockLogger = new Mock<ILogger<PosixByondInstaller>>();
			_ = new PosixByondInstaller(mockPostWriteHandler.Object, mockIOManager.Object, mockLogger.Object);
		}

		[TestMethod]
		public async Task TestCacheClean()
		{
			var mockPostWriteHandler = new Mock<IPostWriteHandler>();
			var mockIOManager = new Mock<IIOManager>();
			var mockLogger = new Mock<ILogger<PosixByondInstaller>>();
			var installer = new PosixByondInstaller(mockPostWriteHandler.Object, mockIOManager.Object, mockLogger.Object);
			await installer.CleanCache(default);
		}


		[TestMethod]
		public async Task TestDownload()
		{
			var mockIOManager = new Mock<IIOManager>();
			var mockPostWriteHandler = new Mock<IPostWriteHandler>();
			var mockLogger = new Mock<ILogger<PosixByondInstaller>>();
			var installer = new PosixByondInstaller(mockPostWriteHandler.Object, mockIOManager.Object, mockLogger.Object);

			await Assert.ThrowsExceptionAsync<ArgumentNullException>(() => installer.DownloadVersion(null, default));

			var ourArray = Array.Empty<byte>();
			mockIOManager.Setup(x => x.DownloadFile(It.Is<Uri>(uri => uri == new Uri("https://secure.byond.com/download/build/511/511.1385_byond_linux.zip")), default)).Returns(Task.FromResult(new MemoryStream(ourArray))).Verifiable();

			var result = await installer.DownloadVersion(new Version(511, 1385), default);

			Assert.AreSame(ourArray, result.ToArray());
			mockIOManager.Verify();
		}

		[TestMethod]
		public async Task TestInstallByond()
		{
			var mockIOManager = new Mock<IIOManager>();
			var mockPostWriteHandler = new Mock<IPostWriteHandler>();
			var mockLogger = new Mock<ILogger<PosixByondInstaller>>();
			var installer = new PosixByondInstaller(mockPostWriteHandler.Object, mockIOManager.Object, mockLogger.Object);

			const string FakePath = "fake";
			await Assert.ThrowsExceptionAsync<ArgumentNullException>(() => installer.InstallByond(null, null, default));
			await Assert.ThrowsExceptionAsync<ArgumentNullException>(() => installer.InstallByond(FakePath, null, default));

			await installer.InstallByond(FakePath, new Version(511, 1385), default);

			mockPostWriteHandler.Verify(x => x.HandleWrite(It.IsAny<string>()), Times.Exactly(4));
		}
	}
}
