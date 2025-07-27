using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Threading;
using System.Threading.Tasks;

namespace Tgstation.Server.Host.IO
{
	/// <summary>
	/// Interface for using filesystems.
	/// </summary>
	public interface IIOManager
	{
		/// <summary>
		/// Gets the primary directory separator character.
		/// </summary>
		char DirectorySeparatorChar { get; }

		/// <summary>
		/// Gets the alternative directory separator character.
		/// </summary>
		char AltDirectorySeparatorChar { get; }

		/// <summary>
		/// Create a new <see cref="IIOManager"/> that resolves paths to the specified <paramref name="subdirectoryPath"/>.
		/// </summary>
		/// <param name="subdirectoryPath">A relative or absolute path that the new <see cref="IIOManager"/> will resolve as its current directory.</param>
		/// <returns>A new <see cref="IIOManager"/>.</returns>
		IIOManager CreateResolverForSubdirectory(string subdirectoryPath);

		/// <summary>
		/// Retrieve the full path of the current working directory.
		/// </summary>
		/// <returns>The full path of the current working directory.</returns>
		string ResolvePath();

		/// <summary>
		/// Retrieve the full path of some <paramref name="path"/> given a relative path. Must be used before passing relative paths to other APIs. All other operations in this <see langword="interface"/> call this internally on given paths.
		/// </summary>
		/// <param name="path">Some path to retrieve the full path of.</param>
		/// <returns><paramref name="path"/> as a full path.</returns>
		string ResolvePath(string path);

		/// <summary>
		/// Gets the file name portion of a <paramref name="path"/>.
		/// </summary>
		/// <param name="path">The path to get the file name of.</param>
		/// <returns>The file name portion of <paramref name="path"/>.</returns>
		string GetFileName(string path);

		/// <summary>
		/// Gets the file name portion of a <paramref name="path"/> with.
		/// </summary>
		/// <param name="path">The path to get the file name of.</param>
		/// <returns>The file name portion of <paramref name="path"/>.</returns>
		string GetFileNameWithoutExtension(string path);

		/// <summary>
		/// Check if a <paramref name="path"/> contains the '..' parent directory accessor.
		/// </summary>
		/// <param name="path">The path to check.</param>
		/// <returns><see langword="true"/> if <paramref name="path"/> contains a '..' accessor, <see langword="false"/> otherwise.</returns>
		bool PathContainsParentAccess(string path);

		/// <summary>
		/// Check if a given <paramref name="parentPath"/> is a parent of a given <paramref name="parentPath"/>.
		/// </summary>
		/// <param name="parentPath">The parent path.</param>
		/// <param name="childPath">The child path.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in <see langword="true"/> if <paramref name="childPath"/> is a child of <paramref name="parentPath"/> or they are equivalent.</returns>
		Task<bool> PathIsChildOf(string parentPath, string childPath, CancellationToken cancellationToken);

		/// <summary>
		/// Copies a directory from <paramref name="src"/> to <paramref name="dest"/>.
		/// </summary>
		/// <param name="ignore">Files and folders to ignore at the root level.</param>
		/// <param name="postCopyCallback">The optional callback called for each source/dest file pair post copy.</param>
		/// <param name="src">The source directory path.</param>
		/// <param name="dest">The destination directory path.</param>
		/// <param name="taskThrottle">The optional maximum number of simultaneous tasks allowed to execute.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask"/> representing the running operation.</returns>
		ValueTask CopyDirectory(
			IEnumerable<string>? ignore,
			Func<string, string, ValueTask>? postCopyCallback,
			string src,
			string dest,
			int? taskThrottle,
			CancellationToken cancellationToken);

		/// <summary>
		/// Check that the file at <paramref name="path"/> exists.
		/// </summary>
		/// <param name="path">The file to check for existence.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in <see langword="true"/> if the file at <paramref name="path"/> exists, <see langword="false"/> otherwise.</returns>
		Task<bool> FileExists(string path, CancellationToken cancellationToken);

