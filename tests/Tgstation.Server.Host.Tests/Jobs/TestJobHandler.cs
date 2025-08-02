using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Tgstation.Server.Host.Jobs.Tests
{
	[TestClass]
	public sealed class TestJobHandler
	{
		Task currentWaitTask;
		bool cancelled;

		async Task<bool> TestJob(CancellationToken cancellationToken)
		{
			await currentWaitTask;
			cancelled = cancellationToken.IsCancellationRequested;
			return true;
		}

		[TestMethod]
		public void TestConstructionAndDisposal()
		{
			cancelled = false;
			Assert.ThrowsExactly<ArgumentNullException>(() => new JobHandler(null));

			currentWaitTask = Task.CompletedTask;
			new JobHandler(TestJob).Dispose();
			Assert.IsFalse(cancelled);
		}

		[TestMethod]
		public async Task TestWait()
		{
			cancelled = false;
			//test with a cancelled cts
			using (var cts = new CancellationTokenSource())
			{
				var tcs = new TaskCompletionSource();
				currentWaitTask = tcs.Task;
				cts.Cancel();
				using var handler = new JobHandler(TestJob);
				await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => handler.Wait(cts.Token));
				handler.Start();
				await Assert.ThrowsExactlyAsync<TaskCanceledException>(() => handler.Wait(cts.Token));
				tcs.SetResult();
				await handler.Wait(default);
			}
			Assert.IsFalse(cancelled);
		}

		[TestMethod]
		public void TestProgress()
		{
			currentWaitTask = Task.CompletedTask;
			cancelled = false;
			using (var handler = new JobHandler(TestJob))
			{
				Assert.IsFalse(handler.Progress.HasValue);
				handler.Progress = 42;
				Assert.AreEqual(42, handler.Progress);
			}
			Assert.IsFalse(cancelled);
		}

		[TestMethod]
		public async Task TestCancellation()
		{
			var tcs = new TaskCompletionSource();
			currentWaitTask = tcs.Task;
			cancelled = false;
			using (var handler = new JobHandler(TestJob))
			{
				handler.Start();
				handler.Cancel();
				tcs.SetResult();
				await handler.Wait(default);
			}
			Assert.IsTrue(cancelled);
		}
	}
}
