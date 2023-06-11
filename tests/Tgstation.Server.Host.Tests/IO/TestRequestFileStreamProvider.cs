using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Moq;

using Tgstation.Server.Common.Http;
using Tgstation.Server.Host.Extensions;

namespace Tgstation.Server.Host.IO.Tests
{
	[TestClass]
	public sealed class TestRequestFileStreamProvider
	{
		[TestMethod]
		public async Task TestConstruction()
		{
			Assert.ThrowsException<ArgumentNullException>(() => new RequestFileStreamProvider(null, null));
			var mockClient = Mock.Of<IHttpClient>();
			Assert.ThrowsException<ArgumentNullException>(() => new RequestFileStreamProvider(mockClient, null));
			await using var test = new RequestFileStreamProvider(mockClient, new HttpRequestMessage());
		}

		[TestMethod]
		public async Task TestBasicDownload()
		{
			var sequence = new byte[] { 1, 2, 3 };
			var resultMs = new MemoryStream(sequence);
			var mockHttpClient = new Mock<IHttpClient>();

			var response = new HttpResponseMessage()
			{
				Content = new StreamContent(resultMs),
			};


			var request = new HttpRequestMessage();
			mockHttpClient
				.Setup(x => x.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, It.IsAny<CancellationToken>()))
				.Returns(Task.FromResult(response))
				.Verifiable();

			await using var downloader = new RequestFileStreamProvider(mockHttpClient.Object, request);

			var download = await downloader.GetResult(default);

			await using var bufferProvider = new BufferedFileStreamProvider(download);

			var buffer = await bufferProvider.GetOwnedResult(default);

			var resultSequence = buffer.ToArray();
			Assert.IsTrue(sequence.SequenceEqual(resultSequence));

			mockHttpClient.VerifyAll();
		}

		[TestMethod]
		public async Task TestMultiDownload()
		{
			var sequence = new byte[] { 1, 2, 3 };
			var resultMs = new MemoryStream(sequence);
			var mockHttpClient = new Mock<IHttpClient>();

			var response = new HttpResponseMessage()
			{
				Content = new StreamContent(resultMs),
			};

			var request = new HttpRequestMessage();
			mockHttpClient
				.Setup(x => x.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, It.IsAny<CancellationToken>()))
				.Returns(Task.FromResult(response))
				.Verifiable();

			await using var downloader = new RequestFileStreamProvider(mockHttpClient.Object, request);

			var task1 = downloader.GetResult(default);
			var task2 = downloader.GetResult(default);
			var task3 = downloader.GetResult(default);

			var task1Result = await task1;

			await using var ms = new MemoryStream();
			await task1Result.CopyToAsync(ms);
			ms.Seek(0, SeekOrigin.Begin);

			Assert.IsTrue(resultMs.ToArray().SequenceEqual(ms.ToArray()));
			Assert.AreSame(task1Result, await task2);
			Assert.AreSame(task1Result, await task3);

			mockHttpClient.VerifyAll();
			Assert.AreEqual(1, mockHttpClient.Invocations.Count);
		}

		[TestMethod]
		public async Task TestInterruptedDownload()
		{
			var resultMs = new MemoryStream();
			var mockHttpClient = new Mock<IHttpClient>();

			var response = new HttpResponseMessage()
			{
				Content = new StreamContent(resultMs),
			};

			var tcs = new TaskCompletionSource<HttpResponseMessage>();

			var request = new HttpRequestMessage();
			mockHttpClient
				.Setup(x => x.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, It.IsAny<CancellationToken>()))
				.Returns<HttpRequestMessage, HttpCompletionOption, CancellationToken>((request, option, cancellationToken) =>
				{
					cancellationToken.Register(() => tcs.TrySetCanceled());
					return tcs.Task;
				})
				.Verifiable();

			await using var downloader = new RequestFileStreamProvider(mockHttpClient.Object, request);

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

			mockHttpClient.VerifyAll();
		}
	}
}
