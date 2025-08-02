using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Moq;

using Tgstation.Server.Common.Tests;
using Tgstation.Server.Host.System;

namespace Tgstation.Server.Host.IO.Tests
{
	[TestClass]
	public sealed class TestFileDownloader
	{
		const string ExpectedData = @"Going forward, the .NET team is using https://github.com/dotnet/runtime to
develop the code and issues formerly in this repository.

Please see the following for more context:

[dotnet/announcements#119 ""Consolidating .NET GitHub repos""](https://github.com/dotnet/announcements/issues/119)";

		[TestMethod]
		public void TestConstructor()
		{
			Assert.ThrowsExactly<ArgumentNullException>(() => new FileDownloader(null, null));
			Assert.ThrowsExactly<ArgumentNullException>(() => new FileDownloader(Mock.Of<IHttpClientFactory>(), null));
			_ = new FileDownloader(Mock.Of<IHttpClientFactory>(), Mock.Of<ILogger<FileDownloader>>());
		}

		[TestMethod]
		public async Task TestDownloadWithoutToken()
		{
			await RunTest(null);
		}

		[TestMethod]
		public async Task TestDownloadWithToken()
		{
			var gitHubToken = Environment.GetEnvironmentVariable("TGS_TEST_GITHUB_TOKEN");
			if (String.IsNullOrWhiteSpace(gitHubToken))
				Assert.Inconclusive("TGS_TEST_GITHUB_TOKEN not set");

			await RunTest(gitHubToken);
		}

		[TestMethod]
		public void TestDownloadThrows()
		{
			var downloader = CreateDownloader(out var loggerFactory);
			using (loggerFactory)
			{
				Assert.ThrowsExactly<ArgumentNullException>(() => downloader.DownloadFile(null, null));
			}
		}

		static FileDownloader CreateDownloader(out ILoggerFactory loggerFactory)
		{
			loggerFactory = LoggerFactory.Create(builder =>
			{
				builder.AddConsole();
				builder.SetMinimumLevel(LogLevel.Trace);
			});

			var mockHttpClientFactory = new Mock<IHttpClientFactory>();
			var httpClient = new HttpClient(
				new MockHttpMessageHandler(
					(_, __) => Task.FromResult(
						new HttpResponseMessage(HttpStatusCode.OK)
						{
							Content = new StringContent(ExpectedData),
						})));

			mockHttpClientFactory.Setup(x => x.CreateClient(String.Empty)).Returns(httpClient);

			try
			{
				return new FileDownloader(
					mockHttpClientFactory.Object,
					loggerFactory.CreateLogger<FileDownloader>());
			}
			catch
			{
				loggerFactory.Dispose();
				throw;
			}
		}

		static async Task RunTest(string gitHubToken)
		{
			var downloader = CreateDownloader(out var loggerFactory);
			using (loggerFactory)
			{
				await using var ms = new MemoryStream();
				await using (var provider = downloader.DownloadFile(
					new Uri("https://raw.githubusercontent.com/dotnet/corefx/archive/README.md"),
					gitHubToken))
				await using (var s = await provider.GetResult(default))
					await s.CopyToAsync(ms);

				var stringData = Encoding.UTF8.GetString(ms.GetBuffer());
				Assert.AreEqual(ExpectedData.Replace("\r", String.Empty), stringData.Replace("\r", String.Empty));
			}
		}
	}
}
