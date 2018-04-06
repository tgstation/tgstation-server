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
			var mockServerFactory = new Mock<IServerFactory>();
			Assert.ThrowsException<ArgumentNullException>(() => new Watchdog(mockServerFactory.Object, null, null));
			var mockActiveAssemblyDeleter = new Mock<IActiveAssemblyDeleter>();
			Assert.ThrowsException<ArgumentNullException>(() => new Watchdog(mockServerFactory.Object, mockActiveAssemblyDeleter.Object, null));
			var mockIsolatedServerContextFactory = new Mock<IIsolatedAssemblyContextFactory>();
			var wd = new Watchdog(mockServerFactory.Object, mockActiveAssemblyDeleter.Object, mockIsolatedServerContextFactory.Object);
		}

		class MockServerFactory : IServerFactory
		{
			readonly IServer server;
			public MockServerFactory(IServer server) => this.server = server;
			public IServer CreateServer() => server;
		}

		[TestMethod]
		public async Task TestRunAsyncWithoutUpdate()
		{
			var mockServer = new Mock<IServer>();
			var mockServerFactory = new MockServerFactory(mockServer.Object);
			var mockActiveAssemblyDeleter = new Mock<IActiveAssemblyDeleter>();
			var mockIsolatedServerContextFactory = new Mock<IIsolatedAssemblyContextFactory>();

			var wd = new Watchdog(mockServerFactory, mockActiveAssemblyDeleter.Object, mockIsolatedServerContextFactory.Object);

			using (var cts = new CancellationTokenSource())
			{
				mockServer.Setup(x => x.RunAsync(It.IsNotNull<string[]>(), cts.Token)).Returns(Task.CompletedTask).Verifiable();
				await wd.RunAsync(Array.Empty<string>(), cts.Token).ConfigureAwait(false);
				mockServer.VerifyAll();
			}
		}

		[TestMethod]
		public async Task TestRunAsyncWithUpdate()
		{
			var mockServer = new Mock<IServer>();
			mockServer.Setup(x => x.UpdatePath).Returns(GetType().Assembly.Location).Verifiable();
			var mockServerFactory = new MockServerFactory(mockServer.Object);
			var mockActiveAssemblyDeleter = new Mock<IActiveAssemblyDeleter>();
			var mockIsolatedServerContextFactory = new Mock<IIsolatedAssemblyContextFactory>();
			mockIsolatedServerContextFactory.Setup(x => x.CreateIsolatedServerFactory(GetType().Assembly.Location)).Returns(mockServerFactory).Verifiable();

			var wd = new Watchdog(mockServerFactory, mockActiveAssemblyDeleter.Object, mockIsolatedServerContextFactory.Object);

			using (var cts = new CancellationTokenSource())
			{
				int count = 0;
				mockServer.Setup(x => x.RunAsync(It.IsNotNull<string[]>(), cts.Token)).Callback(() =>
				{
					if (++count > 1)
						cts.Cancel();
				}).Returns(Task.CompletedTask).Verifiable();
				await wd.RunAsync(Array.Empty<string>(), cts.Token).ConfigureAwait(false);

				mockServer.VerifyAll();
				mockActiveAssemblyDeleter.Verify(x => x.DeleteActiveAssembly(GetType().Assembly.Location), Times.Exactly(2));
				mockIsolatedServerContextFactory.VerifyAll();
			}
		}
	}
}
