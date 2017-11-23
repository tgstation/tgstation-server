using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.IO;
using TGS.TestHelpers;

namespace TGS.Server.Console.Tests
{
	/// <summary>
	/// Tests for <see cref="Console"/>
	/// </summary>
	[TestClass]
	sealed public class TestConsole
	{
		TextReader oldIn;

		[TestInitialize]
		public void Init()
		{
			oldIn = System.Console.In;
			System.Console.SetIn(new FakeTextReader());
		}

		[TestCleanup]
		public void Cleanup()
		{
			System.Console.SetIn(oldIn);
		}

		[TestMethod]
		public void TestGoodRun()
		{
			var mockServerFactory = new Mock<IServerFactory>();
			var mockServer = new Mock<IServer>();
			mockServerFactory.Setup(x => x.CreateServer(It.IsAny<ILogger>())).Returns(mockServer.Object);
			var args = new string[] { };
			using (var C = new Console(mockServerFactory.Object))
			{
				mockServerFactory.Verify(x => x.CreateServer(C), Times.Once());
				C.Run(args);
			}

			mockServer.Verify(x => x.Start(args), Times.Once());
			mockServer.Verify(x => x.Stop(), Times.Once());
			mockServer.Verify(x => x.Dispose(), Times.Once());
		}

		[TestMethod]
		public void TestBadRun()
		{
			var mockServerFactory = new Mock<IServerFactory>();
			var mockServer = new Mock<IServer>();
			mockServerFactory.Setup(x => x.CreateServer(It.IsAny<ILogger>())).Returns(mockServer.Object);
			var args = new string[] { };
			using (var C = new Console(mockServerFactory.Object))
			{
				mockServerFactory.Verify(x => x.CreateServer(C), Times.Once());
				mockServer.Setup(x => x.Start(args)).Throws<TestException>();
				C.Run(args);
			}

			mockServer.Verify(x => x.Start(args), Times.Once());
			mockServer.Verify(x => x.Dispose(), Times.Once());
		}

		[TestMethod]
		public void TestLogger()
		{
			var mockServerFactory = new Mock<IServerFactory>();
			var mockServer = new Mock<IServer>();
			mockServerFactory.Setup(x => x.CreateServer(It.IsAny<ILogger>())).Returns(mockServer.Object);
			using (var C = new Console(mockServerFactory.Object))
			{
				C.WriteAccess("user", true, 0);
				C.WriteAccess("user2", false, 0);
				C.WriteError("some error", EventID.BridgeDLLUpdated, 1);
				C.WriteWarning("some warning", EventID.ChatCommand, 2);
				C.WriteInfo("some info", EventID.BYONDUpdateComplete, 3);
			}

		}
	}
}
