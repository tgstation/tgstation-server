using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Moq;

using Tgstation.Server.Api.Models;

namespace Tgstation.Server.Host.IO.Tests
{
	[TestClass]
	public sealed class TestDelayedFileDownloader
	{
		[TestMethod]
		public async Task TestConstruction()
		{
			Assert.ThrowsException<ArgumentNullException>(() => new DelayedFileDownloader(null, null, null));
			var mockFileDownloader = Mock.Of<IFileDownloader>();
			Assert.ThrowsException<ArgumentNullException>(() => new DelayedFileDownloader(mockFileDownloader, null, null));
			var testUrl = new Uri("http://asdf.com");
			await new DelayedFileDownloader(mockFileDownloader, testUrl, null).DisposeAsync();
			await new DelayedFileDownloader(mockFileDownloader, testUrl, "token").DisposeAsync();
		}

		[TestMethod]
		public async Task TestBasicDownload()
		{
			var resultMs = new MemoryStream();
			var mockFileDownloader = new Mock<IFileDownloader>();
			mockFileDownloader
				.Setup(x => x.DownloadFile(It.IsNotNull<Uri>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
				.Returns(Task.FromResult<Stream>(resultMs))
				.Verifiable();

			await using var downloader = new DelayedFileDownloader(mockFileDownloader.Object, new Uri("http://asdf.com"), null);

			var downloadedMs = await downloader.GetResult(default);
			Assert.AreSame(resultMs, downloadedMs);

			mockFileDownloader.VerifyAll();
		}

		[TestMethod]
		public async Task TestMultiDownload()
		{
			var resultMs = new MemoryStream();
			var mockFileDownloader = new Mock<IFileDownloader>();
			mockFileDownloader
				.Setup(x => x.DownloadFile(It.IsNotNull<Uri>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
				.Returns(Task.FromResult<Stream>(resultMs))
				.Verifiable();

			await using var downloader = new DelayedFileDownloader(mockFileDownloader.Object, new Uri("http://asdf.com"), null);

			var task1 = downloader.GetResult(default);
			var task2 = downloader.GetResult(default);
			var task3 = downloader.GetResult(default);

			Assert.AreSame(resultMs, await task1);
			Assert.AreSame(resultMs, await task2);
			Assert.AreSame(resultMs, await task3);

			mockFileDownloader.VerifyAll();
			Assert.AreEqual(1, mockFileDownloader.Invocations.Count);
		}

		[TestMethod]
		public async Task TestInterruptedDownload()
		{
			var resultMs = new MemoryStream();
			var tcs = new TaskCompletionSource<Stream>();
			var mockFileDownloader = new Mock<IFileDownloader>();
			mockFileDownloader
				.Setup(x => x.DownloadFile(It.IsNotNull<Uri>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
				.Returns(async (Uri url, string token, CancellationToken cancellationToken) =>
				{
					using (cancellationToken.Register(() => tcs.TrySetCanceled()))
						return await tcs.Task;
				})
				.Verifiable();

			await using var downloader = new DelayedFileDownloader(mockFileDownloader.Object, new Uri("http://asdf.com"), null);

			using var cts1 = new CancellationTokenSource();
			var task1 = downloader.GetResult(cts1.Token);

			using var cts2 = new CancellationTokenSource();
			var task2 = downloader.GetResult(cts2.Token);

			using var cts3 = new CancellationTokenSource();
			var task3 = downloader.GetResult(cts3.Token);

			cts2.Cancel();

			await Assert.ThrowsExceptionAsync<TaskCanceledException>(() => task1);
			await Assert.ThrowsExceptionAsync<TaskCanceledException>(() => task2);
			await Assert.ThrowsExceptionAsync<TaskCanceledException>(() => task3);

			mockFileDownloader.VerifyAll();
		}
	}
}
