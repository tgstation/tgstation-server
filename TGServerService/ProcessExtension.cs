﻿using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace TGServerService
{
	//lightly massaged code from https://stackoverflow.com/a/13109774
	public static class ProcessExtension
	{
		enum ThreadAccess : int
		{
			TERMINATE = (0x0001),
			SUSPEND_RESUME = (0x0002),
			GET_CONTEXT = (0x0008),
			SET_CONTEXT = (0x0010),
			SET_INFORMATION = (0x0020),
			QUERY_INFORMATION = (0x0040),
			SET_THREAD_TOKEN = (0x0080),
			IMPERSONATE = (0x0100),
			DIRECT_IMPERSONATION = (0x0200)
		}

		[DllImport("kernel32.dll")]
		static extern IntPtr OpenThread(ThreadAccess dwDesiredAccess, bool bInheritHandle, uint dwThreadId);
		[DllImport("kernel32.dll")]
		static extern bool CloseHandle(IntPtr hObject);
		[DllImport("kernel32.dll")]
		static extern uint SuspendThread(IntPtr hThread);
		[DllImport("kernel32.dll")]
		static extern int ResumeThread(IntPtr hThread);

		public static void Suspend(this Process process)
		{
			foreach (ProcessThread thread in process.Threads)
			{
				var pOpenThread = OpenThread(ThreadAccess.SUSPEND_RESUME, false, (uint)thread.Id);
				if (pOpenThread == IntPtr.Zero)
					continue;
				SuspendThread(pOpenThread);
				CloseHandle(pOpenThread);
			}
		}
		public static void Resume(this Process process)
		{
			foreach (ProcessThread thread in process.Threads)
			{
				var pOpenThread = OpenThread(ThreadAccess.SUSPEND_RESUME, false, (uint)thread.Id);
				if (pOpenThread == IntPtr.Zero)
					continue;
				ResumeThread(pOpenThread);
				CloseHandle(pOpenThread);
			}
		}
	}
}
