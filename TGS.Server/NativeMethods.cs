using System;
using System.Runtime.InteropServices;

namespace TGS.Server
{
	static class NativeMethods
	{
		/// <summary>
		/// https://docs.microsoft.com/en-us/windows/desktop/api/winbase/nf-winbase-createsymboliclinkw#parameters
		/// </summary>
		public enum SymbolicLink
		{
			File = 0,
			Directory = 1
		}

		/// <summary>
		/// https://msdn.microsoft.com/en-us/library/windows/desktop/ms686769(v=vs.85).aspx
		/// </summary>
		public enum ThreadAccess : int
		{
			SUSPEND_RESUME = (0x0002),
		}

		/// <summary>
		/// https://docs.microsoft.com/en-us/windows/desktop/api/winbase/nf-winbase-createsymboliclinkw#parameters
		/// </summary>
		[DllImport("kernel32.dll", SetLastError = true)]
		public static extern bool CreateSymbolicLink(string lpSymlinkFileName, string lpTargetFileName, SymbolicLink dwFlags);

		/// <summary>
		/// https://msdn.microsoft.com/en-us/library/windows/desktop/ms684335(v=vs.85).aspx
		/// </summary>
		[DllImport("kernel32.dll")]
		public static extern IntPtr OpenThread(ThreadAccess dwDesiredAccess, bool bInheritHandle, uint dwThreadId);

		/// <summary>
		/// https://msdn.microsoft.com/en-us/library/windows/desktop/ms724211(v=vs.85).aspx
		/// </summary>
		[DllImport("kernel32.dll")]
		public static extern bool CloseHandle(IntPtr hObject);

		/// <summary>
		/// https://msdn.microsoft.com/en-us/library/windows/desktop/ms686345(v=vs.85).aspx
		/// </summary>
		[DllImport("kernel32.dll")]
		public static extern uint SuspendThread(IntPtr hThread);

		/// <summary>
		/// https://msdn.microsoft.com/en-us/library/windows/desktop/ms685086(v=vs.85).aspx
		/// </summary>
		[DllImport("kernel32.dll")]
		public static extern int ResumeThread(IntPtr hThread);
	}
}
