using Mono.Unix;
using Mono.Unix.Native;
using System;

namespace Tgstation.Server.Host.IO
{
	/// <summary>
	/// <see cref="IPostWriteHandler"/> for POSIX systems
	/// </summary>
	sealed class PosixPostWriteHandler : IPostWriteHandler
	{
		/// <inheritdoc />
		public void HandleWrite(string filePath)
		{
			if (filePath == null)
				throw new ArgumentNullException(nameof(filePath));

			//set executable bit every time, don't want people calling me when their uploaded "sl" binary doesn't work
			if (Syscall.stat(filePath, out var stat) != 0)
				throw new UnixIOException(Stdlib.GetLastError());

			if (stat.st_mode.HasFlag(FilePermissions.S_IXUSR))
				return;

			if (Syscall.chmod(filePath, stat.st_mode | FilePermissions.S_IXUSR) != 0)
				throw new UnixIOException(Stdlib.GetLastError());
		}
	}
}
