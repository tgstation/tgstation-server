using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Collections.Generic;
using TGServiceInterface;
using TGServiceInterface.Components;

namespace TGCommandLine.Commands.Repository.Tests
{
	/// <summary>
	/// Tests for <see cref="RepoSetPushTestmergeCommitsCommand"/>
	/// </summary>
	[TestClass]
	public class TestRepoSetPushTestmergeCommitsCommand : RepositoryCommandTest
	{
		/// <summary>
		/// Ensure it can be turned on
		/// </summary>
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

		/// <summary>
		/// Ensure that it can be turned off
		/// </summary>
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

		/// <summary>
		/// Ensure that <see cref="Command.ExitCode.BadCommand"/> is returned for gibberish parameters
		/// </summary>
		[TestMethod]
		public void TestGibberishParam()
		{
			Assert.AreEqual(new RepoSetPushTestmergeCommitsCommand().DoRun(new List<string> { "gibberish" }), Command.ExitCode.BadCommand);
		}
	}
}
