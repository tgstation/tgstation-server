using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace TGS.Server
{
	/// <summary>
	/// Helpers to <see cref="Suspend(Process)"/>ing and <see cref="Resume(Process)"/> a <see cref="Process"/>. Lightly massaged code from https://stackoverflow.com/a/13109774. Documentation linked from MSDN on 20/10/2017
	/// </summary>
	static class ProcessExtension
	{
		/// <summary>
		/// Suspends all threads for a running <see cref="Process"/>
		/// </summary>
		/// <param name="process">The <see cref="Process"/> to suspend</param>
		public static void Suspend(this Process process)
		{
			foreach (ProcessThread thread in process.Threads)
			{
				var pOpenThread = NativeMethods.OpenThread(NativeMethods.ThreadAccess.SUSPEND_RESUME, false, (uint)thread.Id);
				if (pOpenThread == IntPtr.Zero)
					continue;
				NativeMethods.SuspendThread(pOpenThread);
				NativeMethods.CloseHandle(pOpenThread);
			}
		}

		/// <summary>
		/// Resumes all threads for a running <see cref="Process"/>
		/// </summary>
		/// <param name="process">The <see cref="Process"/> to suspend</param>
		public static void Resume(this Process process)
		{
			foreach (ProcessThread thread in process.Threads)
			{
				var pOpenThread = NativeMethods.OpenThread(NativeMethods.ThreadAccess.SUSPEND_RESUME, false, (uint)thread.Id);
				if (pOpenThread == IntPtr.Zero)
					continue;
				NativeMethods.ResumeThread(pOpenThread);
				NativeMethods.CloseHandle(pOpenThread);
			}
		}

		/// <summary>
		/// Waits asynchronously for the <see cref="Process"/> to exit
		/// </summary>
		/// <param name="process">The <see cref="Process"/> to wait for cancellation</param>
		/// <param name="cancellationToken">If invoked, the <see cref="Task"/> will return immediately as canceled</param>
		/// <returns>A <see cref="Task"/> representing waiting for the <see cref="Process"/> to end</returns>
		public static Task WaitForExitAsync(this Process process, CancellationToken cancellationToken)
		{
			var tcs = new TaskCompletionSource<object>();
			process.EnableRaisingEvents = true;
			process.Exited += (sender, args) => tcs.TrySetResult(null);
			cancellationToken.Register(tcs.SetCanceled);
			return tcs.Task;
		}
	}
}
