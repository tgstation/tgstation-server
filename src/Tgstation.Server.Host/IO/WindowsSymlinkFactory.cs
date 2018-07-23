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
			//check if its not a file
			var flags = File.Exists(targetPath) ? 0 : 1; //SYMBOLIC_LINK_FLAG_DIRECTORY
			flags |= 2; //SYMBOLIC_LINK_FLAG_ALLOW_UNPRIVILEGED_CREATE on win10 1607+

			if (!NativeMethods.CreateSymbolicLink(linkPath, targetPath, flags))
				throw new Win32Exception(Marshal.GetLastWin32Error());
		}, cancellationToken, TaskCreationOptions.LongRunning, TaskScheduler.Current);
	}
}
