using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Tgstation.Server.Host.Core.Tests
{
	[TestClass]
	public sealed class TestAsyncDelayer
	{
		[TestMethod]
		public async Task TestDelay()
		{
			var delayer = new AsyncDelayer();
			var startDelay = delayer.Delay(TimeSpan.FromSeconds(1), default);
			var checkDelay = Task.Delay(TimeSpan.FromSeconds(1) - TimeSpan.FromMilliseconds(100), default);
			await startDelay.ConfigureAwait(false);
			Assert.IsTrue(checkDelay.IsCompleted);
		}

		[TestMethod]
		public async Task TestCancel()
		{
			var delayer = new AsyncDelayer();
			using var cts = new CancellationTokenSource();
			cts.Cancel();
			await Assert.ThrowsExceptionAsync<TaskCanceledException>(() => delayer.Delay(TimeSpan.FromSeconds(1), cts.Token)).ConfigureAwait(false);
		}
	}
}
