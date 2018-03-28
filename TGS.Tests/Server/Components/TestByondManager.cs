using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using TGS.Interface;
using TGS.Server.Chat.Commands;
using TGS.Server.IO;
using TGS.Server.Logging;
using TGS.Tests;

namespace TGS.Server.Components.Tests
{
	/// <summary>
	/// Tests for <see cref="ByondManager"/>
	/// </summary>
	[TestClass]
	public sealed class TestByondManager
	{
		void SetUpdateStat(ByondManager b, ByondStatus stat)
		{
			var po = new PrivateObject(b);
			po.SetField("updateStat", stat);
		}

		[TestMethod]
		public void TestBasics()
		{
			var mockLogger = new Mock<IInstanceLogger>();
			var mockIO = new Mock<IIOManager>();
			mockIO.Setup(x => x.DeleteFile(It.IsAny<string>())).Returns(Task.CompletedTask);
			mockIO.Setup(x => x.DeleteDirectory(It.IsAny<string>(), false, null)).Returns(Task.CompletedTask);
			var mockChat = new Mock<IChatManager>();
			var mockInterop = new Mock<IInteropManager>();
			using (var b = new ByondManager(mockLogger.Object, mockIO.Object, mockChat.Object, mockInterop.Object))
			{
				mockIO.Verify(x => x.DeleteFile(It.IsAny<string>()), Times.Once());
				mockIO.Verify(x => x.DeleteDirectory(It.IsAny<string>(), false, null), Times.Once());
				mockIO.ResetCalls();
				Assert.AreEqual(ByondStatus.Idle, b.CurrentStatus());
				Assert.IsNull(b.GetVersion(ByondVersion.Installed));
				Assert.IsTrue(b.GetVersion(ByondVersion.Latest).Contains("Error"));
				Assert.IsNull(b.GetVersion(ByondVersion.Staged));
			}
			mockIO.Verify(x => x.DeleteFile(It.IsAny<string>()), Times.Once());
			mockIO.Verify(x => x.DeleteDirectory(It.IsAny<string>(), false, null), Times.Once());
		}

		[TestMethod]
		public void TestCommandInfoPopulation()
		{
			var mockLogger = new Mock<IInstanceLogger>();
			var mockIO = new Mock<IIOManager>();
			mockIO.Setup(x => x.DeleteFile(It.IsAny<string>())).Returns(Task.CompletedTask);
			mockIO.Setup(x => x.DeleteDirectory(It.IsAny<string>(), false, null)).Returns(Task.CompletedTask);
			var mockChat = new Mock<IChatManager>();
			var mockInterop = new Mock<IInteropManager>();
			using (var b = new ByondManager(mockLogger.Object, mockIO.Object, mockChat.Object, mockInterop.Object))
			{
				var ci = new PopulateCommandInfoEventArgs(new CommandInfo());
				mockChat.Raise(x => x.OnPopulateCommandInfo += null, ci);
				Assert.AreSame(ci.CommandInfo.Byond, b);
			}
		}

