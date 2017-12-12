using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.ServiceProcess;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace TGS.Server.Service.Tests
{
	/// <summary>
	/// Tests for <see cref="ServiceRunner"/>
	/// </summary>
	[TestClass]
	public sealed class TestServiceRunner
	{
		delegate bool EnumThreadDelegate(IntPtr hWnd, IntPtr lParam);

		[DllImport("user32.dll")]
		static extern bool EnumThreadWindows(int dwThreadId, EnumThreadDelegate lpfn,
			IntPtr lParam);
		[DllImport("user32.dll", CharSet = CharSet.Auto)]
		static extern IntPtr SendMessage(IntPtr hWnd, UInt32 Msg, IntPtr wParam, IntPtr lParam);

		const UInt32 WM_CLOSE = 0x0010;

		void CloseWindow(IntPtr hwnd)
		{
			SendMessage(hwnd, WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
		}

		static IEnumerable<IntPtr> EnumerateProcessWindowHandles(int processId)
		{
			var handles = new List<IntPtr>();

			foreach (ProcessThread thread in Process.GetProcessById(processId).Threads)
				EnumThreadWindows(thread.Id,
					(hWnd, lParam) => { handles.Add(hWnd); return true; }, IntPtr.Zero);

			return handles;
		}
		[TestMethod]
		public void TestRun()
		{
			var SR = new ServiceRunner();
			var task = Task.Run(() => SR.Run(new ServiceBase()));
			while (!task.IsCompleted)
			{
				bool foundAny = false;
				foreach (var I in EnumerateProcessWindowHandles(Process.GetCurrentProcess().Id))
				{
					foundAny = true;
					CloseWindow(I);
				}
				if (!foundAny)
					Thread.Sleep(100);
			}
			task.Wait();
		}
	}
}