		/// <summary>
		/// Check that the directory at <paramref name="path"/> exists.
		/// </summary>
		/// <param name="path">The directory to check for existence.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task"/> resulting in <see langword="true"/> if the directory at <paramref name="path"/> exists, <see langword="false"/> otherwise.</returns>
		Task<bool> DirectoryExists(string path, CancellationToken cancellationToken);

		/// <summary>
		/// Returns all the contents of a file at <paramref name="path"/> as a <see cref="byte"/> array.
		/// </summary>
		/// <param name="path">The path of the file to read.</param>
		/// <param name="cancellationToken">A <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask"/> that results in the contents of a file at <paramref name="path"/>.</returns>
		/// <remarks>This function will fail to read files from the /proc filesystem on Linux.</remarks>
		ValueTask<Memory<byte>> ReadAllBytes(string path, CancellationToken cancellationToken);

		/// <summary>
		/// Returns full directory names in a given <paramref name="path"/>.
		/// </summary>
		/// <param name="path">The path to search for directories.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the directories in <paramref name="path"/>.</returns>
		Task<IReadOnlyList<string>> GetDirectories(string path, CancellationToken cancellationToken);

		/// <summary>
		/// Returns full file names in a given <paramref name="path"/>.
		/// </summary>
		/// <param name="path">The path to search for files.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the files in <paramref name="path"/>.</returns>
		Task<IReadOnlyList<string>> GetFiles(string path, CancellationToken cancellationToken);

		/// <summary>
		/// Creates an asynchronous <see cref="FileStream"/> for sequential writing.
		/// </summary>
		/// <param name="path">The path of the file to write, will be truncated.</param>
		/// <returns>The open <see cref="Stream"/>.</returns>
		Stream CreateAsyncSequentialWriteStream(string path);

		/// <summary>
		/// Creates an asynchronous <see cref="FileStream"/> for sequential reading.
		/// </summary>
		/// <param name="path">The path of the file to write, will be truncated.</param>
		/// <param name="sequential">If the sequential read flag should be added.</param>
		/// <param name="shareWrite">If <see cref="FileShare.Write"/> should be used.</param>
		/// <returns>The open <see cref="Stream"/>.</returns>
		Stream CreateAsyncReadStream(string path, bool sequential, bool shareWrite);

		/// <summary>
		/// Writes some <paramref name="contents"/> to a file at <paramref name="path"/> overwriting previous content.
		/// </summary>
		/// <param name="path">The path of the file to write.</param>
		/// <param name="contents">The contents of the file.</param>
		/// <param name="cancellationToken">A <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask"/> representing the running operation.</returns>
		ValueTask WriteAllBytes(string path, ReadOnlyMemory<byte> contents, CancellationToken cancellationToken);

		/// <summary>
		/// Copy a file from <paramref name="src"/> to <paramref name="dest"/>.
		/// </summary>
		/// <param name="src">The source file to copy.</param>
		/// <param name="dest">The destination path.</param>
		/// <param name="cancellationToken">A <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask"/> representing the running operation.</returns>
		ValueTask CopyFile(string src, string dest, CancellationToken cancellationToken);

		/// <summary>
		/// Gets the directory portion of a given <paramref name="path"/>.
		/// </summary>
		/// <param name="path">A path to check.</param>
		/// <returns>The directory portion of the given <paramref name="path"/>.</returns>
		/// <exception cref="InvalidOperationException">If <paramref name="path"/> is rooted.</exception>
		string GetDirectoryName(string path);

		/// <summary>
		/// Gets a list of files in <paramref name="path"/> with the given <paramref name="extension"/>.
		/// </summary>
		/// <param name="path">The directory which contains the files.</param>
		/// <param name="extension">The extension to look for without the preceeding ".".</param>
		/// <param name="recursive">If the search should be recursive.</param>
		/// <param name="cancellationToken">A <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task"/> resulting in a list of paths to files in <paramref name="path"/> with the given <paramref name="extension"/>.</returns>
		Task<List<string>> GetFilesWithExtension(string path, string extension, bool recursive, CancellationToken cancellationToken);

