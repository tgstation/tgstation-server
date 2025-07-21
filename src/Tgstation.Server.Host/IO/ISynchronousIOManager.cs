using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace Tgstation.Server.Host.IO
{
	/// <summary>
	/// For accessing the disk in a synchronous manner.
	/// </summary>
	interface ISynchronousIOManager
	{
		/// <summary>
		/// Enumerate files in a given <paramref name="path"/>.
		/// </summary>
		/// <param name="path">The path to look for files in.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="IEnumerable{T}"/> of file names in <paramref name="path"/>.</returns>
		IEnumerable<string> GetFiles(string path, CancellationToken cancellationToken);

		/// <summary>
		/// Enumerate directories in a given <paramref name="path"/>.
		/// </summary>
		/// <param name="path">The path to look for directories in.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="IEnumerable{T}"/> of directory names in <paramref name="path"/>.</returns>
		IEnumerable<string> GetDirectories(string path, CancellationToken cancellationToken);

		/// <summary>
		/// Create an empty directory at <paramref name="path"/>.
		/// </summary>
		/// <param name="path">The path to create.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation. Usage may result in partial writes.</param>
		/// <returns><see langword="true"/> if the directory already existed, <see langword="false"/> otherwise.</returns>
		bool CreateDirectory(string path, CancellationToken cancellationToken);

		/// <summary>
		/// Read the <see cref="byte"/>s of a file at a given <paramref name="path"/>.
		/// </summary>
		/// <param name="path">The path of the file to read.</param>
		/// <returns>A <see cref="byte"/> array representing the contents of the file at <paramref name="path"/>.</returns>
		byte[] ReadFile(string path);

		/// <summary>
		/// Deletes a directory at <paramref name="path"/> if it's empty.
		/// </summary>
		/// <param name="path">The path of the directory to delete.</param>
		/// <returns><see langword="true"/> if the directory does not exist or is empty and was deleted. <see langword="false"/> otherwise.</returns>
		bool DeleteDirectory(string path);

		/// <summary>
		/// Write <paramref name="data"/> to a file at a given <paramref name="path"/>.
		/// </summary>
		/// <param name="path">The path to the file to write.</param>
		/// <param name="data">A <see cref="Stream"/> containing the new contents of the file.</param>
		/// <param name="sha1InOut">The function only succeeds if this parameter matches the SHA-1 hash of the contents of the current file. Contains the SHA1 of the file on disk once the function returns.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns><see langword="true"/> on success, <see langword="false"/> if the operation failed due to <paramref name="sha1InOut"/> not matching the file's contents.</returns>
		bool WriteFileChecked(string path, Stream data, ref string? sha1InOut, CancellationToken cancellationToken);

		/// <summary>
		/// Checks if a given <paramref name="path"/> is a directory.
		/// </summary>
		/// <param name="path">The path to check.</param>
		/// <returns><see langword="true"/> if <paramref name="path"/> is a directory, <see langword="false"/> otherwise.</returns>
		bool IsDirectory(string path);

		/// <summary>
		/// Gets the <see cref="Stream"/> for a given file <paramref name="path"/> without write share.
		/// </summary>
		/// <param name="path">The path of the file.</param>
		/// <returns>The <see cref="Stream"/> of the file.</returns>
		Stream GetFileStream(string path);
	}
}
