using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace TGS.Server.IO
{
	/// <summary>
	/// Interface for managing files ala <see cref="System.IO.File"/> and <see cref="System.IO.Directory"/>
	/// </summary>
	public interface IIOManager
	{
		/// <summary>
		/// Retrieve the full path of some <paramref name="path"/> given a relative path. Must be used before passing relative paths to other APIs. All other operations in this <see langword="interface"/> call this internally on given paths
		/// </summary>
		/// <param name="path">Some path to retrieve the full path of</param>
		/// <returns><paramref name="path"/> as a full canonical path</returns>
		string ResolvePath(string path);

		/// <summary>
		/// Returns all the contents of a file at <paramref name="path"/> as a <see cref="string"/>
		/// </summary>
		/// <param name="path">The path of the file to read</param>
		/// <returns>A <see cref="Task"/> that results in the contents of a file at <paramref name="path"/></returns>
		Task<string> ReadAllText(string path);

		/// <summary>
		/// Attempts to create an empty file at <paramref name="path"/>
		/// </summary>
		/// <param name="path">The path to touch</param>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		Task Touch(string path);

		/// <summary>
		/// Writes some <paramref name="contents"/> to a file at <paramref name="path"/> overwriting previous content
		/// </summary>
		/// <param name="path">The path of the file to write</param>
		/// <param name="contents">The contents of the file</param>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		Task WriteAllText(string path, string contents);

		/// <summary>
		/// Writes some <paramref name="additional_contents"/> to a file at <paramref name="path"/> after previous content
		/// </summary>
		/// <param name="path">The path of the file to write</param>
		/// <param name="additional_contents">The contents to add to the file</param>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		Task AppendAllText(string path, string additional_contents);

		/// <summary>
		/// Returns all the contents of a file at <paramref name="path"/> as a <see cref="byte"/> array
		/// </summary>
		/// <param name="path">The path of the file to read</param>
		/// <returns>A <see cref="Task"/> that results in the contents of a file at <paramref name="path"/></returns>
		Task<byte[]> ReadAllBytes(string path);

		/// <summary>
		/// Writes some <paramref name="contents"/> to a file at <paramref name="path"/> overwriting previous content
		/// </summary>
		/// <param name="path">The path of the file to write</param>
		/// <param name="contents">The contents of the file</param>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		Task WriteAllBytes(string path, byte[] contents);

		/// <summary>
		/// Check if a file at <paramref name="path"/> exists
		/// </summary>
		/// <param name="path">The path of the file to check</param>
		/// <returns><see langword="true"/> of <paramref name="path"/> is a file</returns>
		bool FileExists(string path);

		/// <summary>
		/// Deletes a file at <paramref name="path"/>
		/// </summary>
		/// <param name="path">The path of the file to delete</param>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		Task DeleteFile(string path);

		/// <summary>
		/// Copy a file from <paramref name="src"/> to <paramref name="dest"/>
		/// </summary>
		/// <param name="src">The source file to copy</param>
		/// <param name="dest">The destination path</param>
		/// <param name="overwrite">If <see langword="true"/> and <paramref name="dest"/> is a file, the file at <paramref name="dest"/> will be deleted instead of throwing an error</param>
		/// <param name="forceDirectories">If <see langword="true"/>, any directories in <paramref name="dest"/> will be attempted to be created</param>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		Task CopyFile(string src, string dest, bool overwrite, bool forceDirectories = false);

		/// <summary>
		/// Move a file from <paramref name="src"/> to <paramref name="dest"/>
		/// </summary>
		/// <param name="src">The source file to copy</param>
		/// <param name="dest">The destination path</param>
		/// <param name="overwrite">If <see langword="true"/> and <paramref name="dest"/> is a file, the file at <paramref name="dest"/> will be deleted instead of throwing an error</param>
		/// <param name="forceDirectories">If <see langword="true"/>, any directories in <paramref name="dest"/> will be attempted to be created</param>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		Task MoveFile(string src, string dest, bool overwrite, bool forceDirectories = false);

		/// <summary>
		/// Create a directory at <paramref name="path"/>
		/// </summary>
		/// <param name="path">The path of the directory to create</param>
		/// <returns>The <see cref="DirectoryInfo"/> for the created directory</returns>
		DirectoryInfo CreateDirectory(string path);

		/// <summary>
		/// Check if a directory at <paramref name="path"/> exists
		/// </summary>
		/// <param name="path">The path of the file to check</param>
		/// <returns><see langword="true"/> of <paramref name="path"/> is a directory</returns>
		bool DirectoryExists(string path);

		/// <summary>
		/// Recursively delete a directory
		/// </summary>
		/// <param name="path">The path to the directory to delete</param>
		/// <param name="ContentsOnly">If <see langword="true"/>, <paramref name="path"/> will remain as an empty directory. Incompatible with <paramref name="excludeRoot"/></param>
		/// <param name="excludeRoot">If any files or directories in the root level of <paramref name="path"/> match anything in this <see cref="IList{T}"/> of <see cref="string"/>s, they won't be deleted. Incompatible with <paramref name="ContentsOnly"/></param>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		Task DeleteDirectory(string path, bool ContentsOnly = false, IList<string> excludeRoot = null);

		/// <summary>
		/// Recusively copy a directory
		/// </summary>
		/// <param name="src">The directory to copy</param>
		/// <param name="dest">The destination directory</param>
		/// <param name="ignore"><see cref="IEnumerable{T}"/> of <see cref="string"/>s containing files and directories to ignore while copying</param>
		/// <param name="ignoreIfNotExists">If <see langword="true"/> no error will be thrown if <paramref name="src"/> does not exist</param>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		Task CopyDirectory(string src, string dest, IEnumerable<string> ignore = null, bool ignoreIfNotExists = false);

		/// <summary>
		/// Moves a directory. Will force a <see cref="CopyDirectory(string, string, IEnumerable{string}, bool)"/> if <paramref name="src"/> and <paramref name="dest"/> are not on the same drive
		/// </summary>
		/// <param name="src">The directory to move</param>
		/// <param name="dest">The destination directory</param>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		Task MoveDirectory(string src, string dest);

		/// <summary>
		/// Create a file symlink or directory junction <paramref name="link"/> to <paramref name="target"/>
		/// </summary>
		/// <param name="link">The path of the symlink</param>
		/// <param name="target">The path of the symlink target</param>
		void CreateSymlink(string link, string target);

		/// <summary>
		/// Removes the symlink at <paramref name="path"/>
		/// </summary>
		/// <exception cref="System.InvalidOperationException">When <paramref name="path"/> is a concrete file or directory</exception>
		/// <param name="path">The path to unlink</param>
		void Unlink(string path);
	}
}
