using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

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

		public static void WriteMinidump(this Process process, FileStream fileStream)
		{
			NativeMethods.MiniDumpWriteDump(process.Handle, (uint)process.Id, fileStream.SafeFileHandle, (uint)(NativeMethods.Option.Normal), IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
		}
	}
}
