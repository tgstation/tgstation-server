using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using TGS.Server.Logging;

namespace TGS.Server.Service.Tests
{
	/// <summary>
	/// Tests for <see cref="Service"/>
	/// </summary>
	[TestClass]
	public sealed class TestService
	{
		[TestMethod]
		public void TestStartStop()
		{
			var mockServerFactory = new Mock<IServerFactory>();
			var mockServer = new Mock<IServer>();
			mockServerFactory.Setup(x => x.CreateServer(It.IsAny<ILogger>())).Returns(mockServer.Object);
			using (var C = new Service(mockServerFactory.Object))
			{
				mockServerFactory.Verify(x => x.CreateServer(C), Times.Once());
				var PO = new PrivateObject(C);
				var args = new string[] { };
				mockServer.ResetCalls();
				PO.Invoke("OnStart", new object[] { args });
				mockServer.Verify(x => x.Start(args), Times.Once());
				mockServer.ResetCalls();
				PO.Invoke("OnStop");
				mockServer.Verify(x => x.Stop(), Times.Once());
			}
		}

		[TestMethod]
		public void TestLogger()
		{
			var mockServerFactory = new Mock<IServerFactory>();
			var mockServer = new Mock<IServer>();
			mockServerFactory.Setup(x => x.CreateServer(It.IsAny<ILogger>())).Returns(mockServer.Object);
			using (var C = new Service(mockServerFactory.Object))
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
