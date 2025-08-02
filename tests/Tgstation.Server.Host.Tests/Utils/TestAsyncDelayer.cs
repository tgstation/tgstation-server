using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Moq;

namespace Tgstation.Server.Host.Utils.Tests
{
	[TestClass]
	public sealed class TestAsyncDelayer
	{
		[TestMethod]
		public async Task TestDelay()
		{
			var delayer = new AsyncDelayer(Mock.Of<ILogger<AsyncDelayer>>());
			var startDelay = delayer.Delay(TimeSpan.FromSeconds(1), CancellationToken.None);
			var checkDelay = Task.Delay(TimeSpan.FromSeconds(1) - TimeSpan.FromMilliseconds(100), CancellationToken.None);
			await startDelay;
			Assert.IsTrue(checkDelay.IsCompleted);
		}

		[TestMethod]
		public async Task TestCancel()
		{
			var delayer = new AsyncDelayer(Mock.Of<ILogger<AsyncDelayer>>());
			using var cts = new CancellationTokenSource();
			cts.Cancel();
			await Assert.ThrowsExactlyAsync<TaskCanceledException>(() => delayer.Delay(TimeSpan.FromSeconds(1), cts.Token).AsTask());
		}
	}
}
