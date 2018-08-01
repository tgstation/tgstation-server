using Microsoft.Win32.SafeHandles;
using System;
using System.ComponentModel;
using System.DirectoryServices.AccountManagement;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Host.Models;

namespace Tgstation.Server.Host.Security
{
	/// <summary>
	/// <see cref="ISystemIdentityFactory"/> for windows systems. Uses long running tasks due to potential networked domains
	/// </summary>
	sealed class WindowsSystemIdentityFactory : ISystemIdentityFactory
	{
		/// <inheritdoc />
		public Task<ISystemIdentity> CreateSystemIdentity(User user, CancellationToken cancellationToken) => Task.Factory.StartNew(() =>
		{
			if (user == null)
				throw new ArgumentNullException(nameof(user));

			if (user.SystemIdentifier == null)
				throw new InvalidOperationException("User's SystemIdentifier must not be null!");

			PrincipalContext pc = null;
			UserPrincipal principal = null;
			//machine logon first cause it's faster
			try
			{
				pc = new PrincipalContext(ContextType.Machine, Environment.UserDomainName);
				principal = UserPrincipal.FindByIdentity(pc, user.SystemIdentifier);

				if (principal == null)
				{
					pc.Dispose();
					pc = new PrincipalContext(ContextType.Domain, Environment.UserDomainName);
					principal = UserPrincipal.FindByIdentity(pc, user.SystemIdentifier);
					if (principal == null)
						pc.Dispose();
				}
			}
			catch
			{
				pc?.Dispose();
				throw;
			}

			if (principal == null)
				return null;

			return (ISystemIdentity)new WindowsSystemIdentity(principal);
		}, cancellationToken, TaskCreationOptions.LongRunning, TaskScheduler.Current);

		/// <inheritdoc />
		public Task<ISystemIdentity> CreateSystemIdentity(string username, string password, CancellationToken cancellationToken) => Task.Factory.StartNew(() =>
		{
			if (username == null)
				throw new ArgumentNullException(nameof(username));
			if (password == null)
				throw new ArgumentNullException(nameof(password));
			var splits = username.Split('\\');

			var res = NativeMethods.LogonUser(splits.Length > 1 ? splits[1] : splits[0], splits.Length > 1 ? splits[0] : null, password, 3 /*LOGON32_LOGON_NETWORK*/, 0 /*LOGON32_PROVIDER_DEFAULT*/, out var token);
			if (!res)
				return null;

			using (var handle = new SafeAccessTokenHandle(token))	//checked internally, windows identity always duplicates the handle when constructed with a userToken
				return (ISystemIdentity)new WindowsSystemIdentity(new WindowsIdentity(handle.DangerousGetHandle()));   //https://github.com/dotnet/corefx/blob/6ed61acebe3214fcf79b4274f2bb9b55c0604a4d/src/System.Security.Principal.Windows/src/System/Security/Principal/WindowsIdentity.cs#L271
		}, cancellationToken, TaskCreationOptions.LongRunning, TaskScheduler.Current);
	}
}
