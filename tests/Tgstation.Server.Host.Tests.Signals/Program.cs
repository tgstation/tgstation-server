using System;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

using Tgstation.Server.Host.Core;
using Tgstation.Server.Host.Extensions;
using Tgstation.Server.Host.System;

namespace Tgstation.Server.Host.Tests.Signals
{
	static class Program
	{
		static async Task Main()
		{
			if (OperatingSystem.IsWindows())
				throw new InvalidOperationException("Cannot run this test on Windows!");

			await Run();
		}

		[UnsupportedOSPlatform("windows")]
		static async Task Run()
		{
			var mockServerControl = new Mock<IServerControl>();

			var tcs = new TaskCompletionSource<object>();
			mockServerControl
				.Setup(x => x.GracefulShutdown())
				.Callback(() => tcs.SetResult(null))
				.Returns(Task.CompletedTask);

			var mockAsyncDelayer = new Mock<IAsyncDelayer>();
			mockAsyncDelayer.Setup(x => x.Delay(It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
			await using var signalHandler = new PosixSignalHandler(mockServerControl.Object, mockAsyncDelayer.Object, Mock.Of<ILogger<PosixSignalHandler>>());

			Assert.IsFalse(tcs.Task.IsCompleted);

			await signalHandler.StartAsync(default);

			Assert.IsFalse(tcs.Task.IsCompleted);

			Task Start() => signalHandler.StartAsync(default);

			await Assert.ThrowsExceptionAsync<InvalidOperationException>(Start);

			Assert.IsFalse(tcs.Task.IsCompleted);

			using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
			await signalHandler.StopAsync(default).WithToken(cts.Token).ConfigureAwait(false);
			Assert.IsFalse(tcs.Task.IsCompleted);

			await using var signalHandler2 = new PosixSignalHandler(mockServerControl.Object, mockAsyncDelayer.Object, Mock.Of<ILogger<PosixSignalHandler>>());
			await signalHandler2.StartAsync(default);

			using var cts2 = new CancellationTokenSource(TimeSpan.FromSeconds(20));
			await tcs.Task.WithToken(cts2.Token);

			using var cts3 = new CancellationTokenSource(TimeSpan.FromSeconds(5));
			await signalHandler2.StopAsync(default).WithToken(cts3.Token).ConfigureAwait(false);
		}
	}
}
