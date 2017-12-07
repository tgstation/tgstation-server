using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.IdentityModel.Policy;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.ServiceModel;
using System.Threading;

namespace TGS.Server.Security
{
	sealed class IdentityManager
	{
		/// <summary>
		/// See https://msdn.microsoft.com/en-us/library/windows/desktop/aa378184(v=vs.85).aspx
		/// </summary>
		[DllImport("advapi32.dll", SetLastError = true)]
		static extern bool LogonUser(string lpszUsername, string lpszDomain, string lpszPassword, int dwLogonType, int dwLogonProvider, out IntPtr phToken);
		/// <summary>
		/// See https://msdn.microsoft.com/en-us/library/windows/desktop/ms724211(v=vs.85).aspx
		/// </summary>
		[DllImport("kernel32.dll", SetLastError = true)]
		static extern bool CloseHandle(IntPtr handle);
		
		static readonly ThreadLocal<WindowsIdentity> threadLocalIdentity = new ThreadLocal<WindowsIdentity>();

		public static WindowsImpersonationContext Impersonate()
		{
			return threadLocalIdentity.Value.Impersonate();
		}
		
		public static WindowsIdentity SetIdentityForOperation(string user, string password)
		{
			if (user != null && password != null)
			{
				var splits = user.Split('\\');

				var res = LogonUser(splits.Length > 1 ? splits[1] : splits[0], splits.Length > 1 ? splits[0] : null, password, 3 /*LOGON32_LOGON_NETWORK*/, 0 /*LOGON32_PROVIDER_DEFAULT*/, out IntPtr token);
				if (res)
				{
					//IMPORTANT: logoff the user after all is said and done or they'll stay logged in on the system for as long as we are running
					OperationContext.Current.OperationCompleted += (a, b) => { CloseHandle(token); };
					threadLocalIdentity.Value = new WindowsIdentity(token);
					return threadLocalIdentity.Value;
				}
			}
			threadLocalIdentity.Value = OperationContext.Current.ServiceSecurityContext.WindowsIdentity;
			return (WindowsIdentity)threadLocalIdentity.Value.Clone();
		}
	}
}
