using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
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
			var args = Array.Empty<string>();
			mockServer.Setup(x => x.RunAsync(false, args, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask).Verifiable();
			var mockServerFactory = new Mock<IWatchdogFactory>();
			mockServerFactory.Setup(x => x.CreateWatchdog(It.IsAny<ILoggerFactory>())).Returns(mockServer.Object).Verifiable();
			Program.WatchdogFactory = mockServerFactory.Object;
			await Program.Main(args);
			mockServer.VerifyAll();
			mockServerFactory.VerifyAll();
		}
	}
}
