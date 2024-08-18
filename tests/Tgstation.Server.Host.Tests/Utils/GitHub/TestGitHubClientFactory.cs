using System;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Moq;

using Octokit;

using Tgstation.Server.Host.Configuration;
using Tgstation.Server.Host.System;

namespace Tgstation.Server.Host.Utils.GitHub.Tests
{
	[TestClass]
	public sealed class TestGitHubClientFactory
	{
		static ILoggerFactory loggerFactory;

		[ClassInitialize]
		public static void Initialize(TestContext _)
		{
			loggerFactory = LoggerFactory.Create(builder =>
			{
				builder.AddConsole();
				builder.SetMinimumLevel(LogLevel.Trace);
			});
		}

		[ClassCleanup]
		public static void Cleanup()
		{
			loggerFactory.Dispose();
		}

		[TestMethod]
		public void TestContructionThrows()
		{
			Assert.ThrowsException<ArgumentNullException>(() => new GitHubClientFactory(null, null, null));
			Assert.ThrowsException<ArgumentNullException>(() => new GitHubClientFactory(Mock.Of<IAssemblyInformationProvider>(), null, null));
			Assert.ThrowsException<ArgumentNullException>(() => new GitHubClientFactory(Mock.Of<IAssemblyInformationProvider>(), Mock.Of<ILogger<GitHubClientFactory>>(), null));
		}

		[TestMethod]
		public async Task TestCreateBasicClient()
		{
			var mockApp = new Mock<IAssemblyInformationProvider>();
			mockApp.SetupGet(x => x.ProductInfoHeaderValue).Returns(new ProductInfoHeaderValue("TGSTests", "1.2.3")).Verifiable();

			var mockOptions = new Mock<IOptions<GeneralConfiguration>>();

			var gc = new GeneralConfiguration();
			Assert.IsNull(gc.GitHubAccessToken);
			mockOptions.SetupGet(x => x.Value).Returns(gc);
			var factory = new GitHubClientFactory(mockApp.Object, loggerFactory.CreateLogger<GitHubClientFactory>(), mockOptions.Object);

			var client = await factory.CreateClient(CancellationToken.None);
			Assert.IsNotNull(client);
			var credentials = await client.Connection.CredentialStore.GetCredentials();

			Assert.AreEqual(AuthenticationType.Anonymous, credentials.AuthenticationType);

			gc.GitHubAccessToken = "asdfasdfasdfasdfasdfasdf";
			client = await factory.CreateClient(CancellationToken.None);
			Assert.IsNotNull(client);
			credentials = await client.Connection.CredentialStore.GetCredentials();

			Assert.AreEqual(AuthenticationType.Oauth, credentials.AuthenticationType);

			mockApp.VerifyAll();
		}

		[TestMethod]
		public async Task TestCreateTokenClient()
		{
			var mockApp = new Mock<IAssemblyInformationProvider>();
			mockApp.SetupGet(x => x.ProductInfoHeaderValue).Returns(new ProductInfoHeaderValue("TGSTests", "1.2.3")).Verifiable();

			var mockOptions = new Mock<IOptions<GeneralConfiguration>>();
			mockOptions.SetupGet(x => x.Value).Returns(new GeneralConfiguration());
			var factory = new GitHubClientFactory(mockApp.Object, loggerFactory.CreateLogger<GitHubClientFactory>(), mockOptions.Object);

			await Assert.ThrowsExceptionAsync<ArgumentNullException>(() => factory.CreateClient(null, CancellationToken.None).AsTask());

			var client = await factory.CreateClient("asdf", CancellationToken.None);
			Assert.IsNotNull(client);

			var credentials = await client.Connection.CredentialStore.GetCredentials();

			Assert.AreEqual(AuthenticationType.Oauth, credentials.AuthenticationType);

			mockApp.VerifyAll();
		}

		[TestMethod]
		public async Task TestClientCaching()
		{
			var mockApp = new Mock<IAssemblyInformationProvider>();
			mockApp.SetupGet(x => x.ProductInfoHeaderValue).Returns(new ProductInfoHeaderValue("TGSTests", "1.2.3")).Verifiable();

			var mockOptions = new Mock<IOptions<GeneralConfiguration>>();
			mockOptions.SetupGet(x => x.Value).Returns(new GeneralConfiguration());
			var factory = new GitHubClientFactory(mockApp.Object, loggerFactory.CreateLogger<GitHubClientFactory>(), mockOptions.Object);

			var client1 = await factory.CreateClient(CancellationToken.None);
			var client2 = await factory.CreateClient("asdf", CancellationToken.None);
			var client3 = await factory.CreateClient(CancellationToken.None);
			var client4 = await factory.CreateClient("asdf", CancellationToken.None);
			Assert.ReferenceEquals(client1, client3);
			Assert.ReferenceEquals(client2, client4);
		}
	}
}
