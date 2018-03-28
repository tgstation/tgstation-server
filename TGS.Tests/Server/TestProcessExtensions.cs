using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using TGS.Tests;

namespace TGS.Server.Tests
{
	[TestClass]
	public sealed class TestProcessExtensions
	{
		void CheckCloseProcess(Process p)
		{
			if (!p.HasExited)
			{
				p.Kill();
				p.WaitForExit();
			}
		}

		async Task TestWaitForExitAsync(bool withCancellation)
		{
			using (var pp = new TestInfiniteProcessPath())
			using (var p = new Process())
			{
				try
				{
					p.StartInfo.FileName = pp.Path;
					p.StartInfo.CreateNoWindow = true;
					p.Start();
					CancellationTokenSource cts = null;
					if (withCancellation)
						cts = new CancellationTokenSource();
					var t2 = Task.Run(() =>
					{
						if (withCancellation)
							cts.Cancel();
						else
							p.Kill();
					});
					await p.WaitForExitAsync(withCancellation ? cts.Token : CancellationToken.None);
					await t2;
				}
				catch (OperationCanceledException) { }
				finally
				{
					CheckCloseProcess(p);
				}
			}
		}

		[TestMethod]
		public void TestSuspendResume()
		{
			using (var pp = new TestInfiniteProcessPath())
			using (var p = new Process())
			{
				try
				{
					p.StartInfo.FileName = pp.Path;
					p.StartInfo.CreateNoWindow = true;
					p.Start();
					p.Suspend();
					p.Resume();
				}
				finally
				{
					CheckCloseProcess(p);
				}
			}
		}

		[TestMethod]
		public async Task TestWaitForExitAsync()
		{
			await TestWaitForExitAsync(false);
		}

		[TestMethod]
		public async Task TestWaitForExitAsyncWithCancellation()
		{
			await TestWaitForExitAsync(true);
		}
	}
}
