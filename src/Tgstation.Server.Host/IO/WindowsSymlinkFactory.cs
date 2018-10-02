using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Tgstation.Server.Host.IO
{
	/// <summary>
	/// <see cref="ISymlinkFactory"/> for windows systems
	/// </summary>
	sealed class WindowsSymlinkFactory : ISymlinkFactory
	{
		/// <inheritdoc />
		public Task CreateSymbolicLink(string targetPath, string linkPath, CancellationToken cancellationToken) => Task.Factory.StartNew(() =>
		{
			if (targetPath == null)
				throw new ArgumentNullException(nameof(targetPath));
			if (linkPath == null)
				throw new ArgumentNullException(nameof(linkPath));

			//check if its not a file
			var flags = File.Exists(targetPath) ? NativeMethods.CreateSymbolicLinkFlags.None : NativeMethods.CreateSymbolicLinkFlags.Directory;

			flags |= NativeMethods.CreateSymbolicLinkFlags.AllowUnprivilegedCreate;

			cancellationToken.ThrowIfCancellationRequested();
			if (!NativeMethods.CreateSymbolicLink(linkPath, targetPath, flags))
			{
				var error = Marshal.GetLastWin32Error();
				if (error == 87) //INVALID_PARAMETER, SYMBOLIC_LINK_FLAG_ALLOW_UNPRIVILEGED_CREATE isn't supported
				{
					flags &= ~NativeMethods.CreateSymbolicLinkFlags.AllowUnprivilegedCreate;
					if (NativeMethods.CreateSymbolicLink(linkPath, targetPath, flags))
						return;
					error = Marshal.GetLastWin32Error();
				}
				throw new Win32Exception(error);
			}
		}, cancellationToken, TaskCreationOptions.LongRunning, TaskScheduler.Current);
	}
}
