using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Moq;

using Octokit;

using Tgstation.Server.Api.Models;
using Tgstation.Server.Host.Configuration;
using Tgstation.Server.Host.System;
using Tgstation.Server.Host.Tests;

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
			Assert.ThrowsExactly<ArgumentNullException>(() => new GitHubClientFactory(null, null, null, null));
			Assert.ThrowsExactly<ArgumentNullException>(() => new GitHubClientFactory(Mock.Of<IAssemblyInformationProvider>(), null, null, null));
			Assert.ThrowsExactly<ArgumentNullException>(() => new GitHubClientFactory(Mock.Of<IAssemblyInformationProvider>(), Mock.Of<IHttpMessageHandlerFactory>(), null, null));
			Assert.ThrowsExactly<ArgumentNullException>(() => new GitHubClientFactory(Mock.Of<IAssemblyInformationProvider>(), Mock.Of<IHttpMessageHandlerFactory>(), Mock.Of<ILogger<GitHubClientFactory>>(), null));
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
			var factory = new GitHubClientFactory(mockApp.Object, new BasicHttpMessageHandlerFactory(), loggerFactory.CreateLogger<GitHubClientFactory>(), mockOptions.Object);

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
			var factory = new GitHubClientFactory(mockApp.Object, new BasicHttpMessageHandlerFactory(), loggerFactory.CreateLogger<GitHubClientFactory>(), mockOptions.Object);

			await Assert.ThrowsExactlyAsync<ArgumentNullException>(() => factory.CreateClient(null, CancellationToken.None).AsTask());

			var client = await factory.CreateClient("asdf", CancellationToken.None);
			Assert.IsNotNull(client);

			var credentials = await client.Connection.CredentialStore.GetCredentials();

			Assert.AreEqual(AuthenticationType.Oauth, credentials.AuthenticationType);

			mockApp.VerifyAll();
		}

		[TestMethod]
		public async Task TestCreateEncodedClientRealKey()
		{
			var mockApp = new Mock<IAssemblyInformationProvider>();
			mockApp.SetupGet(x => x.ProductInfoHeaderValue).Returns(new ProductInfoHeaderValue("TGSTests", "1.2.3")).Verifiable();

			var mockOptions = new Mock<IOptions<GeneralConfiguration>>();
			mockOptions.SetupGet(x => x.Value).Returns(new GeneralConfiguration());
			var factory = new GitHubClientFactory(mockApp.Object, new BasicHttpMessageHandlerFactory(), loggerFactory.CreateLogger<GitHubClientFactory>(), mockOptions.Object);

			var appID = Environment.GetEnvironmentVariable("TGS_TEST_APP_ID");
			var privateKey = Environment.GetEnvironmentVariable("TGS_TEST_APP_PRIVATE_KEY");
			var repoSlug = Environment.GetEnvironmentVariable("TGS_TEST_REPO_SLUG");

			if (String.IsNullOrWhiteSpace(appID))
				Assert.Inconclusive("Missing App ID");

			if (String.IsNullOrWhiteSpace(privateKey))
				Assert.Inconclusive("Missing App Private Key");

			if (String.IsNullOrWhiteSpace(repoSlug))
				Assert.Inconclusive("Missing repo slug");

			var fakeAccessString = $"{RepositorySettings.TgsAppPrivateKeyPrefix}{appID}:{Convert.ToBase64String(Encoding.UTF8.GetBytes(privateKey))}";
			var slugSplits = repoSlug.Split('/');

			Assert.AreEqual(2, slugSplits.Length);

			var client = await factory.CreateClientForRepository(fakeAccessString, new RepositoryIdentifier(slugSplits[0], slugSplits[1]), CancellationToken.None);
			Assert.IsNotNull(client);

			var credentials = await client.Connection.CredentialStore.GetCredentials();

			Assert.AreEqual(AuthenticationType.Oauth, credentials.AuthenticationType);

			mockApp.VerifyAll();
		}

		[TestMethod]
		public async Task TestCreateEncodedClientFakeKey()
		{
			var mockApp = new Mock<IAssemblyInformationProvider>();
			mockApp.SetupGet(x => x.ProductInfoHeaderValue).Returns(new ProductInfoHeaderValue("TGSTests", "1.2.3")).Verifiable();

			var mockOptions = new Mock<IOptions<GeneralConfiguration>>();
			mockOptions.SetupGet(x => x.Value).Returns(new GeneralConfiguration());
			var factory = new GitHubClientFactory(mockApp.Object, new BasicHttpMessageHandlerFactory(), loggerFactory.CreateLogger<GitHubClientFactory>(), mockOptions.Object);

			await Assert.ThrowsExactlyAsync<ArgumentNullException>(() => factory.CreateClient(null, CancellationToken.None).AsTask());

			const string FakePrivateKey = @"-----BEGIN RSA PRIVATE KEY-----
MIIEowIBAAKCAQEAq3oP6NMRwRZY8eMbm4GRLyfJ07LNpHzjRcjTvMf8LGGSVb8v
DBApR/5+TvWB5qnnh5fJBrR40sboJhIXUXkpyebIu/7lDqXhfjroAk8oNJhyLpW7
u+8DpwTuZYQSSUgdMvNqrWBt7SMrFbhTtEVvQLW5LcwXL9E+V83pwQ1q37wCsedq
fsQbZSxXMMu4efaFoGJ1G60WqYiCCBSsJMPZY5St2G5Tn2caUVQX4V/kyU09gGyN
lJWFpZJP37zBdfmX+VkNj+UFusmcUe7GnklzYhyiXX2sVxzCekCAUxZKO3WKDHbg
CA02oZ8mfQiIbxCYG/uqYmAoAMbQJKqc7iu/0wIDAQABAoIBAEa37F/EzImpQb1g
QD59zPZ5nk7katLvfnuFO22bvHBBPSyH0EtVTvEWD9lYft42K/pLquhM/ZdP2OX6
iAtdwNI3j4mYsba8yqZYfN6W7rojRRTZQ7dZ91OmQPs04KXAS+p7YP9nyW4HFvm6
Lyslh6BUUa6FgPqDfQaRMVogwnbKUilob1eNncXRTw1PUQ+YOK28COYwp+XtNdEM
lecUVqZv5HKSMvP643sMKnj93PXWyisaj77pEdw/1CDQkP1cgqTyX4SaEgx2QDhR
ljaSIQH41JqQCDpWqbqrx3XiqpaWrIAnmUHhP0WRUlD8wCaW5PxY/HwkT/McgyGD
QUofyuECgYEA1XH6850JOrqYZagmTwgSzfbuOmQyYWxdvnq/oRjyy2rpcrISRdMh
3NdpwDX41EhWvMEjfA7l1AYccf72AbwGK+kzosqXn53NHcCjsVd1VzNo9Zm7l9Wj
SJE3ZLSrQ/8nlvrwGr0tgybhq3kLJZLZ+blBSvuYx2YHC4PQEuAo3DcCgYEAzaoO
tPCvpo23jHcevYaQQCYh1sAts9m99LUg2GUYHQ/U11VGq+DTUfReTI7LhXZuyvyx
V+bR5e6NPTuL0XBmESjDSUnj+SCVK8x+NMJMQKqS4OcGSZmx0CCtQCAykZvOA9NQ
zoXW8DkTtiwyQ+AMh9msYnWpJJj2y86/6pOqQ0UCgYB/B+Lu8dr4VO02Myj5iDiI
1BlcLx281Z3FK5C48/wsDGj7lfdCDzHsGVgayQRacuMMW3Ye807dLPXo8nC+/4Q8
xgGxNRmgKW5V8rx5Yy+2wiYJZYE8EC2plqN9D/mN8mFBff9AKq7Xi2BriRKVPhz0
fsjZM3vt0E8JD13angYzaQKBgHHkfBJ9u3grwPrbuL1SOK4dr92iPWz85zIN4GuV
yH3Hl6HMCsACWGRpRJN2/IQjawWkXH2GSLThn3vKbwqECTH1dfgvID2FarZ/n2CO
PPYOwBomNhgqMgtFHUyGyBpUwwjhTD2iZr5PjXf0D74A5E+THuDDsfCfeQSysRsx
vTdVAoGBAI/jjUMdjkY43zhe3w2piwT0fhGfqm9ikdAB9IcgcptuS0ML0ZaWV/eO
8a/G4KMV387BgLUPotWL29EHTdD5ir2yVIhF4JtQEpPucktacruu2TbGUm/5gV96
0+rEPfoEdN+gHI+3w7N6owFmir7Wdz0VK9K8OxsjMLshW3sRlphg
-----END RSA PRIVATE KEY-----";

			var fakeAccessString = $"{RepositorySettings.TgsAppPrivateKeyPrefix}123:{Convert.ToBase64String(Encoding.UTF8.GetBytes(FakePrivateKey))}";

			var client = await factory.CreateClientForRepository(fakeAccessString, new RepositoryIdentifier("fake_owner", "fake_name"), CancellationToken.None);
			Assert.IsNull(client);

			mockApp.VerifyAll();
		}

		[TestMethod]
		public async Task TestClientCaching()
		{
			var mockApp = new Mock<IAssemblyInformationProvider>();
			mockApp.SetupGet(x => x.ProductInfoHeaderValue).Returns(new ProductInfoHeaderValue("TGSTests", "1.2.3")).Verifiable();

			var mockOptions = new Mock<IOptions<GeneralConfiguration>>();
			mockOptions.SetupGet(x => x.Value).Returns(new GeneralConfiguration());
			var factory = new GitHubClientFactory(mockApp.Object, new BasicHttpMessageHandlerFactory(), loggerFactory.CreateLogger<GitHubClientFactory>(), mockOptions.Object);

			var client1 = await factory.CreateClient(CancellationToken.None);
			var client2 = await factory.CreateClient("asdf", CancellationToken.None);
			var client3 = await factory.CreateClient(CancellationToken.None);
			var client4 = await factory.CreateClient("asdf", CancellationToken.None);
			Assert.AreSame(client1, client3);
			Assert.AreSame(client2, client4);
		}
	}
}
