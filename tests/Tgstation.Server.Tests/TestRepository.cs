using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using LibGit2Sharp;

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
				using var repo = new Host.Components.Repository.Repository(
					libGit2Repo,
					new LibGit2Commands(),
					Mock.Of<IIOManager>(),
					Mock.Of<IEventConsumer>(),
					Mock.Of<ICredentialsProvider>(),
					Mock.Of<IPostWriteHandler>(),
					Mock.Of<IGitRemoteFeaturesFactory>(),
					Mock.Of<ILibGit2RepositoryFactory>(),
					Mock.Of<ILogger<Host.Components.Repository.Repository>>(),
					new GeneralConfiguration(),
					() => { });

				const string StartSha = "af4da8beb9f9b374b04a3cc4d65acca662e8cc1a";
				await repo.CheckoutObject(StartSha, null, null, true, false, new JobProgressReporter(Mock.Of<ILogger<JobProgressReporter>>(), null, (stage, progress) => { }), CancellationToken.None);

				Assert.AreEqual(Host.Components.Repository.Repository.NoReference, repo.Reference);

				var result = await repo.CommittishIsParent("2f8588a3ca0f6b027704a2a04381215619de3412", CancellationToken.None);
				Assert.IsTrue(result);
				Assert.AreEqual(StartSha, repo.Head);
				result = await repo.CommittishIsParent("f636418bf47d238d33b0e4a34f0072b23a8aad0e", CancellationToken.None);
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

		[TestMethod]
		public async Task TestFetchingAdditionalCommits()
		{
			var tempPath = Path.Combine(Path.GetTempPath(), "TGS-Repository-Integration-Test", Guid.NewGuid().ToString());
			var repoFac =
				new LibGit2RepositoryFactory(
					Mock.Of<ILogger<LibGit2RepositoryFactory>>());
			var commands = new LibGit2Commands();
			using var manager = new RepositoryManager(
				repoFac,
				commands,
				new DefaultIOManager().CreateResolverForSubdirectory(
					tempPath),
				Mock.Of<IEventConsumer>(),
				new WindowsPostWriteHandler(),
				Mock.Of<IGitRemoteFeaturesFactory>(),
				Mock.Of<ILogger<Host.Components.Repository.Repository>>(),
				Mock.Of<ILogger<RepositoryManager>>(),
				new GeneralConfiguration());
			try
			{
				using (await manager.CloneRepository(
					new Uri("https://github.com/Cyberboss/common_core"),
					null,
					null,
					null,
					new JobProgressReporter(Mock.Of<ILogger<JobProgressReporter>>(), null, (_, _) => { }),
					true,
					default))
				{
				}

				using (var repo = await repoFac.CreateFromPath(tempPath, default))
				{
					repo.Network.Remotes.Update("origin", updater =>
					{
						updater.Url = "https://github.com/tgstation/common_core";
					});

					var targetCommit = repo.Lookup<Commit>("5b0d0a38057a2c8306a852ccbd6cd6f4ae766a33");
					Assert.IsNull(targetCommit);
				}

				using (var repo2 = await manager.LoadRepository(default))
				{
					await repo2.FetchOrigin(
						new JobProgressReporter(Mock.Of<ILogger<JobProgressReporter>>(), null, (_, _) => { }),
						null,
						null,
						false,
						default);
				}

				using var repo3 = await repoFac.CreateFromPath(tempPath, default);
				var remote = repo3.Network.Remotes.First();
				commands.Fetch(repo3, remote.FetchRefSpecs.Select(x => x.Specification), remote, new FetchOptions
				{
					TagFetchMode = TagFetchMode.All,
					Prune = true,
				}, "test");

				var targetCommit2 = repo3.Lookup<Commit>("5b0d0a38057a2c8306a852ccbd6cd6f4ae766a33");
				Assert.IsNotNull(targetCommit2);
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
