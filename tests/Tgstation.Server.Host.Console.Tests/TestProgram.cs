using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Host.Watchdog;

namespace Tgstation.Server.Host.Console.Tests
{
	[TestClass]
	public class TestProgram
	{
		[TestMethod]
		public async Task TestProgramRuns()
		{
			var mockServer = new Mock<IWatchdog>();
			mockServer.Setup(x => x.RunAsync(null, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask).Verifiable();
			var mockServerFactory = new Mock<IWatchdogFactory>();
			mockServerFactory.Setup(x => x.CreateWatchdog()).Returns(mockServer.Object).Verifiable();
			Program.WatchdogFactory = mockServerFactory.Object;
			await Program.Main(null).ConfigureAwait(false);
			mockServer.VerifyAll();
			mockServerFactory.VerifyAll();
		}
	}
}
