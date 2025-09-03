using System;
using System.Threading;
using System.Threading.Tasks;

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
			Assert.ThrowsExactly<ArgumentNullException>(() => new GitHubServiceFactory(null, null, null));
			Assert.ThrowsExactly<ArgumentNullException>(() => new GitHubServiceFactory(Mock.Of<IGitHubClientFactory>(), null, null));
			Assert.ThrowsExactly<ArgumentNullException>(() => new GitHubServiceFactory(Mock.Of<IGitHubClientFactory>(), Mock.Of<ILoggerFactory>(), null));
			var mockOptions = new Mock<IOptionsMonitor<UpdatesConfiguration>>();
			mockOptions.SetupGet(x => x.CurrentValue).Returns(new UpdatesConfiguration());

			_ = new GitHubServiceFactory(Mock.Of<IGitHubClientFactory>(), Mock.Of<ILoggerFactory>(), mockOptions.Object);
		}

		[TestMethod]
		public async Task TestCreateService()
		{
			var mockFactory = new Mock<IGitHubClientFactory>();

#pragma warning disable CA2012 // Use ValueTasks correctly
			mockFactory.Setup(x => x.CreateClient(It.IsAny<CancellationToken>())).Returns(ValueTask.FromResult(Mock.Of<IGitHubClient>())).Verifiable();

			var mockToken = "asdf";
			mockFactory.Setup(x => x.CreateClient(mockToken, It.IsAny<CancellationToken>())).Returns(ValueTask.FromResult(Mock.Of<IGitHubClient>())).Verifiable();
#pragma warning restore CA2012 // Use ValueTasks correctly

			var mockOptions = new Mock<IOptionsMonitor<UpdatesConfiguration>>();
			mockOptions.SetupGet(x => x.CurrentValue).Returns(new UpdatesConfiguration());

			var factory = new GitHubServiceFactory(mockFactory.Object, Mock.Of<ILoggerFactory>(), mockOptions.Object);

			await Assert.ThrowsExactlyAsync<ArgumentNullException>(() => factory.CreateService(null, CancellationToken.None).AsTask());
			Assert.AreEqual(0, mockFactory.Invocations.Count);

			var result1 = await factory.CreateService(CancellationToken.None);
			Assert.IsNotNull(result1);

			var result2 = factory.CreateService(mockToken, CancellationToken.None);
			Assert.IsNotNull(result2);

			mockFactory.VerifyAll();
		}
	}
}
