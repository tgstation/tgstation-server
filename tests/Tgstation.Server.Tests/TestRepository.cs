using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Moq;

using Tgstation.Server.Host.Components.Events;
using Tgstation.Server.Host.Components.Repository;
using Tgstation.Server.Host.Configuration;
using Tgstation.Server.Host.IO;
using Tgstation.Server.Host.Jobs;
using Tgstation.Server.Tests.Live;

namespace Tgstation.Server.Tests
{
	[TestClass]
	public sealed class TestRepository
	{
		[TestMethod]
		public async Task TestRepoParentLookup()
		{
			using var testingServer = new LiveTestingServer(null, false);
			LibGit2Sharp.Repository.Clone("https://github.com/Cyberboss/test", testingServer.Directory);
			var libGit2Repo = new LibGit2Sharp.Repository(testingServer.Directory);
			using var repo = new Repository(
				libGit2Repo,
				new LibGit2Commands(),
				Mock.Of<IIOManager>(),
				Mock.Of<IEventConsumer>(),
				Mock.Of<ICredentialsProvider>(),
				Mock.Of<IPostWriteHandler>(),
				Mock.Of<IGitRemoteFeaturesFactory>(),
				Mock.Of<ILogger<Repository>>(),
				new GeneralConfiguration(),
				() => { });

			const string StartSha = "af4da8beb9f9b374b04a3cc4d65acca662e8cc1a";
			await repo.CheckoutObject(StartSha, null, null, true, new JobProgressReporter(Mock.Of<ILogger<JobProgressReporter>>(), null, (stage, progress) => { }), default);
			var result = await repo.ShaIsParent("2f8588a3ca0f6b027704a2a04381215619de3412", default);
			Assert.IsTrue(result);
			Assert.AreEqual(StartSha, repo.Head);
			result = await repo.ShaIsParent("f636418bf47d238d33b0e4a34f0072b23a8aad0e", default);
			Assert.IsFalse(result);
			Assert.AreEqual(StartSha, repo.Head);
		}
	}
}
