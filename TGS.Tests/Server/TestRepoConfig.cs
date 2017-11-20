using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Threading.Tasks;

namespace TGS.Server.Tests
{
	/// <summary>
	/// Tests for <see cref="RepoConfig"/>
	/// </summary>
	[TestClass]
	public class TestRepoConfig
	{
		/// <summary>
		/// Well formed TGS3.json
		/// </summary>
		const string goodJSON = "{\"documentation\": \"/tg/station server 3 configuration file\",\"changelog\": {\"script\": \"tools/ss13_genchangelog.py\",\"arguments\": \"html/changelog.html html/changelogs\",\"pip_dependancies\": [\"PyYaml\",\"beautifulsoup4\"]},\"synchronize_paths\": [\"html/changelog.html\",\"html/changelogs/*\"],\"static_directories\": [\"config\",\"data\"],\"dlls\": [\"libmysql.dll\"]}";

		/// <summary>
		/// Empty JSON string
		/// </summary>
		const string emptyJSON = "{}";
		[TestMethod]
		public void TestLoading()
		{
			var mockIO = new Mock<IIOManager>();
			mockIO.Setup(x => x.FileExists(It.IsAny<string>())).Returns(false);
			mockIO.Setup(x => x.ReadAllText(It.IsAny<string>())).Returns(Task.FromResult(goodJSON));
			new RepoConfig("asdf", mockIO.Object);
			mockIO.Verify(x => x.FileExists(It.IsAny<string>()), Times.Once());
			mockIO.ResetCalls();
			mockIO.Setup(x => x.FileExists(It.IsAny<string>())).Returns(true);
			var good = new RepoConfig("asdf", mockIO.Object);
			mockIO.Verify(x => x.FileExists(It.IsAny<string>()), Times.Once());
			mockIO.Verify(x => x.ReadAllText(It.IsAny<string>()), Times.Once());
			mockIO.Setup(x => x.ReadAllText(It.IsAny<string>())).Returns(Task.FromResult(emptyJSON));
			mockIO.ResetCalls();
			mockIO.Setup(x => x.FileExists(It.IsAny<string>())).Returns(true);
			var bad = new RepoConfig("asdf", mockIO.Object);
			mockIO.Verify(x => x.FileExists(It.IsAny<string>()), Times.Once());
			mockIO.Verify(x => x.ReadAllText(It.IsAny<string>()), Times.Once());
			
			Assert.IsTrue(good.ChangelogSupport);
			Assert.IsFalse(String.IsNullOrWhiteSpace(good.ChangelogPyArguments));
			Assert.AreEqual(1, good.DLLPaths.Count);
			Assert.AreEqual(2, good.PathsToStage.Count);
			Assert.AreEqual(2, good.PipDependancies.Count);
			Assert.AreEqual(2, good.StaticDirectoryPaths.Count);

			Assert.IsFalse(bad.ChangelogSupport);
			Assert.AreEqual(0, bad.DLLPaths.Count);
			Assert.AreEqual(0, bad.PathsToStage.Count);
			Assert.AreEqual(0, bad.StaticDirectoryPaths.Count);
			Assert.AreEqual(0, bad.PipDependancies.Count);
		}

		[TestMethod]
		public void TestEquivalence()
		{
			var mockIO = new Mock<IIOManager>();
			mockIO.Setup(x => x.ReadAllText(It.IsAny<string>())).Returns(Task.FromResult(goodJSON));
			mockIO.Setup(x => x.FileExists(It.IsAny<string>())).Returns(true);
			var e1 = new RepoConfig("asdf", mockIO.Object);
			var e2 = new RepoConfig("asdf", mockIO.Object);
			mockIO.Setup(x => x.ReadAllText(It.IsAny<string>())).Returns(Task.FromResult(emptyJSON));
			var b = new RepoConfig("asdf", mockIO.Object);
			Assert.IsTrue(e1.Equals(e2));
			Assert.IsFalse(e1.Equals(b));
			Assert.IsFalse(b.Equals(e2));
			Assert.IsFalse(e1.Equals(null));
		}
	}
}
