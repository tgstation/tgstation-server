using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Tgstation.Server.Host.Utils;

namespace Tgstation.Server.Host.IO
{
	/// <summary>
	/// <see cref="IIOManager"/> that resolves paths to <see cref="Environment.CurrentDirectory"/>.
	/// </summary>
	class DefaultIOManager : IIOManager
	{
		/// <summary>
		/// Path to the current working directory for the <see cref="IIOManager"/>.
		/// </summary>
		public const string CurrentDirectory = ".";

		/// <summary>
		/// Default <see cref="FileStream"/> buffer size used by .NET.
		/// </summary>
		public const int DefaultBufferSize = 4096;

		/// <summary>
		/// The <see cref="TaskCreationOptions"/> used to spawn <see cref="Task"/>s for potentially long running, blocking operations.
		/// </summary>
		public const TaskCreationOptions BlockingTaskCreationOptions = TaskCreationOptions.None;

		/// <inheritdoc />
		public char DirectorySeparatorChar => fileSystem.Path.DirectorySeparatorChar;

		/// <inheritdoc />
		public char AltDirectorySeparatorChar => fileSystem.Path.AltDirectorySeparatorChar;

		/// <summary>
		/// The backing <see cref="IFileSystem"/>.
		/// </summary>
		readonly IFileSystem fileSystem;

		/// <summary>
		/// Initializes a new instance of the <see cref="DefaultIOManager"/> class.
		/// </summary>
		/// <param name="fileSystem">The value of <see cref="fileSystem"/>.</param>
		public DefaultIOManager(IFileSystem fileSystem)
		{
			this.fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
		}

		/// <summary>
		/// Recursively empty a directory.
		/// </summary>
		/// <param name="dir"><see cref="DirectoryInfo"/> of the directory to empty.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		static void NormalizeAndDelete(IDirectoryInfo dir, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();

			// check if we are a symbolic link
			if (!dir.Attributes.HasFlag(FileAttributes.Directory) || dir.Attributes.HasFlag(FileAttributes.ReparsePoint))
			{
				dir.Delete();
				return;
			}

			List<Exception>? exceptions = null;
			foreach (var subDir in dir.EnumerateDirectories())
				try
				{
					NormalizeAndDelete(subDir, cancellationToken);
				}
				catch (AggregateException ex)
				{
					exceptions ??= new List<Exception>();
					exceptions.AddRange(ex.InnerExceptions);
				}

			foreach (var file in dir.EnumerateFiles())
			{
				cancellationToken.ThrowIfCancellationRequested();
				try
				{
					file.Attributes = FileAttributes.Normal;
					file.Delete();
				}
				catch (Exception ex)
				{
					exceptions ??= new List<Exception>();
					exceptions.Add(ex);
				}
			}

			cancellationToken.ThrowIfCancellationRequested();
			try
			{
				dir.Delete(true);
			}
			catch (Exception ex)
			{
				exceptions ??= new List<Exception>();
				exceptions.Add(ex);
			}

			if (exceptions != null)
				throw new AggregateException(exceptions);
		}

		/// <inheritdoc />
		public async ValueTask CopyDirectory(
			IEnumerable<string>? ignore,
			Func<string, string, ValueTask>? postCopyCallback,
			string src,
			string dest,
			int? taskThrottle,
			CancellationToken cancellationToken)
		{
			ArgumentNullException.ThrowIfNull(src);
			ArgumentNullException.ThrowIfNull(src);

			if (taskThrottle.HasValue && taskThrottle < 1)
				throw new ArgumentOutOfRangeException(nameof(taskThrottle), taskThrottle, "taskThrottle must be at least 1!");

			src = ResolvePath(src);
			dest = ResolvePath(dest);

			using var semaphore = taskThrottle.HasValue ? new SemaphoreSlim(taskThrottle.Value) : null;
			await Task.WhenAll(CopyDirectoryImpl(src, dest, ignore, postCopyCallback, semaphore, cancellationToken));
		}

		/// <inheritdoc />
		public string ConcatPath(params string[] paths) => fileSystem.Path.Combine(paths);

		/// <inheritdoc />
		public async ValueTask CopyFile(string src, string dest, CancellationToken cancellationToken)
		{
			ArgumentNullException.ThrowIfNull(src);
			ArgumentNullException.ThrowIfNull(dest);

			// tested to hell and back, these are the optimal buffer sizes
			await using var srcStream = fileSystem.FileStream.New(
				ResolvePath(src),
				FileMode.Open,
				FileAccess.Read,
				FileShare.Read | FileShare.Delete,
				DefaultBufferSize,
				FileOptions.Asynchronous | FileOptions.SequentialScan);
			await using var destStream = CreateAsyncSequentialWriteStream(dest);

			// value taken from documentation
			await srcStream.CopyToAsync(destStream, 81920, cancellationToken);
		}

