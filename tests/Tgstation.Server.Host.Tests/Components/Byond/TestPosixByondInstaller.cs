using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
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
			var mockIOManager = new Mock<IIOManager>();
			Assert.ThrowsException<ArgumentNullException>(() => new PosixByondInstaller(mockIOManager.Object, null, null));
			var mockPostWriteHandler = new Mock<IPostWriteHandler>();
			Assert.ThrowsException<ArgumentNullException>(() => new PosixByondInstaller(mockIOManager.Object, mockPostWriteHandler.Object, null));

			var mockLogger = new Mock<ILogger<PosixByondInstaller>>();
			new PosixByondInstaller(mockIOManager.Object, mockPostWriteHandler.Object, mockLogger.Object);
		}

		[TestMethod]
		public async Task TestCacheClean()
		{
			var mockIOManager = new Mock<IIOManager>();
			var mockPostWriteHandler = new Mock<IPostWriteHandler>();
			var mockLogger = new Mock<ILogger<PosixByondInstaller>>();
			var installer = new PosixByondInstaller(mockIOManager.Object, mockPostWriteHandler.Object, mockLogger.Object);

			const string ByondCachePath = "~/.byond/cache";

			mockIOManager.Setup(x => x.DeleteDirectory(ByondCachePath, default)).Returns(Task.CompletedTask).Verifiable();

			await installer.CleanCache(default);

			mockIOManager.Verify();


			mockIOManager.Setup(x => x.DeleteDirectory(ByondCachePath, default)).Throws(new OperationCanceledException()).Verifiable();

			await Assert.ThrowsExceptionAsync<OperationCanceledException>(() => installer.CleanCache(default)).ConfigureAwait(false);

			mockIOManager.Verify();

			mockIOManager.Setup(x => x.DeleteDirectory(ByondCachePath, default)).Throws(new Exception()).Verifiable();

			await installer.CleanCache(default).ConfigureAwait(false);

			mockIOManager.Verify();
		}


		[TestMethod]
		public async Task TestDownload()
		{
			var mockIOManager = new Mock<IIOManager>();
			var mockPostWriteHandler = new Mock<IPostWriteHandler>();
			var mockLogger = new Mock<ILogger<PosixByondInstaller>>();
			var installer = new PosixByondInstaller(mockIOManager.Object, mockPostWriteHandler.Object, mockLogger.Object);
			
			await Assert.ThrowsExceptionAsync<ArgumentNullException>(() => installer.DownloadVersion(null, default)).ConfigureAwait(false);

			var ourArray = Array.Empty<byte>();
			mockIOManager.Setup(x => x.DownloadFile(It.Is<Uri>(uri => uri == new Uri("https://secure.byond.com/download/build/511/511.1385_byond_linux.zip")), default)).Returns(Task.FromResult(ourArray)).Verifiable();

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
			var installer = new PosixByondInstaller(mockIOManager.Object, mockPostWriteHandler.Object, mockLogger.Object);
			
			const string FakePath = "fake";
			await Assert.ThrowsExceptionAsync<ArgumentNullException>(() => installer.InstallByond(null, null, default)).ConfigureAwait(false);
			await Assert.ThrowsExceptionAsync<ArgumentNullException>(() => installer.InstallByond(FakePath, null, default)).ConfigureAwait(false);

			await installer.InstallByond(FakePath, new Version(511, 1385), default).ConfigureAwait(false);

			mockIOManager.Verify(x => x.ConcatPath(It.IsAny<string>(), It.IsNotNull<string>()), Times.Exactly(5));
			mockIOManager.Verify(x => x.WriteAllBytes(It.IsAny<string>(), It.IsNotNull<byte[]>(), default), Times.Exactly(2));
			mockPostWriteHandler.Verify(x => x.HandleWrite(It.IsAny<string>()), Times.Exactly(4));
		}
	}
}
