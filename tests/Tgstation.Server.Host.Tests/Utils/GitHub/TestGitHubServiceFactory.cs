using System;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Moq;

using Octokit;

using Tgstation.Server.Host.Configuration;

namespace Tgstation.Server.Host.Utils.GitHub.Tests
{
	[TestClass]
	public sealed class TestGitHubServiceFactory
	{
		[TestMethod]
		public void TestConstructor()
		{
			Assert.ThrowsException<ArgumentNullException>(() => new GitHubServiceFactory(null, null, null));
			Assert.ThrowsException<ArgumentNullException>(() => new GitHubServiceFactory(Mock.Of<IGitHubClientFactory>(), null, null));
			Assert.ThrowsException<ArgumentNullException>(() => new GitHubServiceFactory(Mock.Of<IGitHubClientFactory>(), Mock.Of<ILoggerFactory>(), null));
			var mockOptions = new Mock<IOptions<UpdatesConfiguration>>();
			mockOptions.SetupGet(x => x.Value).Returns(new UpdatesConfiguration());

			_ = new GitHubServiceFactory(Mock.Of<IGitHubClientFactory>(), Mock.Of<ILoggerFactory>(), mockOptions.Object);
		}

		[TestMethod]
		public void TestCreateService()
		{
			var mockFactory = new Mock<IGitHubClientFactory>();

			mockFactory.Setup(x => x.CreateClient()).Returns(Mock.Of<IGitHubClient>()).Verifiable();

			var mockToken = "asdf";
			mockFactory.Setup(x => x.CreateClient(mockToken)).Returns(Mock.Of<IGitHubClient>()).Verifiable();

			var mockOptions = new Mock<IOptions<UpdatesConfiguration>>();
			mockOptions.SetupGet(x => x.Value).Returns(new UpdatesConfiguration());

			var factory = new GitHubServiceFactory(mockFactory.Object, Mock.Of<ILoggerFactory>(), mockOptions.Object);

			Assert.ThrowsException<ArgumentNullException>(() => factory.CreateService(null));
			Assert.AreEqual(0, mockFactory.Invocations.Count);

			var result1 = factory.CreateService();
			Assert.IsNotNull(result1);

			var result2 = factory.CreateService(mockToken);
			Assert.IsNotNull(result2);

			mockFactory.VerifyAll();
		}
	}
}
