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
	/// Tests for <see cref="RepositoryFactory"/>.
	/// </summary>
	[TestClass]
	public sealed class TestRepositoryFactory
	{
		static IRepositoryFactory CreateFactory() => new RepositoryFactory(Mock.Of<ILogger<RepositoryFactory>>());

		static Task<LibGit2Sharp.IRepository> TestRepoLoading(
			string path,
			IRepositoryFactory repositoryFactory = null) =>
			(repositoryFactory ?? CreateFactory())
			.CreateFromPath(path, default);

		[TestMethod]
		public void TestConstructionThrows() => Assert.ThrowsException<ArgumentNullException>(() => new RepositoryFactory(null));

		[TestMethod]
		public void TestInMemoryRepoCreation()
		{
			new RepositoryFactory(Mock.Of<ILogger<RepositoryFactory>>()).CreateInMemory().Dispose();
		}

		[TestMethod]
		public async Task TestCloning()
		{
			var tempDir = Path.GetTempFileName();
			File.Delete(tempDir);
			try
			{
				var factory = CreateFactory();
				await factory.Clone(
					new Uri("https://github.com/Cyberboss/Test"),
					new CloneOptions
					{
						CredentialsProvider = factory.GenerateCredentialsHandler(null, null)
					},
					tempDir,
					default);

				using (var repo = await TestRepoLoading(tempDir))
				{
					var gitObject = repo.Lookup("f636418bf47d238d33b0e4a34f0072b23a8aad0e");
					Assert.IsNotNull(gitObject);
					var commit = gitObject.Peel<Commit>();

					Assert.AreEqual("Update Test.md", commit.Message);
				}
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
						await Task.Delay(TimeSpan.FromSeconds(3)).ConfigureAwait(false);
					}
				}
			}
		}
	}
}
