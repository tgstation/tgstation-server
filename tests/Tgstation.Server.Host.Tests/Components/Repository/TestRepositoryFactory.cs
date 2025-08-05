using LibGit2Sharp;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Tgstation.Server.Host.Components.Repository.Tests
{
	/// <summary>
	/// Tests for <see cref="LibGit2RepositoryFactory"/>.
	/// </summary>
	[TestClass]
	public sealed class TestRepositoryFactory
	{
		static LibGit2RepositoryFactory CreateFactory() => new (Mock.Of<ILogger<LibGit2RepositoryFactory>>());

		static async Task<LibGit2Sharp.IRepository> TestRepoLoading(
			string path,
			ILibGit2RepositoryFactory repositoryFactory = null) =>
			(await (repositoryFactory ?? CreateFactory())
			.CreateFromPath(path, default));

		[TestMethod]
		public void TestConstructionThrows() => Assert.ThrowsExactly<ArgumentNullException>(() => new LibGit2RepositoryFactory(null));

		[TestMethod]
		public void TestInMemoryRepoCreation()
		{
			new LibGit2RepositoryFactory(Mock.Of<ILogger<LibGit2RepositoryFactory>>()).CreateInMemory();
		}

		[TestMethod]
		public async Task TestCloning()
		{
			var tempDir = Path.GetTempFileName();
			File.Delete(tempDir);
			try
			{
				var factory = CreateFactory();
				var cloneOpts = new CloneOptions();
				cloneOpts.FetchOptions.CredentialsProvider = factory.GenerateCredentialsHandler(null, null);
				await factory.Clone(
					new Uri("https://github.com/Cyberboss/Test"),
					cloneOpts,
					tempDir,
					default);

				using var repo = await TestRepoLoading(tempDir);
				var gitObject = repo.Lookup("f636418bf47d238d33b0e4a34f0072b23a8aad0e");
				Assert.IsNotNull(gitObject);
				var commit = gitObject.Peel<Commit>();

				Assert.AreEqual("Update Test.md", commit.Message);
			}
			finally
			{
				// Takes a while to release the repo handle sometimes...
				for (var i = 0; i < 5; ++i)
				{
					try
					{
						Directory.Delete(tempDir, true);
						break;
					}
					catch (UnauthorizedAccessException)
					{
						await Task.Delay(TimeSpan.FromSeconds(3));
					}
				}
			}
		}
	}
}
