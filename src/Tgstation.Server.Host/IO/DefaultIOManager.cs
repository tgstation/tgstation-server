using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

using Tgstation.Server.Host.System;

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

		/// <summary>
		/// The <see cref="IAssemblyInformationProvider"/> for the <see cref="DefaultIOManager"/>.
		/// </summary>
		readonly IAssemblyInformationProvider assemblyInformationProvider;

		/// <summary>
		/// Recursively empty a directory.
		/// </summary>
		/// <param name="dir"><see cref="DirectoryInfo"/> of the directory to empty.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		static void NormalizeAndDelete(DirectoryInfo dir, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();

			// check if we are a symbolic link
			if (!dir.Attributes.HasFlag(FileAttributes.Directory) || dir.Attributes.HasFlag(FileAttributes.ReparsePoint))
			{
				dir.Delete();
				return;
			}

			foreach (var subDir in dir.EnumerateDirectories())
				NormalizeAndDelete(subDir, cancellationToken);

			foreach (var file in dir.EnumerateFiles())
			{
				cancellationToken.ThrowIfCancellationRequested();
				file.Attributes = FileAttributes.Normal;
				file.Delete();
			}

			cancellationToken.ThrowIfCancellationRequested();
			dir.Delete(true);
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="DefaultIOManager"/> class.
		/// </summary>
		/// <param name="assemblyInformationProvider">The value of <see cref="assemblyInformationProvider"/>.</param>
		public DefaultIOManager(IAssemblyInformationProvider assemblyInformationProvider)
		{
			this.assemblyInformationProvider = assemblyInformationProvider ?? throw new ArgumentNullException(nameof(assemblyInformationProvider));
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="DefaultIOManager"/> class.
		/// </summary>
		protected DefaultIOManager()
		{
		}

		/// <inheritdoc />
		public Task CopyDirectory(
			string src,
			string dest,
			IEnumerable<string> ignore,
			Func<string, string, Task> postCopyCallback,
			CancellationToken cancellationToken)
		{
			if (src == null)
				throw new ArgumentNullException(nameof(src));
			if (dest == null)
				throw new ArgumentNullException(nameof(src));

			src = ResolvePath(src);
			dest = ResolvePath(dest);

			return Task.WhenAll(CopyDirectoryImpl(src, dest, ignore, postCopyCallback, cancellationToken));
		}

		/// <inheritdoc />
		public string ConcatPath(params string[] paths) => Path.Combine(paths);

		/// <inheritdoc />
		public async Task CopyFile(string src, string dest, CancellationToken cancellationToken)
		{
			if (src == null)
				throw new ArgumentNullException(nameof(src));
			if (dest == null)
				throw new ArgumentNullException(nameof(dest));

			// tested to hell and back, these are the optimal buffer sizes
			using var srcStream = new FileStream(
				ResolvePath(src),
				FileMode.Open,
				FileAccess.Read,
				FileShare.Read | FileShare.Delete,
				DefaultBufferSize,
				FileOptions.Asynchronous | FileOptions.SequentialScan);
			using var destStream = CreateAsyncSequentialWriteStream(dest);

			// value taken from documentation
			await srcStream.CopyToAsync(destStream, 81920, cancellationToken);
		}

		/// <inheritdoc />
		public Task CreateDirectory(string path, CancellationToken cancellationToken) => Task.Factory.StartNew(() => Directory.CreateDirectory(ResolvePath(path)), cancellationToken, BlockingTaskCreationOptions, TaskScheduler.Current);

		/// <inheritdoc />
		public Task DeleteDirectory(string path, CancellationToken cancellationToken)
		{
			path = ResolvePath(path);
			var di = new DirectoryInfo(path);
			if (!di.Exists)
				return Task.CompletedTask;

			return Task.Factory.StartNew(
				() => NormalizeAndDelete(di, cancellationToken),
				cancellationToken,
				BlockingTaskCreationOptions,
				TaskScheduler.Current);
		}

		/// <inheritdoc />
		public Task DeleteFile(string path, CancellationToken cancellationToken) => Task.Factory.StartNew(() => File.Delete(ResolvePath(path)), cancellationToken, BlockingTaskCreationOptions, TaskScheduler.Current);

		/// <inheritdoc />
		public Task<bool> FileExists(string path, CancellationToken cancellationToken) => Task.Factory.StartNew(() => File.Exists(ResolvePath(path)), cancellationToken, BlockingTaskCreationOptions, TaskScheduler.Current);

		/// <inheritdoc />
		public Task<bool> DirectoryExists(string path, CancellationToken cancellationToken) => Task.Factory.StartNew(() => Directory.Exists(ResolvePath(path)), cancellationToken, BlockingTaskCreationOptions, TaskScheduler.Current);

		/// <inheritdoc />
		public string GetDirectoryName(string path) => Path.GetDirectoryName(path ?? throw new ArgumentNullException(nameof(path)));

		/// <inheritdoc />
		public string GetFileName(string path) => Path.GetFileName(path ?? throw new ArgumentNullException(nameof(path)));

		/// <inheritdoc />
		public string GetFileNameWithoutExtension(string path) => Path.GetFileNameWithoutExtension(path ?? throw new ArgumentNullException(nameof(path)));

		/// <inheritdoc />
		public Task<List<string>> GetFilesWithExtension(string path, string extension, bool recursive, CancellationToken cancellationToken) => Task.Factory.StartNew(
			() =>
			{
				path = ResolvePath(path);
				if (extension == null)
					throw new ArgumentNullException(extension);
				var results = new List<string>();
				foreach (var fileName in Directory.EnumerateFiles(
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
				if (destination == null)
					throw new ArgumentNullException(nameof(destination));
				source = ResolvePath(source ?? throw new ArgumentNullException(nameof(source)));
				destination = ResolvePath(destination);
				File.Move(source, destination);
			},
			cancellationToken,
			BlockingTaskCreationOptions,
			TaskScheduler.Current);

		/// <inheritdoc />
		public Task MoveDirectory(string source, string destination, CancellationToken cancellationToken) => Task.Factory.StartNew(
			() =>
			{
				if (destination == null)
					throw new ArgumentNullException(nameof(destination));
				source = ResolvePath(source ?? throw new ArgumentNullException(nameof(source)));
				destination = ResolvePath(destination);
				Directory.Move(source, destination);
			},
			cancellationToken,
			BlockingTaskCreationOptions,
			TaskScheduler.Current);

		/// <inheritdoc />
		public async Task<byte[]> ReadAllBytes(string path, CancellationToken cancellationToken)
		{
			path = ResolvePath(path);
			using var file = new FileStream(
				path,
				FileMode.Open,
				FileAccess.Read,
				FileShare.ReadWrite | FileShare.Delete,
				DefaultBufferSize,
				FileOptions.Asynchronous | FileOptions.SequentialScan);
			byte[] buf;
			buf = new byte[file.Length];
			await file.ReadAsync(buf, cancellationToken);
			return buf;
		}

		/// <inheritdoc />
		public string ResolvePath() => ResolvePath(CurrentDirectory);

		/// <inheritdoc />
		public virtual string ResolvePath(string path) => Path.GetFullPath(path ?? throw new ArgumentNullException(nameof(path)));

		/// <inheritdoc />
		public async Task WriteAllBytes(string path, byte[] contents, CancellationToken cancellationToken)
		{
			using var file = CreateAsyncSequentialWriteStream(path);
			await file.WriteAsync(contents, cancellationToken);
		}

		/// <inheritdoc />
		public FileStream CreateAsyncSequentialWriteStream(string path)
		{
			path = ResolvePath(path);
			return new FileStream(
				path,
				FileMode.Create,
				FileAccess.Write,
				FileShare.Read | FileShare.Delete,
				DefaultBufferSize,
				FileOptions.Asynchronous | FileOptions.SequentialScan);
		}

		/// <inheritdoc />
		public Task<IReadOnlyList<string>> GetDirectories(string path, CancellationToken cancellationToken) => Task.Factory.StartNew(
			() =>
			{
				path = ResolvePath(path);
				var results = new List<string>();
				cancellationToken.ThrowIfCancellationRequested();
				foreach (var directoryName in Directory.EnumerateDirectories(path))
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
				foreach (var fileName in Directory.EnumerateFiles(path))
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
		public async Task<MemoryStream> DownloadFile(Uri url, CancellationToken cancellationToken)
		{
			using var httpClient = new HttpClient();
			httpClient.DefaultRequestHeaders.UserAgent.Add(assemblyInformationProvider.ProductInfoHeaderValue);
			var webRequestTask = httpClient.GetAsync(url, cancellationToken);
			using var response = await webRequestTask;
			response.EnsureSuccessStatusCode();
			using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
			var memoryStream = new MemoryStream();
			try
			{
				await responseStream.CopyToAsync(memoryStream, cancellationToken);
				memoryStream.Seek(0, SeekOrigin.Begin);
				return memoryStream;
			}
			catch
			{
				memoryStream.Dispose();
				throw;
			}
		}

		/// <inheritdoc />
		public Task ZipToDirectory(string path, Stream zipFile, CancellationToken cancellationToken) => Task.Factory.StartNew(
			() =>
			{
				path = ResolvePath(path);
				if (zipFile == null)
					throw new ArgumentNullException(nameof(zipFile));

				using var archive = new ZipArchive(zipFile, ZipArchiveMode.Read);
				archive.ExtractToDirectory(path);
			},
			cancellationToken,
			BlockingTaskCreationOptions,
			TaskScheduler.Current);

		/// <inheritdoc />
		public bool PathContainsParentAccess(string path) => path
			?.Split(
				new[]
				{
					Path.DirectorySeparatorChar,
					Path.AltDirectorySeparatorChar,
				})
			.Any(x => x == "..")
			?? throw new ArgumentNullException(nameof(path));

		/// <inheritdoc />
		public Task<DateTimeOffset> GetLastModified(string path, CancellationToken cancellationToken) => Task.Factory.StartNew(
			() =>
			{
				path = ResolvePath(path ?? throw new ArgumentNullException(nameof(path)));
				var fileInfo = new FileInfo(path);
				return new DateTimeOffset(fileInfo.LastWriteTimeUtc);
			},
			cancellationToken,
			BlockingTaskCreationOptions,
			TaskScheduler.Current);

		/// <inheritdoc />
		public FileStream GetFileStream(string path, bool shareWrite) => new (
			ResolvePath(path),
			FileMode.Open,
			FileAccess.Read,
			FileShare.Read | FileShare.Delete | (shareWrite ? FileShare.Write : FileShare.None),
			DefaultBufferSize,
			true);

		/// <summary>
		/// Copies a directory from <paramref name="src"/> to <paramref name="dest"/>.
		/// </summary>
		/// <param name="src">The source directory path.</param>
		/// <param name="dest">The destination directory path.</param>
		/// <param name="ignore">Files and folders to ignore at the root level.</param>
		/// <param name="postCopyCallback">The optional callback called for each source/dest file pair post copy.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="IEnumerable{T}"/> of <see cref="Task"/>s representing the running operations. The first <see cref="Task"/> returned is always the necessary call to <see cref="CreateDirectory(string, CancellationToken)"/>.</returns>
		IEnumerable<Task> CopyDirectoryImpl(
			string src,
			string dest,
			IEnumerable<string> ignore,
			Func<string, string, Task> postCopyCallback,
			CancellationToken cancellationToken)
		{
			var dir = new DirectoryInfo(src);
			Task subdirCreationTask = null;
			foreach (var subDirectory in dir.EnumerateDirectories())
			{
				if (ignore != null && ignore.Contains(subDirectory.Name))
					continue;

				var checkingSubdirCreationTask = true;
				foreach (var copyTask in CopyDirectoryImpl(subDirectory.FullName, Path.Combine(dest, subDirectory.Name), null, postCopyCallback, cancellationToken))
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
					await subdirCreationTask;
					await CopyFile(sourceFile, destFile, cancellationToken);
					if (postCopyCallback != null)
						await postCopyCallback(sourceFile, destFile);
				}

				yield return CopyThisFile();
			}
		}
	}
}
