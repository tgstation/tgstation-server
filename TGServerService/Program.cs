using System;
using System.Collections.Generic;
using System.IO;

namespace TGServerService
{
	public static class Program
	{
		static void Main() => new TGServerService();	//wondows
		
		//Everything in this file is just generic helpers

		//http://stackoverflow.com/questions/1701457/directory-delete-doesnt-work-access-denied-error-but-under-windows-explorer-it
		public static void DeleteDirectory(string path, bool ContentsOnly = false, IList<string> excludeRoot = null)
		{
			var di = new DirectoryInfo(path);
			if (!di.Exists)
				return;
			if (excludeRoot != null)
				for (var I = 0; I < excludeRoot.Count; ++I)
					excludeRoot[I] = excludeRoot[I].ToLower();

			NormalizeAndDelete(di, excludeRoot);
			if (!ContentsOnly)
			{
				if (excludeRoot != null)
					throw new Exception("Cannot fully delete folder with exclusions specified!");
				di.Delete(true);
			}
		}
		static void NormalizeAndDelete(DirectoryInfo dir, IList<string> excludeRoot)
		{
			foreach (var subDir in dir.GetDirectories())
			{
				if (excludeRoot != null && excludeRoot.Contains(subDir.Name.ToLower()))
					continue;
				NormalizeAndDelete(subDir, null);
				subDir.Delete(true);
			}
			foreach (var file in dir.GetFiles())
			{
				if (excludeRoot != null && excludeRoot.Contains(file.Name.ToLower()))
					continue;
				file.Attributes = FileAttributes.Normal;
			}
		}

		public static void CopyDirectory(string sourceDirName, string destDirName, IList<string> ignore = null, bool ignoreIfNotExists = false)
		{
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
				if (ignore != null && ignore.Contains(file.Name))
					continue;
				string temppath = Path.Combine(destDirName, file.Name);
				file.CopyTo(temppath, true);
			}

			// copy them and their contents to new location.
			foreach (DirectoryInfo subdir in dirs)
			{
				if (ignore != null && ignore.Contains(subdir.Name))
					continue;
				string temppath = Path.Combine(destDirName, subdir.Name);
				CopyDirectory(subdir.FullName, temppath, ignore);
			}
		}
	}
}
