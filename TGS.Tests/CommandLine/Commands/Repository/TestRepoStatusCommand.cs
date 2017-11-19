using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Collections.Generic;
using TGS.Interface;
using TGS.Interface.Components;

namespace TGS.CommandLine.Commands.Repository.Tests
{
	/// <summary>
	/// Tests for <see cref="RepoStatusCommand"/>
	/// </summary>
	[TestClass]
	public class TestRepoStatusCommand : RepositoryCommandTest
	{
		/// <summary>
		/// Set the default returns for the mock <see cref="ITGRepository"/> to be <see langword="null"/>
		/// </summary>
		/// <returns>A <see cref="Mock{T}"/> of <see cref="ITGRepository"/> with default <see langword="null"/> return values</returns>
		Mock<ITGRepository> GetDefaultMock()
		{
			var mock = new Mock<ITGRepository>();
			mock.SetReturnsDefault<string>(null);
			return mock;
		}
		
		[TestMethod]
		public void TestPushTestmergeCommits()
		{
			var ran = false;
			var mock = GetDefaultMock();
			mock.Setup(foo => foo.PushTestmergeCommits()).Returns(false).Callback(() => ran = true);
			ConsoleCommand.Interface = MockInterfaceToRepo(mock.Object);
			Assert.AreEqual(new RepoStatusCommand().DoRun(new List<string> { }), Command.ExitCode.Normal);
			Assert.IsTrue(ran);

			ran = false;
			mock.Setup(foo => foo.PushTestmergeCommits()).Returns(true).Callback(() => ran = true);
			ConsoleCommand.Interface = MockInterfaceToRepo(mock.Object);
			Assert.AreEqual(new RepoStatusCommand().DoRun(new List<string> { }), Command.ExitCode.Normal);
			Assert.IsTrue(ran);
		}
	}
}