		/// <summary>
		/// Deletes a file at <paramref name="path"/>.
		/// </summary>
		/// <param name="path">The path of the file to delete.</param>
		/// <param name="cancellationToken">A <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task"/> representing the running operation.</returns>
		Task DeleteFile(string path, CancellationToken cancellationToken);

		/// <summary>
		/// Create a directory at <paramref name="path"/>.
		/// </summary>
		/// <param name="path">The path of the directory to create.</param>
		/// <param name="cancellationToken">A <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task"/> representing the running operation.</returns>
		Task CreateDirectory(string path, CancellationToken cancellationToken);

		/// <summary>
		/// Recursively delete a directory, removes and does not enter any symlinks encounterd.
		/// </summary>
		/// <param name="path">The path to the directory to delete.</param>
		/// <param name="cancellationToken">A <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task"/> representing the running operation.</returns>
		Task DeleteDirectory(string path, CancellationToken cancellationToken);

		/// <summary>
		/// Combines an array of strings into a path.
		/// </summary>
		/// <param name="paths">The paths to combine.</param>
		/// <returns>The combined path.</returns>
		string ConcatPath(params string[] paths);

		/// <summary>
		/// Moves a file at <paramref name="source"/> to <paramref name="destination"/>.
		/// </summary>
		/// <param name="source">The source file path.</param>
		/// <param name="destination">The destination path.</param>
		/// <param name="cancellationToken">A <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task"/> representing the running operation.</returns>
		Task MoveFile(string source, string destination, CancellationToken cancellationToken);

		/// <summary>
		/// Moves a directory at <paramref name="source"/> to <paramref name="destination"/>.
		/// </summary>
		/// <param name="source">The source directory path.</param>
		/// <param name="destination">The destination path.</param>
		/// <param name="cancellationToken">A <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task"/> representing the running operation.</returns>
		Task MoveDirectory(string source, string destination, CancellationToken cancellationToken);

		/// <summary>
		/// Extract a set of <paramref name="zipFile"/> to a given <paramref name="path"/>.
		/// </summary>
		/// <param name="path">The path to unzip to.</param>
		/// <param name="zipFile">The <see cref="Stream"/> of the <see cref="global::System.IO.Compression.ZipArchive"/>. Must have <see cref="Stream.CanSeek"/> set to <see langword="true"/>. Will be read completely and left open. <see cref="Stream.Position"/> will be indeterminate.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task"/> representing the running operation.</returns>
		Task ZipToDirectory(string path, Stream zipFile, CancellationToken cancellationToken);

		/// <summary>
		/// Get the <see cref="DateTimeOffset"/> of when a given <paramref name="path"/> was last modified.
		/// </summary>
		/// <param name="path">The path to get metadata for.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="DateTimeOffset"/> of when the file was last modified.</returns>
		Task<DateTimeOffset> GetLastModified(string path, CancellationToken cancellationToken);

		/// <summary>
		/// Gets a <see cref="IDirectoryInfo"/> for the given <paramref name="path"/>.
		/// </summary>
		/// <param name="path">The path to get <see cref="IDirectoryInfo"/> for.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="IDirectoryInfo"/> of the <paramref name="path"/>.</returns>
		Task<IDirectoryInfo> DirectoryInfo(string path, CancellationToken cancellationToken);

		/// <summary>
		/// Check if a given <paramref name="path"/> is at the root level of the filesystem.
		/// </summary>
		/// <param name="path">The path to check.</param>
		/// <returns><see langword="true"/> if the path is rooted, <see langword="false"/> otherwise.</returns>
		bool IsPathRooted(string path);
	}
}
