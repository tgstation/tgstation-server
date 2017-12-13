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

		[TestMethod]
		public void TestBasicInitialization()
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
			CompilerManager c;
			using (c = new CompilerManager(mockLogger.Object, mockRepoConfigProvider.Object, mockIO.Object, mockConfig.Object, mockChat.Object, mockRepo.Object, mockInterop.Object, mockDD.Object, mockStatic.Object, mockByond.Object, mockEvents.Object))
			{
				var tcs = new TaskCompletionSource<bool>();

				mockByond.Setup(x => x.GetVersion(ByondVersion.Installed)).Callback(() => tcs.SetResult(true));
				mockDD.Setup(x => x.DaemonStatus()).Returns(DreamDaemonStatus.Offline);
				mockIO.Setup(x => x.DirectoryExists(It.IsAny<string>())).Returns(Task.FromResult(true));
				mockIO.Setup(x => x.Unlink(It.IsAny<string>())).Returns(Task.CompletedTask);
				mockIO.Setup(x => x.CreateDirectory(It.IsAny<string>())).Returns(Task.FromResult(new DirectoryInfo(".")));
				mockIO.Setup(x => x.DeleteDirectory(It.IsAny<string>(), false, null)).Returns(Task.CompletedTask);
				mockIO.Setup(x => x.CreateSymlink(It.IsAny<string>(), It.IsAny<string>())).Returns(Task.CompletedTask);

				lock (c)
				{
					Assert.IsTrue(c.Initialize());
					Assert.IsFalse(c.Initialize());
					Assert.IsFalse(c.Compile());
				}
				tcs.Task.Wait();
			}
			Assert.AreEqual(CompilerStatus.Initialized, c.GetStatus());
			Assert.IsNotNull(c.CompileError());

			//do some basic verifications cause this is what initialization really is
			var gameA = IOManager.ConcatPath("Game", "A");
			var gameB = IOManager.ConcatPath("Game", "B");
			var gameLive = IOManager.ConcatPath("Game", "Live");
			var bridgeA = IOManager.ConcatPath(gameA, InteropManager.BridgeDLLName);
			var bridgeB = IOManager.ConcatPath(gameB, InteropManager.BridgeDLLName);
			mockIO.Verify(x => x.DeleteDirectory("Game", false, null));
			mockIO.Verify(x => x.CreateDirectory(gameA));
			mockIO.Verify(x => x.CreateDirectory(gameB));
			mockIO.Verify(x => x.Unlink(gameLive));
			mockIO.Verify(x => x.Unlink(bridgeA));
			mockIO.Verify(x => x.Unlink(bridgeB));
			mockIO.Verify(x => x.CreateSymlink(gameLive, gameA));
			mockStatic.Verify(x => x.SymlinkTo(gameA));
			mockStatic.Verify(x => x.SymlinkTo(gameB));
			mockIO.Verify(x => x.CreateSymlink(bridgeA, InteropManager.BridgeDLLName));
			mockIO.Verify(x => x.CreateSymlink(bridgeB, InteropManager.BridgeDLLName));
		}
	}
}
