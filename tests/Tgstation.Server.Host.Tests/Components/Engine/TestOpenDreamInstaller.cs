using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Moq;

using Tgstation.Server.Api.Models;
using Tgstation.Server.Api.Models.Internal;
using Tgstation.Server.Host.Components.Repository;
using Tgstation.Server.Host.Configuration;
using Tgstation.Server.Host.IO;
using Tgstation.Server.Host.System;

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
			Assert.IsNotNull(generalConfig.OpenDreamGitUrl);
			mockGeneralConfigOptions.SetupGet(x => x.Value).Returns(generalConfig);

			var cloneAttempts = 0;
			var mockRepository = new Mock<IRepository>();
			var mockRepositoryManager = new Mock<IRepositoryManager>();
			mockRepositoryManager.Setup(x => x.CloneRepository(
				generalConfig.OpenDreamGitUrl,
				null,
				null,
				null,
				null,
				true,
				It.IsAny<CancellationToken>()))
				.Callback(() => ++cloneAttempts)
				.Returns(ValueTask.FromResult(needsClone ? mockRepository.Object : null))
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
				mockGeneralConfigOptions.Object);

			var data = await installer.DownloadVersion(
				new EngineVersion
				{
					Engine = EngineType.OpenDream,
					SourceSHA = new string('a', Limits.MaximumCommitShaLength),
				},
				null,
				CancellationToken.None);


			Assert.IsNotNull(data);

			mockRepositoryManager.VerifyAll();
		}
	}
}