		/// <inheritdoc />
		public Task CreateDirectory(string path, CancellationToken cancellationToken) => Task.Factory.StartNew(() => fileSystem.Directory.CreateDirectory(ResolvePath(path)), cancellationToken, BlockingTaskCreationOptions, TaskScheduler.Current);

		/// <inheritdoc />
		public Task DeleteDirectory(string path, CancellationToken cancellationToken)
			=> Task.Factory.StartNew(
				() =>
				{
					var di = fileSystem.DirectoryInfo.New(path);
					if (di.Exists)
						NormalizeAndDelete(di, cancellationToken);
				},
				cancellationToken,
				BlockingTaskCreationOptions,
				TaskScheduler.Current);

		/// <inheritdoc />
		public Task DeleteFile(string path, CancellationToken cancellationToken) => Task.Factory.StartNew(() => fileSystem.File.Delete(ResolvePath(path)), cancellationToken, BlockingTaskCreationOptions, TaskScheduler.Current);

		/// <inheritdoc />
		public Task<bool> FileExists(string path, CancellationToken cancellationToken) => Task.Factory.StartNew(() => fileSystem.File.Exists(ResolvePath(path)), cancellationToken, BlockingTaskCreationOptions, TaskScheduler.Current);

		/// <inheritdoc />
		public Task<bool> DirectoryExists(string path, CancellationToken cancellationToken) => Task.Factory.StartNew(() => fileSystem.Directory.Exists(ResolvePath(path)), cancellationToken, BlockingTaskCreationOptions, TaskScheduler.Current);

		/// <inheritdoc />
		public string GetDirectoryName(string path) => fileSystem.Path.GetDirectoryName(path ?? throw new ArgumentNullException(nameof(path)))
			?? throw new InvalidOperationException($"Null was returned. Path ({path}) must be rooted. This is not supported!");

		/// <inheritdoc />
		public string GetFileName(string path) => fileSystem.Path.GetFileName(path ?? throw new ArgumentNullException(nameof(path)));

		/// <inheritdoc />
		public string GetFileNameWithoutExtension(string path) => fileSystem.Path.GetFileNameWithoutExtension(path ?? throw new ArgumentNullException(nameof(path)));

		/// <inheritdoc />
		public Task<List<string>> GetFilesWithExtension(string path, string extension, bool recursive, CancellationToken cancellationToken) => Task.Factory.StartNew(
			() =>
			{
				path = ResolvePath(path);
				ArgumentNullException.ThrowIfNull(extension);
				var results = new List<string>();
				foreach (var fileName in fileSystem.Directory.EnumerateFiles(
					path,
					$"*.{extension}",
					recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly))
				{
					cancellationToken.ThrowIfCancellationRequested();
					results.Add(fileName);
				}

				return results;
			},
			cancellationToken,
			BlockingTaskCreationOptions,
			TaskScheduler.Current);

		/// <inheritdoc />
		public Task MoveFile(string source, string destination, CancellationToken cancellationToken) => Task.Factory.StartNew(
			() =>
			{
				ArgumentNullException.ThrowIfNull(destination);
				source = ResolvePath(source ?? throw new ArgumentNullException(nameof(source)));
				destination = ResolvePath(destination);
				fileSystem.File.Move(source, destination);
			},
			cancellationToken,
			BlockingTaskCreationOptions,
			TaskScheduler.Current);

		/// <inheritdoc />
		public Task MoveDirectory(string source, string destination, CancellationToken cancellationToken) => Task.Factory.StartNew(
			() =>
			{
				ArgumentNullException.ThrowIfNull(destination);
				source = ResolvePath(source ?? throw new ArgumentNullException(nameof(source)));
				destination = ResolvePath(destination);
				fileSystem.Directory.Move(source, destination);
			},
			cancellationToken,
			BlockingTaskCreationOptions,
			TaskScheduler.Current);

		/// <inheritdoc />
		public async ValueTask<byte[]> ReadAllBytes(string path, CancellationToken cancellationToken)
		{
			await using var file = CreateAsyncReadStream(path, true, true);
			byte[] buf;
			buf = new byte[file.Length];
			await file.ReadAsync(buf, cancellationToken);
			return buf;
		}

