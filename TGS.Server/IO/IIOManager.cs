﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
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
		/// Returns all the contents of a file at <paramref name="path"/> as a <see cref="List{T}"/> of lines
		/// </summary>
		/// <param name="path">The path of the file to read</param>
		/// <returns>A <see cref="Task"/> that results in a <see cref="List{T}"/> of the contents of a file at <paramref name="path"/> split by line</returns>
		Task<List<string>> ReadAllLines(string path);

		/// <summary>
		/// Attempts to create an empty file at <paramref name="path"/>
		/// </summary>
		/// <param name="path">The path to touch</param>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		Task Touch(string path);

		/// <summary>
		/// Writes some line seperated <paramref name="contents"/> to a file at <paramref name="path"/> overwriting previous content
		/// </summary>
		/// <param name="path">The path of the file to write</param>
		/// <param name="contents">The line seperated contents of the file</param>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		Task WriteAllLines(string path, IEnumerable<string> contents);

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
		/// <returns>A <see cref="Task"/> that results in <see langword="true"/> of <paramref name="path"/> is a file</returns>
		Task<bool> FileExists(string path);

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
		/// <returns>A <see cref="Task"/> that results in the <see cref="DirectoryInfo"/> for the created directory</returns>
		Task<DirectoryInfo> CreateDirectory(string path);

		/// <summary>
		/// Check if a directory at <paramref name="path"/> exists
		/// </summary>
		/// <param name="path">The path of the file to check</param>
		/// <returns>A <see cref="Task"/> that results in <see langword="true"/> of <paramref name="path"/> is a directory</returns>
		Task<bool> DirectoryExists(string path);

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
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		Task CreateSymlink(string link, string target);

		/// <summary>
		/// Removes the symlink at <paramref name="path"/>
		/// </summary>
		/// <exception cref="System.InvalidOperationException">When <paramref name="path"/> is a concrete file or directory</exception>
		/// <param name="path">The path to unlink</param>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		Task Unlink(string path);

		/// <summary>
		/// Downloads a file from <paramref name="url"/> to <paramref name="path"/>
		/// </summary>
		/// <param name="url">The URL to download</param>
		/// <param name="path">The path to save the file at</param>
		/// <param name="cancellationToken">A <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task"/> representing the operation</returns>
		Task DownloadFile(string url, string path, CancellationToken cancellationToken);

		/// <summary>
		/// Performs an HTTP GET on <paramref name="url"/> and returns the result
		/// </summary>
		/// <param name="url">The URL to GET</param>
		/// <returns>A <see cref="Task"/> that results in the result of the GET</returns>
		Task<string> GetURL(string url);

		/// <summary>
		/// Unzips a zip<paramref name="file"/> to a <paramref name="destination"/> folder
		/// </summary>
		/// <param name="file">Path to the zipfile</param>
		/// <param name="destination">Destination path</param>
		/// <returns>A <see cref="Task"/> representing the operation</returns>
		Task UnzipFile(string file, string destination);
		
		/// <summary>
		/// Find all files in a directory with a given extension
		/// </summary>
		/// <param name="directory">The directory to search</param>
		/// <param name="extension">The extension to look for</param>
		/// <returns>A <see cref="Task"/> resulting in an <see cref="List{T}"/> of <see cref="string"/>s containing the full paths to files with the given <paramref name="extension"/> in <paramref name="directory"/></returns>
		Task<List<string>> GetFilesWithExtensionInDirectory(string directory, string extension);

		/// <summary>
		/// Gets the name of a <paramref name="file"/> without preceeding directories
		/// </summary>
		/// <param name="file">A path to a file</param>
		/// <returns>The name of a <paramref name="file"/> without preceeding directories</returns>
		string GetFileName(string file);
	}
}
