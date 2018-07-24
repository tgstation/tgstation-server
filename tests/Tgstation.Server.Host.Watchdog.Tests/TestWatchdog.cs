using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Host.Startup;

namespace Tgstation.Server.Host.Watchdog.Tests
{
	[TestClass]
	public sealed class TestWatchdog
	{
		[TestMethod]
		public void TestConstruction()
		{
			Assert.ThrowsException<ArgumentNullException>(() => new Watchdog(null, null, null));
			var mockActiveAssemblyDeleter = new Mock<IActiveLibraryDeleter>();
			Assert.ThrowsException<ArgumentNullException>(() => new Watchdog(mockActiveAssemblyDeleter.Object, null, null));
			var mockIsolatedServerContextFactory = new Mock<IIsolatedAssemblyContextFactory>();
			Assert.ThrowsException<ArgumentNullException>(() => new Watchdog(mockActiveAssemblyDeleter.Object, mockIsolatedServerContextFactory.Object, null));
			var mockLogger = new LoggerFactory().CreateLogger<Watchdog>();
			var wd = new Watchdog(mockActiveAssemblyDeleter.Object, mockIsolatedServerContextFactory.Object, mockLogger);
		}

		class MockServerFactory : IServerFactory
		{
			readonly IServer server;
			public MockServerFactory(IServer server) => this.server = server;
			public IServer CreateServer(string[] args, string updatePath) => server;
		}

		[TestMethod]
		public async Task TestRunAsyncWithoutUpdate()
		{
			var mockServer = new Mock<IServer>();
			var mockServerFactory = new MockServerFactory(mockServer.Object);
			var mockActiveAssemblyDeleter = new Mock<IActiveLibraryDeleter>();
			var mockIsolatedServerContextFactory = new Mock<IIsolatedAssemblyContextFactory>();
			var mockLogger = new LoggerFactory().CreateLogger<Watchdog>();

			var wd = new Watchdog(mockActiveAssemblyDeleter.Object, mockIsolatedServerContextFactory.Object, mockLogger);

			using (var cts = new CancellationTokenSource())
			{
				mockServer.Setup(x => x.RunAsync(cts.Token)).Returns(Task.CompletedTask).Verifiable();
				await wd.RunAsync(Array.Empty<string>(), cts.Token).ConfigureAwait(false);
				mockServer.VerifyAll();
			}
		}
	}
}
