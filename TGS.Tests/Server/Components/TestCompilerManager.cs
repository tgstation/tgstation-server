using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using TGS.Interface;
using TGS.Server.Chat.Commands;
using TGS.Server.Configuration;
using TGS.Server.IO;
using TGS.Server.Logging;
using TGS.Tests;

namespace TGS.Server.Components.Tests
{
	/// <summary>
	/// Tests for <see cref="CompilerManager"/>
	/// </summary>
	[TestClass]
	public sealed class TestCompilerManager
	{
		[TestMethod]
		public void TestBasics()
		{
			var mockLogger = new Mock<IInstanceLogger>();
			var mockRepoConfigProvider = new Mock<IRepoConfigProvider>();
			var mockIO = new Mock<IIOManager>();
			var mockConfig = new Mock<IInstanceConfig>();
			var mockChat = new Mock<IChatManager>();
			var mockRepo = new Mock<IRepositoryManager>();
			var mockInterop = new Mock<IInteropManager>();
			var mockDD = new Mock<IDreamDaemonManager>();
			var mockStatic = new Mock<IStaticManager>();
			var mockByond = new Mock<IByondManager>();
			var mockEvents = new Mock<IActionEventManager>();
			using (var c = new CompilerManager(mockLogger.Object, mockRepoConfigProvider.Object, mockIO.Object, mockConfig.Object, mockChat.Object, mockRepo.Object, mockInterop.Object, mockDD.Object, mockStatic.Object, mockByond.Object, mockEvents.Object))
			{
				Assert.IsNotNull(c.Cancel());
				Assert.AreEqual(CompilerStatus.Uninitialized, c.GetStatus());
				mockConfig.SetupGet(x => x.ProjectName).Returns("asdf");
				Assert.AreEqual("asdf", c.ProjectName());
				c.SetProjectName("fdsa");
				mockConfig.VerifySet(x => x.ProjectName = "fdsa", Times.Once());
				Assert.IsNull(c.CompileError());
			}
			mockIO.Setup(x => x.FileExists(It.IsAny<string>())).Returns(Task.FromResult(true));
			using (var c = new CompilerManager(mockLogger.Object, mockRepoConfigProvider.Object, mockIO.Object, mockConfig.Object, mockChat.Object, mockRepo.Object, mockInterop.Object, mockDD.Object, mockStatic.Object, mockByond.Object, mockEvents.Object))
				Assert.AreEqual(CompilerStatus.Initialized, c.GetStatus());
		}
	}
}
