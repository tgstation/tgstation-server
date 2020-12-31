using Mono.Unix;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Tgstation.Server.Host.IO
{
	/// <summary>
	/// <see cref="ISymlinkFactory"/> for posix systems
	/// </summary>
	sealed class PosixSymlinkFactory : ISymlinkFactory
	{
		/// <inheritdoc />
		public Task CreateSymbolicLink(string targetPath, string linkPath, CancellationToken cancellationToken) => Task.Factory.StartNew(() =>
		{
			if (targetPath == null)
				throw new ArgumentNullException(nameof(targetPath));
			if (linkPath == null)
				throw new ArgumentNullException(nameof(linkPath));

			UnixFileSystemInfo fsInfo;
			var isFile = File.Exists(targetPath);
			cancellationToken.ThrowIfCancellationRequested();
			if (isFile)
				fsInfo = new UnixFileInfo(targetPath);
			else
				fsInfo = new UnixDirectoryInfo(targetPath);
			cancellationToken.ThrowIfCancellationRequested();
			fsInfo.CreateSymbolicLink(linkPath);
		}, cancellationToken, DefaultIOManager.BlockingTaskCreationOptions, TaskScheduler.Current);
	}
}
