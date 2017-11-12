using System;
using System.Collections.Generic;
using System.IO;
using System.ServiceProcess;
using System.Threading.Tasks;

namespace TGServerService
{
	static class Program
	{
		/// <summary>
		/// Entry point to the program
		/// </summary>
		static void Main() {
			using (var S = new Service())
				ServiceBase.Run(S);
		}

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
		public static async void DeleteDirectory(string path, bool ContentsOnly = false, IList<string> excludeRoot = null)
		{
			var di = new DirectoryInfo(path);
			if (!di.Exists)
				return;
			if (excludeRoot != null)
				for (var I = 0; I < excludeRoot.Count; ++I)
					excludeRoot[I] = excludeRoot[I].ToLower();
			if (CheckDeleteSymlinkDir(di))
				return;
			await NormalizeAndDelete(di, excludeRoot, false);
			if (!ContentsOnly)
			{
				if (excludeRoot != null && excludeRoot.Count > 0)
					throw new Exception("Cannot fully delete folder with exclusions specified!");
				di.Delete(true);
			}
		}


		/// <summary>
		/// Properly unlinks directory <paramref name="di"/> if it is a symlink
		/// </summary>
		/// <param name="di"><see cref="DirectoryInfo"/> for the directory in question</param>
		/// <returns><see langword="true"/> if <paramref name="di"/> was a symlink and deleted, <see langword="false"/> otherwise</returns>
		static bool CheckDeleteSymlinkDir(DirectoryInfo di)
		{
			if (!di.Attributes.HasFlag(FileAttributes.Directory))
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
		static async Task NormalizeAndDelete(DirectoryInfo dir, IList<string> excludeRoot, bool deleteRoot)
		{
			var tasks = new List<Task> { Task.Factory.StartNew(() =>
			{
				 foreach (var file in dir.GetFiles())
				 {
					 if (excludeRoot != null && excludeRoot.Contains(file.Name.ToLower()))
						 continue;
					 file.Attributes = FileAttributes.Normal;
					 file.Delete();
				 }
			}) };
		
			foreach (var subDir in dir.GetDirectories())
			{
				if (excludeRoot != null && excludeRoot.Contains(subDir.Name.ToLower()))
					continue;
				if (CheckDeleteSymlinkDir(subDir))
					continue;
				tasks.Add(NormalizeAndDelete(subDir, null, true));
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
		public static async void CopyDirectory(string sourceDirName, string destDirName, IList<string> ignore = null, bool ignoreIfNotExists = false)
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
			await CopyDirectoryImpl(sourceDirName, destDirName, realIgnore, ignoreIfNotExists);				
		}

		/// <summary>
		/// Recusively copy a directory
		/// </summary>
		/// <param name="sourceDirName">The directory to copy</param>
		/// <param name="destDirName">The destination directory</param>
		/// <param name="ignore">List of lowercase files and directories to ignore while copying</param>
		/// <param name="ignoreIfNotExists">If <see langword="true"/> no error will be thrown if <paramref name="sourceDirName"/> does not exist</param>
		static async Task CopyDirectoryImpl(string sourceDirName, string destDirName, IList<string> ignore, bool ignoreIfNotExists) { 
			// If the destination directory doesn't exist, create it.
			if (!Directory.Exists(destDirName))
			{
				Directory.CreateDirectory(destDirName);
			}
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

			DirectoryInfo[] dirs = dir.GetDirectories();

			// Get the files in the directory and copy them to the new location.
			FileInfo[] files = dir.GetFiles();
			foreach (FileInfo file in files)
			{
				if (ignore != null && ignore.Contains(file.Name.ToLower()))
					continue;
				string temppath = Path.Combine(destDirName, file.Name);
				file.CopyTo(temppath, true);
			}

			var tasks = new List<Task>();
			// copy them and their contents to new location.
			foreach (DirectoryInfo subdir in dirs)
			{
				if (ignore != null && ignore.Contains(subdir.Name.ToLower()))
					continue;
				string temppath = Path.Combine(destDirName, subdir.Name);
				tasks.Add(CopyDirectoryImpl(subdir.FullName, temppath, ignore, false));
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
