using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
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
		public void TestCompileWithMismatchedConfigs()
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
			mockIO.Setup(x => x.FileExists(It.IsAny<string>())).Returns(Task.FromResult(true));
			using (c = new CompilerManager(mockLogger.Object, mockRepoConfigProvider.Object, mockIO.Object, mockConfig.Object, mockChat.Object, mockRepo.Object, mockInterop.Object, mockDD.Object, mockStatic.Object, mockByond.Object, mockEvents.Object))
			{
				//byond install check
				mockByond.Setup(x => x.GetVersion(ByondVersion.Installed)).Returns("511.1385").Verifiable();

				//repo config check
				mockIO.Setup(x => x.FileExists(It.IsAny<string>())).Returns(Task.FromResult(true));
				bool first = true;
				mockIO.Setup(x => x.ReadAllText(It.IsAny<string>())).Returns(() =>
				{
					if (first)
					{
						first = false;
						return Task.FromResult("{}");
					}
					return Task.FromResult("{\"synchronize_paths\": [\"html/changelog.html\", \"html/changelogs/*\"]}");
				}).Verifiable();
				mockRepo.Setup(x => x.GetRepoConfig()).Returns(new RepoConfig("", mockIO.Object)).Verifiable();
				mockRepoConfigProvider.Setup(x => x.GetRepoConfig()).Returns(new RepoConfig("", mockIO.Object)).Verifiable();

				Assert.IsTrue(c.Compile());
			}
			Assert.IsNotNull(c.CompileError());
			Assert.IsNull(c.CompileError());
			mockIO.VerifyAll();
			mockRepo.VerifyAll();
			mockRepoConfigProvider.VerifyAll();
			mockByond.VerifyAll();
			mockChat.Verify(x => x.SendMessage(It.IsAny<string>(), MessageType.DeveloperInfo), Times.AtMostOnce());
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
				mockIO.Setup(x => x.ResolvePath(It.IsAny<string>())).Returns((string x) => { return x; });

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
			mockIO.Verify(x => x.CreateSymlink(bridgeA, InteropManager.BridgeDLLName));
			mockIO.Verify(x => x.CreateSymlink(bridgeB, InteropManager.BridgeDLLName));
		}

		[TestMethod]
		public void TestBasicCompile()
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

			mockIO.Setup(x => x.FileExists(It.IsAny<string>())).Returns(Task.FromResult(true));
			CompilerManager c;
			using (c = new CompilerManager(mockLogger.Object, mockRepoConfigProvider.Object, mockIO.Object, mockConfig.Object, mockChat.Object, mockRepo.Object, mockInterop.Object, mockDD.Object, mockStatic.Object, mockByond.Object, mockEvents.Object))
			{
				string error = null;
				//byond install check
				mockByond.Setup(x => x.GetVersion(ByondVersion.Installed)).Returns("511.1385").Verifiable();

				//repo config check
				mockIO.Setup(x => x.FileExists(It.IsAny<string>())).Returns(Task.FromResult(true));
				mockIO.Setup(x => x.ReadAllText(It.IsAny<string>())).Returns(Task.FromResult("{}"));
				mockRepo.Setup(x => x.GetRepoConfig()).Returns(new RepoConfig("", mockIO.Object)).Verifiable();
				mockRepoConfigProvider.Setup(x => x.GetRepoConfig()).Returns(new RepoConfig("", mockIO.Object)).Verifiable();

				//staging dir lookup
				mockIO.Setup(x => x.DirectoryExists(It.IsAny<string>())).Returns(Task.FromResult(true));
				mockIO.Setup(x => x.Touch(It.IsAny<string>())).Returns(Task.CompletedTask);
				mockIO.Setup(x => x.DeleteFile(It.IsAny<string>())).Returns(Task.CompletedTask);
				mockIO.Setup(x => x.FileExists(It.IsAny<string>())).Returns(Task.FromResult(true));

				//staging directory cleanup
				mockIO.Setup(x => x.DeleteDirectory(It.IsAny<string>(), true, It.IsNotNull<IList<string>>())).Returns(Task.CompletedTask);
				mockIO.Setup(x => x.CreateDirectory(It.IsAny<string>())).Returns(Task.FromResult(new DirectoryInfo(".")));

				//repo copy
				mockStatic.Setup(x => x.SymlinkTo(It.IsNotNull<string>())).Verifiable();
				mockIO.Setup(x => x.CreateSymlink(It.IsAny<string>(), It.IsAny<string>())).Returns(Task.CompletedTask).Verifiable();
				mockRepo.Setup(x => x.CopyTo(It.IsNotNull<string>(), It.IsNotNull<IEnumerable<string>>())).Returns(Task.FromResult<string>(null)).Verifiable();
				mockRepo.Setup(x => x.CopyToRestricted(It.IsNotNull<string>(), It.IsNotNull<IEnumerable<string>>())).Returns(Task.FromResult<string>(null)).Verifiable();
				mockRepo.Setup(x => x.GetHead(false, out error)).Returns("gottenSha").Verifiable();
				mockIO.Setup(x => x.FileExists(It.Is<string>(y => y.Contains(InteropManager.BridgeDLLName)))).Returns(Task.FromResult(false)).Verifiable();

				//precompile
				mockConfig.SetupGet(x => x.ProjectName).Returns("tgstation").Verifiable();
				mockEvents.Setup(x => x.HandleEvent(ActionEvent.Precompile)).Returns(true).Verifiable();

				using (var tp = new TestProcessPath())
				{
					tp.ExitCode = 0;
					mockByond.Setup(x => x.LockDMExecutable(false, out error)).Returns(tp.Path).Verifiable();

					//postcompile
					mockDD.Setup(x => x.RunSuspended(It.IsNotNull<Action>())).Callback((Action x) => x()).Verifiable();
					mockIO.Setup(x => x.Unlink(It.IsAny<string>())).Returns(Task.CompletedTask);
					mockEvents.Setup(x => x.HandleEvent(ActionEvent.Postcompile)).Returns(true).Verifiable();

					var tcs = new TaskCompletionSource<bool>();

					mockLogger.Setup(x => x.WriteInfo(It.IsNotNull<string>(), EventID.DMCompileSuccess)).Callback(() => tcs.SetResult(true)).Verifiable();

					lock (c)
					{
						Assert.IsTrue(c.Compile());
						Assert.IsFalse(c.Compile());
						Assert.IsFalse(c.Initialize());
					}
					tcs.Task.Wait();
				}
			}
			Assert.IsNull(c.CompileError());
			mockRepo.Verify(x => x.UpdateLiveSha("gottenSha"), Times.Once());
			mockInterop.Verify(x => x.WorldAnnounce(It.IsNotNull<string>()), Times.Once());
			mockChat.Verify(x => x.SendMessage(It.IsNotNull<string>(), MessageType.DeveloperInfo), Times.Exactly(2));
			mockIO.VerifyAll();
			mockEvents.VerifyAll();
			mockDD.VerifyAll();
			mockLogger.VerifyAll();
			mockRepoConfigProvider.VerifyAll();
			mockByond.VerifyAll();
			mockStatic.VerifyAll();
		}

		[TestMethod]
		public void TestCompileCancellation()
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

			mockIO.Setup(x => x.FileExists(It.IsAny<string>())).Returns(Task.FromResult(true));
			CompilerManager c;

			using (var tp = new TestInfiniteProcessPath())
			using (c = new CompilerManager(mockLogger.Object, mockRepoConfigProvider.Object, mockIO.Object, mockConfig.Object, mockChat.Object, mockRepo.Object, mockInterop.Object, mockDD.Object, mockStatic.Object, mockByond.Object, mockEvents.Object))
			{
				string error = null;
				//byond install check
				mockByond.Setup(x => x.GetVersion(ByondVersion.Installed)).Returns("511.1385").Verifiable();

				//repo config check
				mockIO.Setup(x => x.FileExists(It.IsAny<string>())).Returns(Task.FromResult(true));
				mockIO.Setup(x => x.ReadAllText(It.IsAny<string>())).Returns(Task.FromResult("{}"));
				mockRepo.Setup(x => x.GetRepoConfig()).Returns(new RepoConfig("", mockIO.Object)).Verifiable();
				mockRepoConfigProvider.Setup(x => x.GetRepoConfig()).Returns(new RepoConfig("", mockIO.Object)).Verifiable();

				//staging dir lookup
				mockIO.Setup(x => x.DirectoryExists(It.IsAny<string>())).Returns(Task.FromResult(true));
				mockIO.Setup(x => x.Touch(It.IsAny<string>())).Returns(Task.CompletedTask);
				mockIO.Setup(x => x.DeleteFile(It.IsAny<string>())).Returns(Task.CompletedTask);
				mockIO.Setup(x => x.FileExists(It.IsAny<string>())).Returns(Task.FromResult(true));

				//staging directory cleanup
				mockIO.Setup(x => x.DeleteDirectory(It.IsAny<string>(), true, It.IsNotNull<IList<string>>())).Returns(Task.CompletedTask);
				mockIO.Setup(x => x.CreateDirectory(It.IsAny<string>())).Returns(Task.FromResult(new DirectoryInfo(".")));

				//repo copy
				mockStatic.Setup(x => x.SymlinkTo(It.IsNotNull<string>())).Verifiable();
				mockIO.Setup(x => x.CreateSymlink(It.IsAny<string>(), It.IsAny<string>())).Returns(Task.CompletedTask).Verifiable();
				mockRepo.Setup(x => x.CopyTo(It.IsNotNull<string>(), It.IsNotNull<IEnumerable<string>>())).Returns(Task.FromResult<string>(null)).Verifiable();
				mockRepo.Setup(x => x.CopyToRestricted(It.IsNotNull<string>(), It.IsNotNull<IEnumerable<string>>())).Returns(Task.FromResult<string>(null)).Verifiable();
				mockRepo.Setup(x => x.GetHead(false, out error)).Returns("gottenSha").Verifiable();
				mockIO.Setup(x => x.FileExists(It.Is<string>(y => y.Contains(InteropManager.BridgeDLLName)))).Returns(Task.FromResult(false)).Verifiable();

				//precompile
				mockConfig.SetupGet(x => x.ProjectName).Returns("tgstation").Verifiable();
				mockEvents.Setup(x => x.HandleEvent(ActionEvent.Precompile)).Returns(true).Verifiable();
				var tcs = new TaskCompletionSource<bool>();
				mockByond.Setup(x => x.LockDMExecutable(false, out error)).Returns(tp.Path).Callback(() => tcs.SetResult(true)).Verifiable();

				mockLogger.Setup(x => x.WriteInfo(It.IsNotNull<string>(), EventID.DMCompileCancel)).Verifiable();

				lock (c)
				{
					Assert.IsTrue(c.Compile());
					Assert.IsFalse(c.Compile());
					Assert.IsFalse(c.Initialize());
				}
				tcs.Task.Wait();
			}
			Assert.IsNull(c.CompileError());
			mockRepo.Verify(x => x.UpdateLiveSha("gottenSha"), Times.Never());
			mockInterop.Verify(x => x.WorldAnnounce(It.IsNotNull<string>()), Times.Never());
			mockChat.Verify(x => x.SendMessage(It.IsNotNull<string>(), MessageType.DeveloperInfo), Times.Exactly(2));
			mockIO.VerifyAll();
			mockEvents.VerifyAll();
			mockDD.VerifyAll();
			mockLogger.VerifyAll();
			mockRepoConfigProvider.VerifyAll();
			mockByond.VerifyAll();
			mockStatic.VerifyAll();
		}
	}
}