		[TestMethod]
		public void TestGetLastError()
		{
			var mockLogger = new Mock<IInstanceLogger>();
			var mockIO = new Mock<IIOManager>();
			mockIO.Setup(x => x.DeleteFile(It.IsAny<string>())).Returns(Task.CompletedTask);
			mockIO.Setup(x => x.DeleteDirectory(It.IsAny<string>(), false, null)).Returns(Task.CompletedTask);
			var mockChat = new Mock<IChatManager>();
			var mockInterop = new Mock<IInteropManager>();

			ByondManager b;
			using (b = new ByondManager(mockLogger.Object, mockIO.Object, mockChat.Object, mockInterop.Object))
			{
				Assert.IsNull(b.GetError());
				var tcs = new TaskCompletionSource<bool>();
				mockIO.Setup(x => x.DownloadFile(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.FromException(new TestException())).Callback(() => tcs.SetResult(true));
				b.UpdateToVersion(511, 1395);
				tcs.Task.Wait();
			}
			Assert.IsNotNull(b.GetError());
			Assert.IsNull(b.GetError());
		}

		[TestMethod]
		public void TestBadUpdate()
		{
			var mockLogger = new Mock<IInstanceLogger>();
			var mockIO = new Mock<IIOManager>();
			mockIO.Setup(x => x.DeleteFile(It.IsAny<string>())).Returns(Task.CompletedTask);
			mockIO.Setup(x => x.DeleteDirectory(It.IsAny<string>(), false, null)).Returns(Task.CompletedTask);
			var mockChat = new Mock<IChatManager>();
			var mockInterop = new Mock<IInteropManager>();
			
			ByondManager b;
			using (b = new ByondManager(mockLogger.Object, mockIO.Object, mockChat.Object, mockInterop.Object))
			{
				Assert.IsNull(b.GetError());
				var tcs = new TaskCompletionSource<bool>();
				mockIO.Setup(x => x.DownloadFile(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).Callback(() => tcs.SetResult(true)).Throws(new TestException());
				b.UpdateToVersion(511, 1395);
				tcs.Task.Wait();
			}

			Assert.IsNotNull(b.GetError());
			Assert.IsNull(b.GetError());
		}

		[TestMethod]
		public void TestClearCache()
		{
			var mockLogger = new Mock<IInstanceLogger>();
			var mockIO = new Mock<IIOManager>();
			mockIO.Setup(x => x.DeleteFile(It.IsAny<string>())).Returns(Task.CompletedTask);
			var mockChat = new Mock<IChatManager>();
			var mockInterop = new Mock<IInteropManager>();

			ByondManager b;
			using (b = new ByondManager(mockLogger.Object, mockIO.Object, mockChat.Object, mockInterop.Object))
			{
				mockIO.ResetCalls();
				mockIO.Setup(x => x.DeleteDirectory(It.IsAny<string>(), true, null)).Returns(Task.FromException(new TestException()));
				b.ClearCache();
				mockIO.Verify(x => x.DeleteDirectory(It.IsAny<string>(), true, null), Times.Once());
				mockIO.Setup(x => x.DeleteDirectory(It.IsAny<string>(), true, null)).Returns(Task.CompletedTask);
				mockIO.ResetCalls();
				b.ClearCache();
				mockIO.Verify(x => x.DeleteDirectory(It.IsAny<string>(), true, null), Times.Once());
			}
		}

		[TestMethod]
		public void TestLocks()
		{
			var mockLogger = new Mock<IInstanceLogger>();
			var mockIO = new Mock<IIOManager>();
			mockIO.Setup(x => x.DeleteFile(It.IsAny<string>())).Returns(Task.CompletedTask);
			mockIO.Setup(x => x.DeleteDirectory(It.IsAny<string>(), false, null)).Returns(Task.CompletedTask);
			var mockChat = new Mock<IChatManager>();
			var mockInterop = new Mock<IInteropManager>();

			ByondManager b;
			using (b = new ByondManager(mockLogger.Object, mockIO.Object, mockChat.Object, mockInterop.Object))
			{
				Assert.ThrowsException<InvalidOperationException>(() => b.UnlockDDExecutable());
				Assert.ThrowsException<InvalidOperationException>(() => b.UnlockDMExecutable());

				Assert.IsNull(b.LockDDExecutable(out string error));
				Assert.IsNotNull(error);
				Assert.IsNull(b.LockDMExecutable(false, out error));
				Assert.IsNotNull(error);
				Assert.IsNull(b.LockDMExecutable(true, out error));
				Assert.IsNotNull(error);

				mockIO.Setup(x => x.DirectoryExists(It.IsAny<string>())).Returns(Task.FromResult(true));
				mockIO.Setup(x => x.FileExists(It.IsAny<string>())).Returns(Task.FromResult(true));
				mockIO.Setup(x => x.ReadAllText(It.IsAny<string>())).Returns(Task.FromResult("511.1394"));
				mockIO.Setup(x => x.ResolvePath(It.IsAny<string>())).Returns("SomePath");
				
				Assert.IsNotNull(b.LockDDExecutable(out error));
				Assert.IsNull(error);
				Assert.IsNotNull(b.LockDDExecutable(out error));
				Assert.IsNull(error);
				Assert.IsNotNull(b.LockDMExecutable(false, out error));
				Assert.IsNull(error);
				SetUpdateStat(b, ByondStatus.Staged);
				Assert.IsNotNull(b.LockDMExecutable(true, out error));
				SetUpdateStat(b, ByondStatus.Idle);
				Assert.IsNull(error);

				b.UnlockDDExecutable();
				b.UnlockDDExecutable();
				b.UnlockDMExecutable();
				b.UnlockDMExecutable();

				Assert.IsNotNull(b.LockDDExecutable(out error));
				Assert.IsNull(error);
				Assert.IsNotNull(b.LockDDExecutable(out error));
				Assert.IsNull(error);
				Assert.IsNotNull(b.LockDMExecutable(false, out error));
				Assert.IsNull(error);
				b.UnlockDMExecutable();
				b.UnlockDDExecutable();
				b.UnlockDDExecutable();
			}
		}

		[TestMethod]
		public void TestApplyStagedUpdate()
		{
			var mockLogger = new Mock<IInstanceLogger>();
			var mockIO = new Mock<IIOManager>();
			mockIO.Setup(x => x.DeleteFile(It.IsAny<string>())).Returns(Task.CompletedTask);
			mockIO.Setup(x => x.DeleteDirectory(It.IsAny<string>(), false, null)).Returns(Task.CompletedTask);
			var mockChat = new Mock<IChatManager>();
			var mockInterop = new Mock<IInteropManager>();

			ByondManager b;
			using (b = new ByondManager(mockLogger.Object, mockIO.Object, mockChat.Object, mockInterop.Object))
			{
				Assert.IsFalse(b.ApplyStagedUpdate());
				Assert.IsNull(b.GetError());

				var tcs1 = new TaskCompletionSource<bool>();
				var tcs2 = new TaskCompletionSource<bool>();
				mockIO.Setup(x => x.MoveDirectory(It.IsAny<string>(), It.IsAny<string>())).Callback(() =>
				{
					tcs1.SetResult(true);
					tcs2.Task.Wait();
				}).Returns(Task.CompletedTask);
				SetUpdateStat(b, ByondStatus.Staged);
				var updateTask = Task.Run(() => b.ApplyStagedUpdate());
				tcs1.Task.Wait();
				Assert.AreEqual(ByondStatus.Updating, b.CurrentStatus());
				Assert.IsNull(b.LockDDExecutable(out string error));
				Assert.IsNull(b.LockDMExecutable(true, out error));
				Assert.IsNull(b.LockDMExecutable(false, out error));
				tcs2.SetResult(true);
				Assert.IsTrue(updateTask.Result);
				Assert.AreEqual(ByondStatus.Idle, b.CurrentStatus());
				Assert.IsNull(b.GetError());

				SetUpdateStat(b, ByondStatus.Staged);
				mockIO.Setup(x => x.MoveDirectory(It.IsAny<string>(), It.IsAny<string>())).Throws(new TestException());
				Assert.IsFalse(b.ApplyStagedUpdate());
				Assert.IsNotNull(b.GetError());
				Assert.IsNull(b.GetError());
			}
		}

		[TestMethod]
		public void TestUpdateWithoutApply()
		{
			var mockLogger = new Mock<IInstanceLogger>();
			var mockIO = new Mock<IIOManager>();
			mockIO.Setup(x => x.DeleteFile(It.IsAny<string>())).Returns(Task.CompletedTask);
			mockIO.Setup(x => x.DeleteDirectory(It.IsAny<string>(), false, null)).Returns(Task.CompletedTask);
			var mockChat = new Mock<IChatManager>();
			var mockInterop = new Mock<IInteropManager>();

			ByondManager b;
			using (b = new ByondManager(mockLogger.Object, mockIO.Object, mockChat.Object, mockInterop.Object))
			{
				mockIO.Setup(x => x.DeleteFile(It.IsAny<string>())).Returns(Task.CompletedTask);
				mockIO.Setup(x => x.DownloadFile(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
				mockIO.Setup(x => x.UnzipFile(It.IsAny<string>(), It.IsAny<string>())).Returns(Task.CompletedTask);
				mockIO.Setup(x => x.CreateDirectory(It.IsAny<string>())).Returns(Task.FromResult(new DirectoryInfo(".")));
				mockIO.Setup(x => x.MoveDirectory(It.IsAny<string>(), It.IsAny<string>())).Returns(Task.CompletedTask);
				mockIO.Setup(x => x.DeleteDirectory(It.IsAny<string>(), false, null)).Returns(Task.CompletedTask);
				mockIO.Setup(x => x.WriteAllText(It.IsAny<string>(), It.IsAny<string>())).Returns(Task.CompletedTask);
				mockIO.Setup(x => x.DirectoryExists(It.IsAny<string>())).Returns(Task.FromResult(true));
				mockIO.Setup(x => x.FileExists(It.IsAny<string>())).Returns(Task.FromResult(true));
				mockIO.Setup(x => x.ReadAllText(It.IsAny<string>())).Returns(Task.FromResult("511.1394"));
				b.LockDDExecutable(out string error);
				var tcs = new TaskCompletionSource<bool>();
				mockInterop.Setup(x => x.SendCommand(InteropCommand.RestartOnWorldReboot, null)).Callback(() => tcs.SetResult(true));
				b.UpdateToVersion(511, 1395);
				tcs.Task.Wait();
			}
			Assert.IsNotNull(b.GetError());
		}
		
		[TestMethod]
		public void TestBadUpdateStart()
		{
			var mockLogger = new Mock<IInstanceLogger>();
			var mockIO = new Mock<IIOManager>();
			mockIO.Setup(x => x.DeleteFile(It.IsAny<string>())).Returns(Task.CompletedTask);
			mockIO.Setup(x => x.DeleteDirectory(It.IsAny<string>(), false, null)).Returns(Task.CompletedTask);
			var mockChat = new Mock<IChatManager>();
			var mockInterop = new Mock<IInteropManager>();

			ByondManager b;
			using (b = new ByondManager(mockLogger.Object, mockIO.Object, mockChat.Object, mockInterop.Object))
				lock (b)
				{
					b.UpdateToVersion(511, 1395);
					SetUpdateStat(b, ByondStatus.Idle);
				}
			mockIO.Verify(x => x.DownloadFile(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never());
			Assert.AreEqual(ByondStatus.Idle, b.CurrentStatus());
		}

		[TestMethod]
		public void TestGetVersion()
		{
			var mockLogger = new Mock<IInstanceLogger>();
			var mockIO = new Mock<IIOManager>();
			mockIO.Setup(x => x.DeleteFile(It.IsAny<string>())).Returns(Task.CompletedTask);
			mockIO.Setup(x => x.DeleteDirectory(It.IsAny<string>(), false, null)).Returns(Task.CompletedTask);
			var mockChat = new Mock<IChatManager>();
			var mockInterop = new Mock<IInteropManager>();


			using (var b = new ByondManager(mockLogger.Object, mockIO.Object, mockChat.Object, mockInterop.Object))
			{
				string path1 = null;
				mockIO.Setup(x => x.DirectoryExists(It.IsAny<string>())).Returns(Task.FromResult(true));
				mockIO.Setup(x => x.FileExists(It.IsAny<string>())).Returns(Task.FromResult(true));

				mockIO.Setup(x => x.ReadAllText(It.IsAny<string>())).Callback<string>((a) => path1 = path1 ?? a).Returns(Task.FromResult("asdfasdfasdf"));
				Assert.AreEqual("asdfasdfasdf", b.GetVersion(ByondVersion.Installed));
				mockIO.Verify(x => x.ReadAllText(It.IsAny<string>()), Times.Once());
				mockIO.ResetCalls();
				Assert.AreEqual("asdfasdfasdf", b.GetVersion(ByondVersion.Staged));
				mockIO.Verify(x => x.ReadAllText(It.IsAny<string>()), Times.Once());
				mockIO.Verify(x => x.ReadAllText(path1), Times.Never());
				mockIO.Setup(x => x.GetURL(It.IsAny<string>())).Returns(Task.FromResult(TestData.SampleBYONDBuildPage));
				Assert.AreEqual("511.1385", b.GetVersion(ByondVersion.Latest));
				mockIO.Setup(x => x.GetURL(It.IsAny<string>())).Returns(Task.FromResult(String.Empty));
				Assert.IsNull(b.GetVersion(ByondVersion.Latest));
				mockIO.Setup(x => x.GetURL(It.IsAny<string>())).Returns(Task.FromResult(TestData.SampleBYONDBuildPage.Replace("_byond.exe", "_notbyond.exe")));
				Assert.IsNull(b.GetVersion(ByondVersion.Latest));
			}
		}

		[TestMethod]
		public void TestUpdate()
		{
			var mockLogger = new Mock<IInstanceLogger>();
			var mockIO = new Mock<IIOManager>();
			mockIO.Setup(x => x.DownloadFile(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
			mockIO.Setup(x => x.DeleteFile(It.IsAny<string>())).Returns(Task.CompletedTask);
			mockIO.Setup(x => x.CreateDirectory(It.IsAny<string>())).Returns(Task.FromResult(new DirectoryInfo(".")));
			mockIO.Setup(x => x.MoveDirectory(It.IsAny<string>(), It.IsAny<string>())).Returns(Task.CompletedTask);
			mockIO.Setup(x => x.DeleteDirectory(It.IsAny<string>(), false, null)).Returns(Task.CompletedTask);
			mockIO.Setup(x => x.WriteAllText(It.IsAny<string>(), It.IsAny<string>())).Returns(Task.CompletedTask);
			var mockChat = new Mock<IChatManager>();
			var mockInterop = new Mock<IInteropManager>();
			ByondManager b;
			using (b = new ByondManager(mockLogger.Object, mockIO.Object, mockChat.Object, mockInterop.Object))
			{
				var tcs1 = new TaskCompletionSource<bool>();
				var tcs2 = new TaskCompletionSource<bool>();
				var tcs3 = new TaskCompletionSource<bool>();
				string error;
				lock (b)
				{
					Assert.IsTrue(b.UpdateToVersion(512, 1395));
					Assert.IsFalse(b.UpdateToVersion(512, 1395));
					Assert.AreEqual(ByondStatus.Starting, b.CurrentStatus());
					Assert.IsNull(b.LockDDExecutable(out error));
					Assert.IsNull(b.LockDMExecutable(false, out error));
					Assert.IsNull(b.LockDMExecutable(true, out error));
					mockChat.Setup(x => x.SendMessage(It.IsAny<string>(), MessageType.DeveloperInfo)).Callback(() =>
					{
						tcs2.SetResult(true);
						tcs3.Task.Wait();
					});
				}

				tcs2.Task.Wait();
				Assert.IsFalse(b.UpdateToVersion(512, 1395));
				Assert.AreEqual(ByondStatus.Downloading, b.CurrentStatus());
				Assert.IsNull(b.LockDDExecutable(out error));
				Assert.IsNull(b.LockDMExecutable(false, out error));
				Assert.IsNull(b.LockDMExecutable(true, out error));
				tcs2 = new TaskCompletionSource<bool>();
				mockIO.Setup(x => x.UnzipFile(It.IsAny<string>(), It.IsAny<string>())).Returns(Task.CompletedTask).Callback(() =>
				{
					tcs2.SetResult(true);
					tcs1.Task.Wait();
				});
				tcs3.SetResult(true);

				tcs2.Task.Wait();
				mockChat.Reset();
				Assert.IsFalse(b.UpdateToVersion(512, 1395));
				Assert.AreEqual(ByondStatus.Staging, b.CurrentStatus());

				mockIO.Setup(x => x.DirectoryExists(It.IsAny<string>())).Returns(Task.FromResult(true));
				mockIO.Setup(x => x.FileExists(It.IsAny<string>())).Returns(Task.FromResult(true));
				mockIO.Setup(x => x.ReadAllText(It.IsAny<string>())).Returns(Task.FromResult("511.1394"));
				mockIO.Setup(x => x.ResolvePath(It.IsAny<string>())).Returns("SomePath");

				Assert.IsNotNull(b.LockDDExecutable(out error));
				Assert.IsNotNull(b.LockDMExecutable(false, out error));
				Assert.IsNotNull(b.LockDMExecutable(true, out error));
				tcs1.SetResult(true);
				//let it cancel
			}
			Assert.IsNull(b.GetError());
			Assert.AreEqual(ByondStatus.Staged, b.CurrentStatus());
		}
		[TestMethod]
		public void TestUpdateWithDownloadFileCancellation()
		{
			var mockLogger = new Mock<IInstanceLogger>();
			var mockIO = new Mock<IIOManager>();
			mockIO.Setup(x => x.DeleteFile(It.IsAny<string>())).Returns(Task.CompletedTask);
			mockIO.Setup(x => x.DownloadFile(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
			mockIO.Setup(x => x.CreateDirectory(It.IsAny<string>())).Returns(Task.FromResult(new DirectoryInfo(".")));
			mockIO.Setup(x => x.MoveDirectory(It.IsAny<string>(), It.IsAny<string>())).Returns(Task.CompletedTask);
			mockIO.Setup(x => x.DeleteDirectory(It.IsAny<string>(), false, null)).Returns(Task.CompletedTask);
			mockIO.Setup(x => x.WriteAllText(It.IsAny<string>(), It.IsAny<string>())).Returns(Task.CompletedTask);
			var mockChat = new Mock<IChatManager>();
			var mockInterop = new Mock<IInteropManager>();
			ByondManager b;
			using (b = new ByondManager(mockLogger.Object, mockIO.Object, mockChat.Object, mockInterop.Object))
			{
				var tcs1 = new TaskCompletionSource<bool>();
				mockIO.Setup(x => x.DownloadFile(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).Callback<string, string, CancellationToken>((a, sf, c) =>
				{
					tcs1.SetResult(true);
					c.WaitHandle.WaitOne();
				});
				b.UpdateToVersion(511, 1385);
				tcs1.Task.Wait();
				//let it cancel
			}
		}
	}
}
