using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Collections.Generic;
using TGS.Interface;
using TGS.Interface.Components;

namespace TGS.CommandLine.Commands.Repository.Tests
{
	/// <summary>
	/// Tests for <see cref="RepoSetPushTestmergeCommitsCommand"/>
	/// </summary>
	[TestClass]
	public class TestRepoSetPushTestmergeCommitsCommand : RepositoryCommandTest
	{
		[TestMethod]
		public void TestTurnOn()
		{
			var ran = false;
			var repo = new Mock<ITGRepository>();
			repo.Setup(foo => foo.SetPushTestmergeCommits(true)).Callback(() => { ran = true; });
			ConsoleCommand.Interface = MockInterfaceToRepo(repo.Object);
			Assert.AreEqual(new RepoSetPushTestmergeCommitsCommand().DoRun(new List<string> { "on" }), Command.ExitCode.Normal);
			Assert.IsTrue(ran);
		}
		
		[TestMethod]
		public void TestTurnOff()
		{

			var ran = false;
			var repo = new Mock<ITGRepository>();
			repo.Setup(foo => foo.SetPushTestmergeCommits(false)).Callback(() => { ran = true; });
			ConsoleCommand.Interface = MockInterfaceToRepo(repo.Object);
			Assert.AreEqual(new RepoSetPushTestmergeCommitsCommand().DoRun(new List<string> { "off" }), Command.ExitCode.Normal);
			Assert.IsTrue(ran);
		}
		
		[TestMethod]
		public void TestGibberishParam()
		{
			Assert.AreEqual(new RepoSetPushTestmergeCommitsCommand().DoRun(new List<string> { "gibberish" }), Command.ExitCode.BadCommand);
		}
	}
}
