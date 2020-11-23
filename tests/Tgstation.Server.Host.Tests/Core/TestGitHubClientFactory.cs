using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Octokit;
using System;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Tgstation.Server.Host.System;

namespace Tgstation.Server.Host.Core.Tests
{
	[TestClass]
	public sealed class TestGitHubClientFactory
	{
		[TestMethod]
		public void TestContruction() => Assert.ThrowsException<ArgumentNullException>(() => new GitHubClientFactory(null));

		[TestMethod]
		public async Task TestCreateBasicClient()
		{
			var mockApp = new Mock<IAssemblyInformationProvider>();
			mockApp.SetupGet(x => x.ProductInfoHeaderValue).Returns(new ProductInfoHeaderValue("TGSTests", "1.2.3")).Verifiable();

			var factory = new GitHubClientFactory(mockApp.Object);

			var client = factory.CreateClient();
			Assert.IsNotNull(client);
			var credentials = await client.Connection.CredentialStore.GetCredentials().ConfigureAwait(false);

			Assert.AreEqual(AuthenticationType.Anonymous, credentials.AuthenticationType);


			mockApp.VerifyAll();
		}

		[TestMethod]
		public async Task TestCreateTokenClient()
		{
			var mockApp = new Mock<IAssemblyInformationProvider>();
			mockApp.SetupGet(x => x.ProductInfoHeaderValue).Returns(new ProductInfoHeaderValue("TGSTests", "1.2.3")).Verifiable();

			var factory = new GitHubClientFactory(mockApp.Object);

			Assert.ThrowsException<ArgumentNullException>(() => factory.CreateClient(null));

			var client = factory.CreateClient("asdf");
			Assert.IsNotNull(client);

			var credentials = await client.Connection.CredentialStore.GetCredentials().ConfigureAwait(false);

			Assert.AreEqual(AuthenticationType.Oauth, credentials.AuthenticationType);

			mockApp.VerifyAll();
		}
	}
}
