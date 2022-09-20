using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Octokit;
using System;
using System.Net.Http.Headers;
using System.Threading.Tasks;

using Tgstation.Server.Host.Configuration;
using Tgstation.Server.Host.System;

namespace Tgstation.Server.Host.Core.Tests
{
	[TestClass]
	public sealed class TestGitHubClientFactory
	{
		[TestMethod]
		public void TestContruction()
		{
			Assert.ThrowsException<ArgumentNullException>(() => new GitHubClientFactory(Mock.Of<IAssemblyInformationProvider>(), null));
			Assert.ThrowsException<ArgumentNullException>(() => new GitHubClientFactory(null, null));
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
			var factory = new GitHubClientFactory(mockApp.Object, mockOptions.Object);

			var client = factory.CreateClient();
			Assert.IsNotNull(client);
			var credentials = await client.Connection.CredentialStore.GetCredentials();

			Assert.AreEqual(AuthenticationType.Anonymous, credentials.AuthenticationType);

			gc.GitHubAccessToken = "asdfasdfasdfasdfasdfasdf";
			client = factory.CreateClient();
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
			var factory = new GitHubClientFactory(mockApp.Object, mockOptions.Object);

			Assert.ThrowsException<ArgumentNullException>(() => factory.CreateClient(null));

			var client = factory.CreateClient("asdf");
			Assert.IsNotNull(client);

			var credentials = await client.Connection.CredentialStore.GetCredentials();

			Assert.AreEqual(AuthenticationType.Oauth, credentials.AuthenticationType);

			mockApp.VerifyAll();
		}
	}
}