		/// <inheritdoc />
		public string ResolvePath()
			=> ResolvePath(CurrentDirectory);

		/// <inheritdoc />
		public virtual string ResolvePath(string path)
			=> fileSystem.Path.GetFullPath(path ?? throw new ArgumentNullException(nameof(path)));

		/// <inheritdoc />
		public async ValueTask WriteAllBytes(string path, byte[] contents, CancellationToken cancellationToken)
		{
			await using var file = CreateAsyncSequentialWriteStream(path);
			await file.WriteAsync(contents, cancellationToken);
		}

		/// <inheritdoc />
		public Stream CreateAsyncSequentialWriteStream(string path)
		{
			path = ResolvePath(path);
			return fileSystem.FileStream.New(
				path,
				FileMode.Create,
				FileAccess.Write,
				FileShare.Read | FileShare.Delete,
				DefaultBufferSize,
				FileOptions.Asynchronous | FileOptions.SequentialScan);
		}

		/// <inheritdoc />
		public Stream CreateAsyncReadStream(string path, bool sequental, bool shareWrite)
		{
			path = ResolvePath(path);
			return fileSystem.FileStream.New(
				path,
				FileMode.Open,
				FileAccess.Read,
				FileShare.ReadWrite | FileShare.Delete | (shareWrite ? FileShare.Write : FileShare.None),
				DefaultBufferSize,
				sequental
					? FileOptions.Asynchronous | FileOptions.SequentialScan
					: FileOptions.Asynchronous);
		}

		/// <inheritdoc />
		public Task<IReadOnlyList<string>> GetDirectories(string path, CancellationToken cancellationToken) => Task.Factory.StartNew(
			() =>
			{
				path = ResolvePath(path);
				var results = new List<string>();
				cancellationToken.ThrowIfCancellationRequested();
				foreach (var directoryName in fileSystem.Directory.EnumerateDirectories(path))
				{
					results.Add(directoryName);
					cancellationToken.ThrowIfCancellationRequested();
				}

				return (IReadOnlyList<string>)results;
			},
			cancellationToken,
			BlockingTaskCreationOptions,
			TaskScheduler.Current);

		/// <inheritdoc />
		public Task<IReadOnlyList<string>> GetFiles(string path, CancellationToken cancellationToken) => Task.Factory.StartNew(
			() =>
			{
				path = ResolvePath(path);
				var results = new List<string>();
				cancellationToken.ThrowIfCancellationRequested();
				foreach (var fileName in fileSystem.Directory.EnumerateFiles(path))
				{
					results.Add(fileName);
					cancellationToken.ThrowIfCancellationRequested();
				}

				return (IReadOnlyList<string>)results;
			},
			cancellationToken,
			BlockingTaskCreationOptions,
			TaskScheduler.Current);

		/// <inheritdoc />
		public Task ZipToDirectory(string path, Stream zipFile, CancellationToken cancellationToken) => Task.Factory.StartNew(
			() =>
			{
				path = ResolvePath(path);
				ArgumentNullException.ThrowIfNull(zipFile);

#if NET9_0_OR_GREATER
#error Check if zip file seeking has been addressesed. See https://github.com/tgstation/tgstation-server/issues/1531
#endif

				// ZipArchive does a synchronous copy on unseekable streams we want to avoid
				if (!zipFile.CanSeek)
					throw new ArgumentException("Stream does not support seeking!", nameof(zipFile));

				using var archive = new ZipArchive(zipFile, ZipArchiveMode.Read, true);
				archive.ExtractToDirectory(path);
			},
			cancellationToken,
			BlockingTaskCreationOptions,
			TaskScheduler.Current);

		/// <inheritdoc />
		public bool PathContainsParentAccess(string path) => path
			?.Split(
				[
					fileSystem.Path.DirectorySeparatorChar,
					fileSystem.Path.AltDirectorySeparatorChar,
				])
			.Any(x => x == "..")
			?? throw new ArgumentNullException(nameof(path));

		/// <inheritdoc />
		public Task<DateTimeOffset> GetLastModified(string path, CancellationToken cancellationToken) => Task.Factory.StartNew(
			() =>
			{
				path = ResolvePath(path ?? throw new ArgumentNullException(nameof(path)));
				var fileInfo = fileSystem.FileInfo.New(path);
				return new DateTimeOffset(fileInfo.LastWriteTimeUtc);
			},
			cancellationToken,
			BlockingTaskCreationOptions,
			TaskScheduler.Current);

