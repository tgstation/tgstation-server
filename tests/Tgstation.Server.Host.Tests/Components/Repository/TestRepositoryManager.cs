using System;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;

using LibGit2Sharp;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Moq;

using Remora.Rest.Core;

using Tgstation.Server.Host.Components.Events;
using Tgstation.Server.Host.Configuration;
using Tgstation.Server.Host.IO;
using Tgstation.Server.Host.Jobs;

namespace Tgstation.Server.Host.Components.Repository.Tests
{
	[TestClass]
	public sealed class TestRepositoryManager
	{
		const string TestRepoPath = "adfiwurjwhouerfiunfjdfn";

		RepositoryManager repositoryManager;
		Mock<ILibGit2RepositoryFactory> mockRepoFactory;
		Mock<ILibGit2Commands> mockCommands;
		Mock<IIOManager> mockIOManager;
		Mock<IGitRemoteFeaturesFactory> mockGitRemoteFeaturesFactory;

		[TestInitialize]
		public void Initialize()
		{
			mockIOManager = new Mock<IIOManager>();
			mockRepoFactory = new Mock<ILibGit2RepositoryFactory>();
			mockCommands = new Mock<ILibGit2Commands>();
			mockGitRemoteFeaturesFactory = new Mock<IGitRemoteFeaturesFactory>();

			mockIOManager.Setup(x => x.ResolvePath()).Returns(TestRepoPath);

			repositoryManager = new RepositoryManager(
				mockRepoFactory.Object,
				mockCommands.Object,
				mockIOManager.Object,
				Mock.Of<IEventConsumer>(),
				Mock.Of<IPostWriteHandler>(),
				mockGitRemoteFeaturesFactory.Object,
				Mock.Of<IOptionsMonitor<GeneralConfiguration>>(),
				Mock.Of<ILogger<Repository>>(),
				Mock.Of<ILogger<RepositoryManager>>());
		}

		[TestCleanup]
		public void TestCleanup() => repositoryManager.Dispose();

		[TestMethod]
		public async Task TestCloneAbortsIfRepoExists()
		{
			mockIOManager.Setup(x => x.DirectoryExists(TestRepoPath, It.IsAny<CancellationToken>())).ReturnsAsync(true).Verifiable();

			using var cloneResult = await repositoryManager.CloneRepository(
				new Uri("https://github.com/Cyberboss/common_core"),
				null,
				null,
				null,
				null,
				false,
				CancellationToken.None);

			Assert.IsNull(cloneResult);

			mockIOManager.VerifyAll();
		}

		[TestMethod]
		public async Task TestBasicClone()
		{
			mockIOManager.Setup(x => x.DirectoryExists(TestRepoPath, It.IsAny<CancellationToken>())).ReturnsAsync(false).Verifiable();
			var mockRepo = new Mock<LibGit2Sharp.IRepository>();

			mockRepoFactory.Setup(x => x.CreateFromPath(TestRepoPath, It.IsAny<CancellationToken>())).ReturnsAsync(mockRepo.Object).Verifiable();

			using var cloneResult = await repositoryManager.CloneRepository(
				new Uri("https://github.com/Cyberboss/common_core"),
				null,
				null,
				null,
				new JobProgressReporter(),
				false,
				CancellationToken.None);

			Assert.IsNotNull(cloneResult);

			mockRepoFactory.VerifyAll();
			mockIOManager.VerifyAll();
		}

		[TestMethod]
		public async Task TestBasicLoad()
		{
			mockIOManager.Setup(x => x.DirectoryExists(TestRepoPath, It.IsAny<CancellationToken>())).ReturnsAsync(false).Verifiable(Times.Never);
			var mockRepo = new Mock<LibGit2Sharp.IRepository>();

			mockRepoFactory.Setup(x => x.CreateFromPath(TestRepoPath, It.IsAny<CancellationToken>())).ReturnsAsync(mockRepo.Object).Verifiable();

			using var loadResult = await repositoryManager.LoadRepository(
				CancellationToken.None);

			Assert.IsNotNull(loadResult);

			mockRepoFactory.VerifyAll();
			mockIOManager.VerifyAll();
		}

		[TestMethod]
		public async Task TestLoadFailsIfRepoDoesntExist()
		{
			mockIOManager.Setup(x => x.DirectoryExists(TestRepoPath, It.IsAny<CancellationToken>())).ReturnsAsync(false).Verifiable(Times.Never);
			var mockRepo = new Mock<LibGit2Sharp.IRepository>();

			mockRepoFactory.Setup(x => x.CreateFromPath(TestRepoPath, It.IsAny<CancellationToken>())).ThrowsAsync(new RepositoryNotFoundException()).Verifiable();

			using var loadResult = await repositoryManager.LoadRepository(
				CancellationToken.None);

			Assert.IsNull(loadResult);

			mockRepoFactory.VerifyAll();
			mockIOManager.VerifyAll();
		}
	}
}
