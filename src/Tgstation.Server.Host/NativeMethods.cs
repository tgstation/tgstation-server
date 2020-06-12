using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Tgstation.Server.Host
{
	/// <summary>
	/// Native Windows methods used by the code.
	/// </summary>
	#pragma warning disable SA1600
	#pragma warning disable SA1602
	#pragma warning disable SA1611
	#pragma warning disable SA1615
	static class NativeMethods
	{
		/// <summary>
		/// See https://docs.microsoft.com/en-us/windows/desktop/api/winbase/nf-winbase-createsymboliclinka#parameters
		/// </summary>
		[Flags]
		public enum CreateSymbolicLinkFlags : int
		{
			None = 0,
			Directory = 1,
			AllowUnprivilegedCreate = 2
		}

		/// <summary>
		/// See https://msdn.microsoft.com/en-us/library/windows/desktop/ms686769(v=vs.85).aspx
		/// </summary>
		public enum ThreadAccess : int
		{
			SuspendResume = 0x0002,
		}

		/// <summary>
		/// See https://docs.microsoft.com/en-us/windows/win32/api/minidumpapiset/ne-minidumpapiset-minidump_type
		/// </summary>
		[Flags]
		public enum MiniDumpType : uint
		{
			Normal = 0x00000000
		}

		/// <summary>
		/// See https://docs.microsoft.com/en-us/windows/desktop/api/winuser/nf-winuser-getwindowthreadprocessid
		/// </summary>
		[DllImport("user32.dll")]
		public static extern int GetWindowThreadProcessId(IntPtr hWnd, out int lpdwProcessId);

		/// <summary>
		/// See https://docs.microsoft.com/en-us/windows/desktop/api/winuser/nf-winuser-findwindoww
		/// </summary>
		[DllImport("user32.dll", CharSet = CharSet.Unicode)]
		public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

		/// <summary>
		/// See https://docs.microsoft.com/en-us/windows/desktop/api/winuser/nf-winuser-sendmessage
		/// </summary>
		[DllImport("user32.dll")]
		public static extern int SendMessage(IntPtr hWnd, int wMsg, IntPtr wParam, IntPtr lParam);

		/// <summary>
		/// See https://msdn.microsoft.com/en-us/library/ms633493(v=VS.85).aspx
		/// </summary>
		public delegate bool EnumWindowProc(IntPtr hwnd, IntPtr lParam);

		/// <summary>
		/// See https://docs.microsoft.com/en-us/windows/desktop/api/winuser/nf-winuser-enumchildwindows
		/// </summary>
		[DllImport("user32.dll")]
		public static extern bool EnumChildWindows(IntPtr window, EnumWindowProc callback, IntPtr lParam);

		/// <summary>
		/// See https://docs.microsoft.com/en-us/windows/desktop/api/winuser/nf-winuser-getwindowtextw
		/// </summary>
		[DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		public static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

		/// <summary>
		/// See https://msdn.microsoft.com/en-us/library/windows/desktop/aa378184(v=vs.85).aspx
		/// </summary>
		[DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
		public static extern bool LogonUser(string lpszUsername, string lpszDomain, string lpszPassword, int dwLogonType, int dwLogonProvider, out IntPtr phToken);

		/// <summary>
		/// See https://docs.microsoft.com/en-us/windows/desktop/api/winbase/nf-winbase-createsymboliclinkw
		/// </summary>
		[DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
		public static extern bool CreateSymbolicLink(string lpSymlinkFileName, string lpTargetFileName, CreateSymbolicLinkFlags dwFlags);

		/// <summary>
		/// See https://msdn.microsoft.com/en-us/library/windows/desktop/ms684335(v=vs.85).aspx
		/// </summary>
		[DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
		public static extern IntPtr OpenThread(ThreadAccess dwDesiredAccess, bool bInheritHandle, uint dwThreadId);

		/// <summary>
		/// See https://msdn.microsoft.com/en-us/library/windows/desktop/ms724211(v=vs.85).aspx
		/// </summary>
		[DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
		public static extern bool CloseHandle(IntPtr hObject);

		/// <summary>
		/// See https://msdn.microsoft.com/en-us/library/windows/desktop/ms686345(v=vs.85).aspx
		/// </summary>
		[DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
		public static extern uint SuspendThread(IntPtr hThread);

		/// <summary>
		/// See https://msdn.microsoft.com/en-us/library/windows/desktop/ms685086(v=vs.85).aspx
		/// </summary>
		[DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
		public static extern uint ResumeThread(IntPtr hThread);

		/// <summary>
		/// See https://docs.microsoft.com/en-us/windows/win32/api/minidumpapiset/nf-minidumpapiset-minidumpwritedump
		/// </summary>
		[DllImport("dbghelp.dll", SetLastError = true, CharSet = CharSet.Unicode)]
		public static extern bool MiniDumpWriteDump(
			IntPtr hProcess,
			uint processId,
			SafeHandle hFile,
			MiniDumpType dumpType,
			IntPtr expParam,
			IntPtr userStreamParam,
			IntPtr callbackParam);
	}
}
