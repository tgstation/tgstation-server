using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Moq;

using Tgstation.Server.Common.Http;
using Tgstation.Server.Common.Tests;
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
			var mockClient = new HttpClient();
			Assert.ThrowsException<ArgumentNullException>(() => new RequestFileStreamProvider(mockClient, null));
			await using var test = new RequestFileStreamProvider(mockClient, new HttpRequestMessage());
		}

		[TestMethod]
		public async Task TestBasicDownload()
		{
			var sequence = new byte[] { 1, 2, 3 };
			var resultMs = new MemoryStream(sequence);

			var response = new HttpResponseMessage()
			{
				Content = new StreamContent(resultMs),
			};

			var ran = false;
			var request = new HttpRequestMessage(HttpMethod.Get, "https://example.com");
			var mockHttpClient = new HttpClient(
				new MockHttpMessageHandler(
					(_, _) =>
					{
						ran = true;
						return Task.FromResult(response);
					}));

			await using var downloader = new RequestFileStreamProvider(mockHttpClient, request);

			var download = await downloader.GetResult(default);

			await using var bufferProvider = new BufferedFileStreamProvider(download);

			var buffer = await bufferProvider.GetOwnedResult(default);

			var resultSequence = buffer.ToArray();
			Assert.IsTrue(sequence.SequenceEqual(resultSequence));
			Assert.IsTrue(ran);
		}

		[TestMethod]
		public async Task TestMultiDownload()
		{
			var sequence = new byte[] { 1, 2, 3 };
			var resultMs = new MemoryStream(sequence);

			var response = new HttpResponseMessage()
			{
				Content = new StreamContent(resultMs),
			};

			int ran = 0;
			var request = new HttpRequestMessage(HttpMethod.Get, "https://example.com");
			var mockHttpClient = new HttpClient(
				new MockHttpMessageHandler(
					(_, _) =>
					{
						++ran;
						return Task.FromResult(response);
					}));

			await using var downloader = new RequestFileStreamProvider(mockHttpClient, request);

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

			Assert.AreEqual(1, ran);
		}

		[TestMethod]
		public async Task TestInterruptedDownload()
		{
			var resultMs = new MemoryStream();

			var response = new HttpResponseMessage()
			{
				Content = new StreamContent(resultMs),
			};

			var tcs = new TaskCompletionSource<HttpResponseMessage>();

			var ran = false;
			var request = new HttpRequestMessage(HttpMethod.Get, "https://example.com");
			var mockHttpClient = new HttpClient(
				new MockHttpMessageHandler(
					(_, cancellationToken) =>
					{
						ran = true;
						cancellationToken.Register(() => tcs.TrySetCanceled(cancellationToken));
						return tcs.Task;
					}));

			await using var downloader = new RequestFileStreamProvider(mockHttpClient, request);

			using var cts1 = new CancellationTokenSource();
			var task1 = downloader.GetResult(cts1.Token);

			using var cts2 = new CancellationTokenSource();
			var task2 = downloader.GetResult(cts2.Token);

			using var cts3 = new CancellationTokenSource();
			var task3 = downloader.GetResult(cts3.Token);

			cts2.Cancel();

			await Assert.ThrowsExceptionAsync<TaskCanceledException>(task1.AsTask);
			await Assert.ThrowsExceptionAsync<TaskCanceledException>(task2.AsTask);
			await Assert.ThrowsExceptionAsync<TaskCanceledException>(task3.AsTask);

			Assert.IsTrue(ran);
		}
	}
}
