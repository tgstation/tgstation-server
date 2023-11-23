using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Mono.Unix;

#nullable disable

namespace Tgstation.Server.Host.IO
{
	/// <summary>
	/// <see cref="IFilesystemLinkFactory"/> for POSIX systems.
	/// </summary>
	sealed class PosixFilesystemLinkFactory : IFilesystemLinkFactory
	{
		/// <inheritdoc />
		public bool SymlinkedDirectoriesAreDeletedAsFiles => true;

		/// <inheritdoc />
		public Task CreateHardLink(string targetPath, string linkPath, CancellationToken cancellationToken) => Task.Factory.StartNew(
			() =>
			{
				ArgumentNullException.ThrowIfNull(targetPath);
				ArgumentNullException.ThrowIfNull(linkPath);

				cancellationToken.ThrowIfCancellationRequested();
				var fsInfo = new UnixFileInfo(targetPath);
				cancellationToken.ThrowIfCancellationRequested();
				fsInfo.CreateLink(linkPath);
			},
			cancellationToken,
			DefaultIOManager.BlockingTaskCreationOptions,
			TaskScheduler.Current);

		/// <inheritdoc />
		public Task CreateSymbolicLink(string targetPath, string linkPath, CancellationToken cancellationToken) => Task.Factory.StartNew(
			() =>
			{
				ArgumentNullException.ThrowIfNull(targetPath);
				ArgumentNullException.ThrowIfNull(linkPath);

				UnixFileSystemInfo fsInfo;
				var isFile = File.Exists(targetPath);
				cancellationToken.ThrowIfCancellationRequested();
				if (isFile)
					fsInfo = new UnixFileInfo(targetPath);
				else
					fsInfo = new UnixDirectoryInfo(targetPath);
				cancellationToken.ThrowIfCancellationRequested();
				fsInfo.CreateSymbolicLink(linkPath);
			},
			cancellationToken,
			DefaultIOManager.BlockingTaskCreationOptions,
			TaskScheduler.Current);
	}
}
