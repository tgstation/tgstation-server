using Microsoft.Win32.SafeHandles;
using System;
using System.ComponentModel;
using System.Globalization;
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

			//System identity at this point will always be in the form DOMAIN\\USER or USER
			var splits = user.SystemIdentifier.Split('\\');
			string identity;
			if (splits.Length > 1)
				identity = String.Format(CultureInfo.InvariantCulture, "{0}@{1}", splits[0], splits[1]);
			else
				identity = user.SystemIdentifier;

			return (ISystemIdentity)new WindowsSystemIdentity(new WindowsIdentity(identity));
		}, cancellationToken, TaskCreationOptions.LongRunning, TaskScheduler.Current);

		/// <inheritdoc />
		public Task<ISystemIdentity> CreateSystemIdentity(string username, string password, CancellationToken cancellationToken) => Task.Factory.StartNew(() =>
		{
			var splits = username.Split('\\');

			var res = NativeMethods.LogonUser(splits.Length > 1 ? splits[1] : splits[0], splits.Length > 1 ? splits[0] : null, password, 3 /*LOGON32_LOGON_NETWORK*/, 0 /*LOGON32_PROVIDER_DEFAULT*/, out var token);
			if (!res)
				throw new Win32Exception(Marshal.GetLastWin32Error());

			using (var handle = new SafeAccessTokenHandle(token))	//checked internally, windows identity always duplicates the handle when constructed with a userToken
				return (ISystemIdentity)new WindowsSystemIdentity(new WindowsIdentity(handle.DangerousGetHandle()));   //https://github.com/dotnet/corefx/blob/6ed61acebe3214fcf79b4274f2bb9b55c0604a4d/src/System.Security.Principal.Windows/src/System/Security/Principal/WindowsIdentity.cs#L271
		}, cancellationToken, TaskCreationOptions.LongRunning, TaskScheduler.Current);
	}
}
