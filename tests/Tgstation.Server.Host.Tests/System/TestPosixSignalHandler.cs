using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Mono.Unix.Native;
using Moq;

using Tgstation.Server.Host.Core;
using Tgstation.Server.Host.Extensions;

namespace Tgstation.Server.Host.System.Tests
{
	[TestClass]
	public sealed class TestPosixSignalHandler
	{
		[TestMethod]
		public void TestConstruction()
		{
			Assert.ThrowsException<ArgumentNullException>(() => new PosixSignalHandler(null, null));

			var mockServerControl = Mock.Of<IServerControl>();
			Assert.ThrowsException<ArgumentNullException>(() => new PosixSignalHandler(mockServerControl, null));

			new PosixSignalHandler(mockServerControl, Mock.Of<ILogger<PosixSignalHandler>>());
		}

		[TestMethod]
		public async Task TestSignalListening()
		{
			if (new PlatformIdentifier().IsWindows)
				Assert.Inconclusive("POSIX only test.");

			var mockServerControl = new Mock<IServerControl>();

			var callCount = 0;
			mockServerControl
				.Setup(x => x.GracefulShutdown())
				.Callback(() => ++callCount)
				.Returns(Task.CompletedTask);

			var signalHandler = new PosixSignalHandler(mockServerControl.Object, Mock.Of<ILogger<PosixSignalHandler>>());

			Assert.AreEqual(0, callCount);

			await signalHandler.StartAsync(default);

			Assert.AreEqual(0, callCount);

			await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => signalHandler.StartAsync(default));

			Assert.AreEqual(0, callCount);

			using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
			await signalHandler.StopAsync(default).WithToken(cts.Token).ConfigureAwait(false);
			Assert.AreEqual(0, callCount);

			signalHandler = new PosixSignalHandler(mockServerControl.Object, Mock.Of<ILogger<PosixSignalHandler>>());
			await signalHandler.StartAsync(default);

			await Task.Delay(TimeSpan.FromSeconds(1));
			var currentPid = Syscall.getpid();

			using var killProc = global::System.Diagnostics.Process.Start("kill", $"-SIGUSR1 {currentPid}");
			killProc.WaitForExit();

			await Task.Delay(TimeSpan.FromSeconds(1));
			Assert.AreEqual(1, callCount);

			using var cts2 = new CancellationTokenSource(TimeSpan.FromSeconds(5));
			await signalHandler.StopAsync(default).WithToken(cts2.Token).ConfigureAwait(false);
		}
	}
}
