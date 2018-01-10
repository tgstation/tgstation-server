using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace TGS.Server
{
	/// <summary>
	/// Generic helpers for the <see cref="Server"/>
	/// </summary>
	static class Helpers
	{
		/// <summary>
		/// Copy a file from <paramref name="source"/> to <paramref name="dest"/>, but first ensure the destination directory exists
		/// </summary>
		/// <param name="source">The source file</param>
		/// <param name="dest">The destination file</param>
		/// <param name="overwrite">If <see langword="true"/>, <paramref name="source"/> will overwrite <paramref name="dest"/> if it is a file. Otherwise, if <paramref name="dest"/> exists, an exception will be thrown</param>
		public static void CopyFileForceDirectories(string source, string dest, bool overwrite)
		{
			try
			{
				Directory.CreateDirectory(Path.GetDirectoryName(dest));
			}
			catch { }   //we don't care if the above errors
			File.Copy(source, dest, overwrite);    //if this throws errors thats all we care about
		}

		//http://stackoverflow.com/questions/1701457/directory-delete-doesnt-work-access-denied-error-but-under-windows-explorer-it
		/// <summary>
		/// Recursive directory deleter
		/// </summary>
		/// <param name="path">The directory to delete</param>
		/// <param name="ContentsOnly">If <see langword="true"/>, an empty <paramref name="path"/> will remain instead of being deleted fully. Incompatible with <paramref name="excludeRoot"/></param>
		/// <param name="excludeRoot">If any files or directories in the root level of <paramref name="path"/> match anything in this <see cref="IList{T}"/> of <see cref="string"/>s, they won't be deleted. Incompatible with <paramref name="ContentsOnly"/></param>
		public static void DeleteDirectory(string path, bool ContentsOnly = false, IList<string> excludeRoot = null)
		{
			if (!ContentsOnly && excludeRoot != null && excludeRoot.Count > 0)
				throw new ArgumentException("Cannot fully delete folder with exclusions specified!");
			var di = new DirectoryInfo(path);
			if (!di.Exists)
				return;
			if (excludeRoot != null)
				for (var I = 0; I < excludeRoot.Count; ++I)
					excludeRoot[I] = excludeRoot[I].ToLower();
			if (CheckDeleteSymlinkDir(di))
				return;
			NormalizeAndDelete(di, excludeRoot, !ContentsOnly).Wait();
		}

		/// <summary>
		/// Find all files in a directory with a given extension
		/// </summary>
		/// <param name="directory">The directory to search</param>
		/// <param name="extension">The extension to look for</param>
		/// <returns>An <see cref="IEnumerable{T}"/> of <see cref="string"/>s containing the full paths to files with the given <paramref name="extension"/> in <paramref name="directory"/></returns>
		public static IEnumerable<string> GetFilesWithExtensionInDirectory(string directory, string extension)
		{
			if (directory == null)
				throw new ArgumentNullException(nameof(directory));
			if (extension == null)
				throw new ArgumentNullException(nameof(extension));

			var di = new DirectoryInfo(directory);
			if (!di.Exists)
				yield break;

			foreach (var F in di.EnumerateFiles(String.Format(CultureInfo.InvariantCulture, "*.{0}", extension)))
				yield return F.FullName;
		}

		/// <summary>
		/// Properly unlinks directory <paramref name="di"/> if it is a symlink
		/// </summary>
		/// <param name="di"><see cref="DirectoryInfo"/> for the directory in question</param>
		/// <returns><see langword="true"/> if <paramref name="di"/> was a symlink and deleted, <see langword="false"/> otherwise</returns>
		static bool CheckDeleteSymlinkDir(DirectoryInfo di)
		{
			if (!di.Attributes.HasFlag(FileAttributes.Directory) || di.Attributes.HasFlag(FileAttributes.ReparsePoint))
			{    //this is probably a symlink
				Directory.Delete(di.FullName);
				return true;
			}
			return false;
		}

		/// <summary>
		/// Recursively empty a directory
		/// </summary>
		/// <param name="dir"><see cref="DirectoryInfo"/> of the directory to empty</param>
		/// <param name="excludeRoot">Lowercase file and directory names to skip while emptying this level. Not passed forward</param>
		/// <param name="deleteRoot">If <see langword="true"/>, deletes <paramref name="dir"/> on completion</param>
		/// <returns>A <see cref="Task"/> representing the operation</returns>
		static async Task NormalizeAndDelete(DirectoryInfo dir, IList<string> excludeRoot, bool deleteRoot)
		{
			var tasks = new List<Task>();
			foreach (var subDir in dir.GetDirectories())
			{
				var upperName = subDir.Name.ToUpperInvariant();
				if (excludeRoot != null && excludeRoot.Any(x => x.ToUpperInvariant() == upperName))
					continue;
				if (CheckDeleteSymlinkDir(subDir))
					continue;
				tasks.Add(NormalizeAndDelete(subDir, null, true));
			}
			foreach (var file in dir.GetFiles())
			{
				var upperName = file.Name.ToUpperInvariant();
				if (excludeRoot != null && excludeRoot.Any(x => x.ToUpperInvariant() == upperName))
					continue;
				file.Attributes = FileAttributes.Normal;
				file.Delete();
			}
			await Task.WhenAll(tasks);
			if(deleteRoot)
				dir.Delete(true);
		}

		/// <summary>
		/// Recusively copy a directory
		/// </summary>
		/// <param name="sourceDirName">The directory to copy</param>
		/// <param name="destDirName">The destination directory</param>
		/// <param name="ignore">List of files and directories to ignore while copying</param>
		/// <param name="ignoreIfNotExists">If <see langword="true"/> no error will be thrown if <paramref name="sourceDirName"/> does not exist</param>
		public static void CopyDirectory(string sourceDirName, string destDirName, IList<string> ignore = null, bool ignoreIfNotExists = false)
		{
			IList<string> realIgnore;
			if (ignore != null)
			{
				realIgnore = new List<string>();
				foreach (var I in ignore)
					realIgnore.Add(I.ToLower());
			}
			else
				realIgnore = null;
			CopyDirectoryImpl(sourceDirName, destDirName, realIgnore, ignoreIfNotExists).Wait();				
		}

		/// <summary>
		/// Recusively copy a directory
		/// </summary>
		/// <param name="sourceDirName">The directory to copy</param>
		/// <param name="destDirName">The destination directory</param>
		/// <param name="ignore">List of lowercase files and directories to ignore while copying</param>
		/// <param name="ignoreIfNotExists">If <see langword="true"/> no error will be thrown if <paramref name="sourceDirName"/> does not exist</param>
		/// <returns>A <see cref="Task"/> representing the operation</returns>
		static async Task CopyDirectoryImpl(string sourceDirName, string destDirName, IList<string> ignore, bool ignoreIfNotExists) { 
			// If the destination directory doesn't exist, create it.
			Directory.CreateDirectory(destDirName);
			// Get the subdirectories for the specified directory.
			DirectoryInfo dir = new DirectoryInfo(sourceDirName);

			if (!dir.Exists)
			{
				if (ignoreIfNotExists)
					return;
				throw new DirectoryNotFoundException(
					"Source directory does not exist or could not be found: "
					+ sourceDirName);
			}

			var tasks = new List<Task>();
			DirectoryInfo[] dirs = dir.GetDirectories();
			// copy them and their contents to new location.
			foreach (DirectoryInfo subdir in dirs)
			{
				var upperName = subdir.Name.ToUpperInvariant();
				if (ignore != null && ignore.Any(x => x.ToUpperInvariant() == upperName))
					continue;
				string temppath = Path.Combine(destDirName, subdir.Name);
				tasks.Add(CopyDirectoryImpl(subdir.FullName, temppath, ignore, false));
			}

			// Get the files in the directory and copy them to the new location.
			FileInfo[] files = dir.GetFiles();
			foreach (FileInfo file in files)
			{
				var upperName = file.Name.ToUpperInvariant();
				if (ignore != null && ignore.Any(x => x.ToUpperInvariant() == upperName))
					continue;
				string temppath = Path.Combine(destDirName, file.Name);
				file.CopyTo(temppath, true);
			}

			await Task.WhenAll(tasks);
		}

		/// <summary>
		/// Properly escapes characters for a BYOND Topic() packet. See http://www.byond.com/docs/ref/info.html#/proc/list2params
		/// </summary>
		/// <param name="input">The <see cref="string"/> to sanitize</param>
		/// <returns>The sanitized string</returns>
		public static string SanitizeTopicString(string input)
		{
			return input.Replace("%", "%25").Replace("=", "%3d").Replace(";", "%3b").Replace("&", "%26").Replace("+", "%2b");
		}

		/// <summary>
		/// Normalizes different versions of a path <see cref="string"/>
		/// </summary>
		/// <param name="path">The path to normalize</param>
		/// <returns>The normalized path</returns>
		public static string NormalizePath(string path)
		{
			return Path.GetFullPath(new Uri(path).LocalPath)
					   .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
					   .ToUpperInvariant();
		}
	}
}
