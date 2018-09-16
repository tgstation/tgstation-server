using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.IdentityModel.Policy;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.ServiceModel;
using System.ServiceModel.Channels;

namespace TGS.Server.Security
{
	sealed class AuthenticationHeaderDecoder : ServiceAuthenticationManager
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

		/// <summary>
		/// Authenticates a given <see cref="Message"/>
		/// </summary>
		/// <param name="authPolicy">The default <see cref="IAuthorizationPolicy"/>s</param>
		/// <param name="listenUri">The <see cref="Uri"/> at which the message was received.</param>
		/// <param name="message">The <see cref="Message"/> to be authenticated</param>
		/// <returns>A <see cref="ReadOnlyCollection{T}"/> of <see cref="IAuthorizationPolicy"/>s. Either <paramref name="authPolicy"/> or one containing a single <see cref="WindowsAuthorizationPolicy"/> if this function successfully creates one</returns>
		public override ReadOnlyCollection<IAuthorizationPolicy> Authenticate(ReadOnlyCollection<IAuthorizationPolicy> authPolicy, Uri listenUri, ref Message message)
		{
			try
			{
				var userPosition = message.Headers.FindHeader("Username", "http://tempuri.org");
				var passPosition = message.Headers.FindHeader("Password", "http://tempuri.org");

				if (userPosition != -1 && passPosition != -1)
				{
					var user = message.Headers.GetHeader<string>(userPosition);
					var pass = message.Headers.GetHeader<string>(passPosition);

					var splits = user.Split('\\');

					var res = LogonUser(splits.Length > 1 ? splits[1] : splits[0], splits.Length > 1 ? splits[0] : null, pass, 3 /*LOGON32_LOGON_NETWORK*/, 0 /*LOGON32_PROVIDER_DEFAULT*/, out IntPtr token);
					if (res)
					{
						//IMPORTANT: logoff the user after all is said and done or they'll stay logged in on the system for as long as we are running
						OperationContext.Current.OperationCompleted += (a, b) => { CloseHandle(token); };
						return new ReadOnlyCollection<IAuthorizationPolicy>(new List<IAuthorizationPolicy> { new WindowsAuthorizationPolicy(new WindowsIdentity(token)) });
					}
				}
			}
			catch { }
			return new ReadOnlyCollection<IAuthorizationPolicy>(new List<IAuthorizationPolicy>());
		}
	}
}
