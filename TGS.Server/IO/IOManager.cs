using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace TGS.Server.IO
{
	/// <inheritdoc />
	public class IOManager : IIOManager
	{
		#region Win32 Shit
		/// <summary>
		/// See https://msdn.microsoft.com/en-us/library/windows/desktop/aa363866(v=vs.85).aspx
		/// </summary>
		[DllImport("kernel32.dll", SetLastError = true)]
		static extern bool CreateSymbolicLink(string lpSymlinkFileName, string lpTargetFileName, SymbolicLink dwFlags);
		/// <summary>
		/// Type of link to make with <see cref="CreateSymbolicLink(string, string, SymbolicLink)"/>
		/// </summary>
		enum SymbolicLink
		{
			/// <summary>
			/// Create a file symlink
			/// </summary>
			File = 0,
			/// <summary>
			/// Create a directory junction
			/// </summary>
			Directory = 1
		}
		#endregion

		/// <summary>
		/// Recusively copy a directory
		/// </summary>
		/// <param name="sourceDirName">The directory to copy</param>
		/// <param name="destDirName">The destination directory</param>
		/// <param name="ignore">List of lowercase files and directories to ignore while copying</param>
		/// <param name="ignoreIfNotExists">If <see langword="true"/> no error will be thrown if <paramref name="sourceDirName"/> does not exist</param>
		static async Task CopyDirectoryImpl(string sourceDirName, string destDirName, IList<string> ignore, bool ignoreIfNotExists)
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
		/// Recursively empty a directory
		/// </summary>
		/// <param name="dir"><see cref="DirectoryInfo"/> of the directory to empty</param>
		/// <param name="excludeRoot">Lowercase file and directory names to skip while emptying this level. Not passed forward</param>
		/// <param name="deleteRoot">If <see langword="true"/>, <paramref name="dir"/> will be deleted before this function exists</param>
		static async Task NormalizeAndDelete(DirectoryInfo dir, IList<string> excludeRoot, bool deleteRoot)
		{
			var tasks = new List<Task>();

			foreach (var subDir in dir.GetDirectories())
			{
				if (excludeRoot != null && excludeRoot.Contains(subDir.Name.ToLower()))
					continue;
				if (CheckDeleteSymlinkDir(subDir))
					continue;
				tasks.Add(NormalizeAndDelete(subDir, null, true));
			}
			foreach (var file in dir.GetFiles())
			{
				if (excludeRoot != null && excludeRoot.Contains(file.Name.ToLower()))
					continue;
				file.Attributes = FileAttributes.Normal;
				file.Delete();
			}
			await Task.WhenAll(tasks);
			if (deleteRoot)
				dir.Delete(true);
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

		/// <inheritdoc />
		public virtual string ResolvePath(string path)
		{
			return Path.GetFullPath(new Uri(path).LocalPath)
					   .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
					   .ToUpperInvariant();
		}

		/// <inheritdoc />
		public async Task DeleteDirectory(string path, bool ContentsOnly = false, IList<string> excludeRoot = null)
		{
			if (!ContentsOnly && excludeRoot != null && excludeRoot.Count > 0)
				throw new InvalidOperationException("Cannot fully delete folder with exclusions specified!");
			path = ResolvePath(path);
			var di = new DirectoryInfo(path);
			if (!di.Exists)
				return;
			if (excludeRoot != null)
				for (var I = 0; I < excludeRoot.Count; ++I)
					excludeRoot[I] = excludeRoot[I].ToLower();
			if (CheckDeleteSymlinkDir(di))
				return;
			await NormalizeAndDelete(di, excludeRoot, !ContentsOnly);
		}

		/// <inheritdoc />
		public async Task CopyDirectory(string sourceDirName, string destDirName, IEnumerable<string> ignore = null, bool ignoreIfNotExists = false)
		{
			sourceDirName = ResolvePath(sourceDirName);
			destDirName = ResolvePath(destDirName);
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

		/// <inheritdoc />
		public void CreateSymlink(string link, string target)
		{
			link = ResolvePath(link);
			target = ResolvePath(target);
			if (!CreateSymbolicLink(new DirectoryInfo(link).FullName, new DirectoryInfo(target).FullName, File.Exists(target) ? SymbolicLink.File : SymbolicLink.Directory))
				throw new Exception(String.Format("Failed to create symlink from {0} to {1}! Error: {2}", target, link, Marshal.GetLastWin32Error()));
		}
		
		/// <inheritdoc />
		public Task<string> ReadAllText(string path)
		{
			return Task.Factory.StartNew(() =>
			{
				path = ResolvePath(path);
				return File.ReadAllText(path);
			});
		}

		/// <inheritdoc />
		public Task WriteAllText(string path, string contents)
		{
			return Task.Factory.StartNew(() =>
			{
				path = ResolvePath(path);
				File.WriteAllText(path, contents);
			});
		}

		/// <inheritdoc />
		public Task AppendAllText(string path, string additional_contents)
		{
			return Task.Factory.StartNew(() =>
			{
				path = ResolvePath(path);
				File.AppendAllText(path, additional_contents);
			});
		}

		/// <inheritdoc />
		public Task<byte[]> ReadAllBytes(string path)
		{
			return Task.Factory.StartNew(() =>
			{
				path = ResolvePath(path);
				return File.ReadAllBytes(path);
			});
		}

		/// <inheritdoc />
		public Task WriteAllBytes(string path, byte[] contents)
		{
			return Task.Factory.StartNew(() =>
			{
				path = ResolvePath(path);
				File.WriteAllBytes(path, contents);
			});
		}

		/// <inheritdoc />
		public bool FileExists(string path)
		{
			path = ResolvePath(path);
			return File.Exists(path);
		}

		/// <inheritdoc />
		public Task DeleteFile(string path)
		{
			return Task.Factory.StartNew(() =>
			{
				path = ResolvePath(path);
				try
				{
					File.Delete(path);
				}
				catch (DirectoryNotFoundException) { }	//don't care
			});
		}

		/// <inheritdoc />
		public Task CopyFile(string src, string dest, bool overwrite, bool forceDirectories)
		{
			return Task.Factory.StartNew(() =>
			{
				src = ResolvePath(src);
				dest = ResolvePath(dest);
				if (forceDirectories)
					Directory.CreateDirectory(Path.GetDirectoryName(dest));
				File.Copy(src, dest, overwrite);
			});
		}

		/// <inheritdoc />
		public Task MoveFile(string src, string dest, bool overwrite, bool forceDirectories)
		{
			return Task.Factory.StartNew(() =>
			{
				src = ResolvePath(src);
				dest = ResolvePath(dest);
				if (forceDirectories)
					Directory.CreateDirectory(Path.GetDirectoryName(dest));
				if (File.Exists(dest))
					File.Delete(dest);
				File.Move(src, dest);
			});
		}

		/// <inheritdoc />
		public void CreateDirectory(string path)
		{
			path = ResolvePath(path);
			Directory.CreateDirectory(path);
		}

		/// <inheritdoc />
		public bool DirectoryExists(string path)
		{
			path = ResolvePath(path);
			return Directory.Exists(path);
		}

		/// <inheritdoc />
		public Task MoveDirectory(string src, string dest)
		{
			src = ResolvePath(src);
			dest = ResolvePath(dest);
			if (Path.GetPathRoot(src) != Path.GetPathRoot(dest))
				return CopyDirectoryImpl(src, dest, null, false);
			return Task.Factory.StartNew(() => Directory.Move(src, dest));
		}
	}
}
