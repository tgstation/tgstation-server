using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tgstation.Server.Host.Utils.Tests
{
	[TestClass]
	public sealed class TestFifoSemaphore
	{
		[TestMethod]
		public async Task TestContention()
		{
			const int Count = 1000000;

			using var cts = new CancellationTokenSource();
			cts.Cancel();

			using var semaphore = new FifoSemaphore();
			var tcs = new TaskCompletionSource();

			var orderTotal = 0;
			var orderActual = 0;
			var entryCount = 0;

			async Task LockAndUnlock(int? expectedOrder)
			{
				try
				{
					using (await semaphore.Lock(expectedOrder.HasValue ? CancellationToken.None : cts.Token))
					{
						Assert.AreEqual(1, Interlocked.Increment(ref entryCount));
						await tcs.Task;
						Assert.AreEqual(0, Interlocked.Decrement(ref entryCount));
						Assert.AreEqual(expectedOrder.Value, ++orderActual);
					}
				}
				catch (OperationCanceledException)
				{
					Assert.IsFalse(expectedOrder.HasValue);
				}
			}

			var tasks = new List<Task>(Count);
			for (var i = 0; i < Count; ++i)
				tasks.Add(
					LockAndUnlock(
						(i % 3 == 0)
							? null
							: ++orderTotal));

			var totalCancelled = tasks.Count(x => x.IsCompleted);
			Assert.AreEqual((Count / 3) + 1, totalCancelled);

			tcs.SetResult();

			await Task.WhenAll(tasks).WaitAsync(TimeSpan.FromMinutes(2));
		}
	}
}
