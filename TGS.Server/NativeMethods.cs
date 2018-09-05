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


		[Flags]
		public enum Option : uint
		{
			Normal = 0x00000000,
			WithDataSegs = 0x00000001,
			WithFullMemory = 0x00000002,
			WithHandleData = 0x00000004,
			FilterMemory = 0x00000008,
			ScanMemory = 0x00000010,
			WithUnloadedModules = 0x00000020,
			WithIndirectlyReferencedMemory = 0x00000040,
			FilterModulePaths = 0x00000080,
			WithProcessThreadData = 0x00000100,
			WithPrivateReadWriteMemory = 0x00000200,
			WithoutOptionalData = 0x00000400,
			WithFullMemoryInfo = 0x00000800,
			WithThreadInfo = 0x00001000,
			WithCodeSegs = 0x00002000,
			WithoutAuxiliaryState = 0x00004000,
			WithFullAuxiliaryState = 0x00008000,
			WithPrivateWriteCopyMemory = 0x00010000,
			IgnoreInaccessibleMemory = 0x00020000,
			ValidTypeFlags = 0x0003ffff
		};

		public enum ExceptionInfo
		{
			None,
			Present
		}


		[StructLayout(LayoutKind.Sequential, Pack = 4)]  // Pack=4 is important! So it works also for x64!
		public struct MiniDumpExceptionInformation
		{
			public uint ThreadId;

			public IntPtr ExceptionPointers;

			[MarshalAs(UnmanagedType.Bool)]
			public bool ClientPointers;
		}

		// Overload requiring MiniDumpExceptionInformation
		[DllImport("dbghelp.dll", EntryPoint = "MiniDumpWriteDump", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
		static extern bool MiniDumpWriteDump(IntPtr hProcess, uint processId, SafeHandle hFile, uint dumpType, ref MiniDumpExceptionInformation expParam, IntPtr userStreamParam, IntPtr callbackParam);
		
		// Overload supporting MiniDumpExceptionInformation == NULL
		[DllImport("dbghelp.dll", EntryPoint = "MiniDumpWriteDump", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
		static extern bool MiniDumpWriteDump(IntPtr hProcess, uint processId, SafeHandle hFile, uint dumpType, IntPtr expParam, IntPtr userStreamParam, IntPtr callbackParam);

		[DllImport("kernel32.dll", EntryPoint = "GetCurrentThreadId", ExactSpelling = true)]
		static extern uint GetCurrentThreadId();
	}
}
