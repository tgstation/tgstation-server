using System;

using Microsoft.Extensions.Logging;
using Mono.Unix;
using Mono.Unix.Native;

namespace Tgstation.Server.Host.IO
{
	/// <summary>
	/// <see cref="IPostWriteHandler"/> for POSIX systems.
	/// </summary>
	sealed class PosixPostWriteHandler : IPostWriteHandler
	{
		/// <summary>
		/// The <see cref="ILogger{TCategoryName}"/> for the <see cref="PosixPostWriteHandler"/>.
		/// </summary>
		readonly ILogger<PosixPostWriteHandler> logger;

		/// <summary>
		/// Initializes a new instance of the <see cref="PosixPostWriteHandler"/> class.
		/// </summary>
		/// <param name="logger">The value of <see cref="logger"/>.</param>
		public PosixPostWriteHandler(ILogger<PosixPostWriteHandler> logger)
		{
			this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
		}

		/// <inheritdoc />
		public bool NeedsPostWrite(string sourceFilePath)
		{
			if (sourceFilePath == null)
				throw new ArgumentNullException(nameof(sourceFilePath));

			if (Syscall.stat(sourceFilePath, out var stat) != 0)
				throw new UnixIOException(Stdlib.GetLastError());

			return stat.st_mode.HasFlag(FilePermissions.S_IXUSR);
		}

		/// <inheritdoc />
		public void HandleWrite(string filePath)
		{
			if (filePath == null)
				throw new ArgumentNullException(nameof(filePath));

			// set executable bit every time, don't want people calling me when their uploaded "sl" binary doesn't work
			if (Syscall.stat(filePath, out var stat) != 0)
				throw new UnixIOException(Stdlib.GetLastError());

			if (stat.st_mode.HasFlag(FilePermissions.S_IXUSR))
			{
				logger.LogTrace("{0} already +x", filePath);
				return;
			}

			logger.LogTrace("Setting +x on {0}", filePath);
			if (Syscall.chmod(filePath, stat.st_mode | FilePermissions.S_IXUSR) != 0)
				throw new UnixIOException(Stdlib.GetLastError());
		}
	}
}
