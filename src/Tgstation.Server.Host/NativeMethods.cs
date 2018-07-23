using System;
using System.Runtime.InteropServices;

namespace Tgstation.Server.Host
{
	/// <summary>
	/// Native methods used by the code
	/// </summary>
	static class NativeMethods
	{
		/// <summary>
		/// See https://msdn.microsoft.com/en-us/library/windows/desktop/aa378184(v=vs.85).aspx
		/// </summary>
		[DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
		public static extern bool LogonUser(string lpszUsername, string lpszDomain, string lpszPassword, int dwLogonType, int dwLogonProvider, out IntPtr phToken);

		/// <summary>
		/// See https://docs.microsoft.com/en-us/windows/desktop/api/winbase/nf-winbase-createsymboliclinkw
		/// </summary>
		[DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
		public static extern bool CreateSymbolicLink(string lpSymlinkFileName, string lpTargetFileName, int dwFlags);
	}
}