		/// <inheritdoc />
		public Task<bool> PathIsChildOf(string parentPath, string childPath, CancellationToken cancellationToken) => Task.Factory.StartNew(
			() =>
			{
				parentPath = ResolvePath(parentPath);
				childPath = ResolvePath(childPath);

				if (parentPath == childPath)
					return true;

				// https://stackoverflow.com/questions/5617320/given-full-path-check-if-path-is-subdirectory-of-some-other-path-or-otherwise?lq=1
				var di1 = fileSystem.DirectoryInfo.New(parentPath);
				var di2 = fileSystem.DirectoryInfo.New(childPath);
				while (di2.Parent != null)
				{
					if (di2.Parent.FullName == di1.FullName)
						return true;

					di2 = di2.Parent;
				}

				return false;
			},
			cancellationToken,
			BlockingTaskCreationOptions,
			TaskScheduler.Current);

		/// <inheritdoc />
		public Task<IDirectoryInfo> DirectoryInfo(string path, CancellationToken cancellationToken)
			=> Task.Factory.StartNew(
				() => fileSystem.DirectoryInfo.New(ResolvePath(path)),
				cancellationToken,
				BlockingTaskCreationOptions,
				TaskScheduler.Current);

		/// <inheritdoc />
		public bool IsPathRooted(string path)
			=> fileSystem.Path.IsPathRooted(path);

		/// <inheritdoc />
		public IIOManager CreateResolverForSubdirectory(string subdirectoryPath)
		{
			ArgumentNullException.ThrowIfNull(subdirectoryPath);

			return new ResolvingIOManager(
				fileSystem,
				ConcatPath(
					ResolvePath(),
					subdirectoryPath));
		}

		/// <summary>
		/// Copies a directory from <paramref name="src"/> to <paramref name="dest"/>.
		/// </summary>
		/// <param name="src">The source directory path.</param>
		/// <param name="dest">The destination directory path.</param>
		/// <param name="ignore">Optional files and folders to ignore at the root level.</param>
		/// <param name="postCopyCallback">The optional callback called for each source/dest file pair post copy.</param>
		/// <param name="semaphore">Optional <see cref="SemaphoreSlim"/> used to limit degree of parallelism.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="IEnumerable{T}"/> of <see cref="Task"/>s representing the running operations. The first <see cref="Task"/> returned is always the necessary call to <see cref="CreateDirectory(string, CancellationToken)"/>.</returns>
		IEnumerable<Task> CopyDirectoryImpl(
			string src,
			string dest,
			IEnumerable<string>? ignore,
			Func<string, string, ValueTask>? postCopyCallback,
			SemaphoreSlim? semaphore,
			CancellationToken cancellationToken)
		{
			var dir = fileSystem.DirectoryInfo.New(src);
			Task? subdirCreationTask = null;
			foreach (var subDirectory in dir.EnumerateDirectories())
			{
				if (ignore != null && ignore.Contains(subDirectory.Name))
					continue;

				var checkingSubdirCreationTask = true;
				foreach (var copyTask in CopyDirectoryImpl(subDirectory.FullName, fileSystem.Path.Combine(dest, subDirectory.Name), null, postCopyCallback, semaphore, cancellationToken))
				{
					if (subdirCreationTask == null)
					{
						subdirCreationTask = copyTask;
						yield return subdirCreationTask;
					}
					else if (!checkingSubdirCreationTask)
						yield return copyTask;

					checkingSubdirCreationTask = false;
				}
			}

			foreach (var fileInfo in dir.EnumerateFiles())
			{
				if (subdirCreationTask == null)
				{
					subdirCreationTask = CreateDirectory(dest, cancellationToken);
					yield return subdirCreationTask;
				}

				if (ignore != null && ignore.Contains(fileInfo.Name))
					continue;

				var sourceFile = fileInfo.FullName;
				var destFile = ConcatPath(dest, fileInfo.Name);

				async Task CopyThisFile()
				{
					await subdirCreationTask.WaitAsync(cancellationToken);
					using var lockContext = semaphore != null
						? await SemaphoreSlimContext.Lock(semaphore, cancellationToken)
						: null;
					await CopyFile(sourceFile, destFile, cancellationToken);
					if (postCopyCallback != null)
						await postCopyCallback(sourceFile, destFile);
				}

				yield return CopyThisFile();
			}
		}
	}
}
