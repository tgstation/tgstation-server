using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
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
					var po = new PrivateObject(b);
					po.SetField("updateStat", ByondStatus.Idle);
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
				mockIO.ResetCalls();
				mockIO.Setup(x => x.GetURL(It.IsAny<string>())).Returns(Task.FromResult(TestData.SampleBYONDBuildPage));
				Assert.AreEqual("511.1385", b.GetVersion(ByondVersion.Latest));
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
				Assert.IsFalse(b.UpdateToVersion(512, 1395));
				Assert.AreEqual(ByondStatus.Staging, b.CurrentStatus());
				Assert.IsNull(b.LockDDExecutable(out error));
				Assert.IsNull(b.LockDMExecutable(false, out error));
				Assert.IsNull(b.LockDMExecutable(true, out error));
				tcs1.SetResult(true);
				//let it cancel
			}
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
