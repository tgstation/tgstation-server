using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Moq;

using Tgstation.Server.Api.Models;
using Tgstation.Server.Api.Models.Internal;
using Tgstation.Server.Common.Http;
using Tgstation.Server.Host.Components.Repository;
using Tgstation.Server.Host.Configuration;
using Tgstation.Server.Host.IO;
using Tgstation.Server.Host.Jobs;
using Tgstation.Server.Host.System;
using Tgstation.Server.Host.Utils;

namespace Tgstation.Server.Host.Components.Engine.Tests
{
	[TestClass]
	public sealed class TestOpenDreamInstaller
	{
		[TestMethod]
		public async Task TestDownloadsUseSameRepositoryIfItExists()
		{
			await RepoDownloadTest(false);
		}

		[TestMethod]
		public async Task TestDownloadsCloneRepositoryIfItDoesntExists()
		{
			await RepoDownloadTest(true);
		}

		static async Task RepoDownloadTest(bool needsClone)
		{
			var mockGeneralConfigOptions = new Mock<IOptions<GeneralConfiguration>>();
			var generalConfig = new GeneralConfiguration();
			var mockSessionConfigOptions = new Mock<IOptions<SessionConfiguration>>();
			var sessionConfig = new SessionConfiguration();
			Assert.IsNotNull(generalConfig.OpenDreamGitUrl);
			mockGeneralConfigOptions.SetupGet(x => x.Value).Returns(generalConfig);
			mockSessionConfigOptions.SetupGet(x => x.Value).Returns(sessionConfig);

			var cloneAttempts = 0;
			var mockRepository = new Mock<IRepository>();
			mockRepository.Setup(x => x.CommittishIsParent(It.IsNotNull<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
			var mockRepositoryManager = new Mock<IRepositoryManager>();
			mockRepositoryManager.Setup(x => x.CloneRepository(
				generalConfig.OpenDreamGitUrl,
				null,
				null,
				null,
				It.IsNotNull<JobProgressReporter>(),
				true,
				It.IsAny<CancellationToken>()))
				.Callback(() => ++cloneAttempts)
				.ReturnsAsync(needsClone ? mockRepository.Object : null)
				.Verifiable(Times.Exactly(1));

			mockRepositoryManager.Setup(x => x.LoadRepository(
				It.IsAny<CancellationToken>()))
				.Returns(() =>
				{
					Assert.AreEqual(1, cloneAttempts);
					return ValueTask.FromResult(mockRepository.Object);
				})
				.Verifiable(Times.Exactly(needsClone ? 0 : 1));

			var installer = new OpenDreamInstaller(
				Mock.Of<IIOManager>(),
				Mock.Of<ILogger<OpenDreamInstaller>>(),
				Mock.Of<IPlatformIdentifier>(),
				Mock.Of<IProcessExecutor>(),
				mockRepositoryManager.Object,
				Mock.Of<IAsyncDelayer>(),
				Mock.Of<IAbstractHttpClientFactory>(),
				mockGeneralConfigOptions.Object,
				mockSessionConfigOptions.Object);

			var data = await installer.DownloadVersion(
				new EngineVersion
				{
					Engine = EngineType.OpenDream,
					SourceSHA = new string('a', Limits.MaximumCommitShaLength),
				},
				new JobProgressReporter(),
				CancellationToken.None);


			Assert.IsNotNull(data);

			mockRepositoryManager.VerifyAll();
		}
	}
}
