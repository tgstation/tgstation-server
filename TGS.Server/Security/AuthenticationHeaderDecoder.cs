using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.IdentityModel.Claims;
using System.IdentityModel.Policy;
using System.Runtime.InteropServices;
using System.ServiceModel;
using System.ServiceModel.Channels;

namespace TGS.Server.Security
{
	sealed class AuthenticationHeaderDecoder : ServiceAuthenticationManager
	{
		[DllImport("advapi32.dll", SetLastError = true)]
		static extern bool LogonUser(string lpszUsername, string lpszDomain, string lpszPassword, int dwLogonType, int dwLogonProvider, out IntPtr phToken);
		[DllImport("kernel32.dll", SetLastError = true)]
		static extern bool CloseHandle(IntPtr handle);

		public ClaimSet Issuer => throw new NotImplementedException();

		public string Id => throw new NotImplementedException();

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
						//IMPORTANT: logoff the user after all is said and done or they'll stay logged in on the system for as long as we are
						OperationContext.Current.OperationCompleted += (a, b) => { CloseHandle(token); };
						return new ReadOnlyCollection<IAuthorizationPolicy>(new List<IAuthorizationPolicy> { new WindowsAuthorizationPolicy(token) });
					}
				}
			}
			catch { }
			return authPolicy;
		}
	}
}
