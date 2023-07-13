using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Moq;

using Tgstation.Server.Host.Components.Events;
using Tgstation.Server.Host.Components.Repository;
using Tgstation.Server.Host.Configuration;
using Tgstation.Server.Host.IO;
using Tgstation.Server.Host.Jobs;

namespace Tgstation.Server.Tests
{
	[TestClass]
	public sealed class TestRepository
	{
		[TestMethod]
		public async Task TestRepoParentLookup()
		{
			var tempPath = Path.Combine(Path.GetTempPath(), "TGS-Repository-Integration-Test", Guid.NewGuid().ToString());
			Directory.CreateDirectory(tempPath);
			try
			{
				LibGit2Sharp.Repository.Clone("https://github.com/Cyberboss/test", tempPath);
				var libGit2Repo = new LibGit2Sharp.Repository(tempPath);
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
				await repo.CheckoutObject(StartSha, null, null, true, new JobProgressReporter(Mock.Of<ILogger<JobProgressReporter>>(), null, (stage, progress) => { }), CancellationToken.None);
				var result = await repo.ShaIsParent("2f8588a3ca0f6b027704a2a04381215619de3412", CancellationToken.None);
				Assert.IsTrue(result);
				Assert.AreEqual(StartSha, repo.Head);
				result = await repo.ShaIsParent("f636418bf47d238d33b0e4a34f0072b23a8aad0e", CancellationToken.None);
				Assert.IsFalse(result);
				Assert.AreEqual(StartSha, repo.Head);
			}
			finally
			{
				await new DefaultIOManager().DeleteDirectory(
					Path.GetDirectoryName(tempPath),
					CancellationToken.None);
			}
		}
	}
}
