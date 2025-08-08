using System;
using System.IO.Abstractions;
using System.Threading;
using System.Threading.Tasks;

using Mono.Unix;

namespace Tgstation.Server.Host.IO
{
	/// <summary>
	/// <see cref="IFilesystemLinkFactory"/> for POSIX systems.
	/// </summary>
	sealed class PosixFilesystemLinkFactory : IFilesystemLinkFactory
	{
		/// <inheritdoc />
		public bool SymlinkedDirectoriesAreDeletedAsFiles => true;

		/// <summary>
		/// The <see cref="IFileSystem"/> to use.
		/// </summary>
		readonly IFileSystem fileSystem;

		/// <summary>
		/// Initializes a new instance of the <see cref="PosixFilesystemLinkFactory"/> class.
		/// </summary>
		/// <param name="fileSystem">The value of <see cref="fileSystem"/>.</param>
		public PosixFilesystemLinkFactory(IFileSystem fileSystem)
		{
			this.fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
		}

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
				var isFile = fileSystem.File.Exists(targetPath);
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
