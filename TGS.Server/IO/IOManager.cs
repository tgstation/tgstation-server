using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace TGS.Server.IO
{
	/// <inheritdoc />
	public class IOManager : IIOManager
	{
		/// <summary>
		/// Wrapper for <see cref="Path.Combine(string[])"/>
		/// </summary>
		/// <param name="paths">The paths to combine</param>
		/// <returns>The combined path</returns>
		public static string ConcatPath(params string[] paths)
		{
			return Path.Combine(paths);
		}

		/// <summary>
		/// Wrapper for <see cref="Path.GetDirectoryName(string)"/>
		/// </summary>
		/// <param name="path">The path to get the directory name for</param>
		/// <returns>The path to the directory of <paramref name="path"/></returns>
		public static string GetDirectoryName(string path)
		{
			return Path.GetDirectoryName(path);
		}

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
				string temppath = ConcatPath(destDirName, file.Name);
				file.CopyTo(temppath, true);
			}

			var tasks = new List<Task>();
			// copy them and their contents to new location.
			foreach (DirectoryInfo subdir in dirs)
			{
				if (ignore != null && ignore.Contains(subdir.Name.ToLower()))
					continue;
				string temppath = ConcatPath(destDirName, subdir.Name);
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
		/// Checks if <paramref name="path"/> is a symlink
		/// </summary>
		/// <param name="path">The path to check</param>
		/// <returns><see langword="true"/> if <paramref name="path"/> is a symlink, <see langword="false"/> otherwise</returns>
		static bool IsSymlink(string path)
		{
			FileAttributes attrsToCheck;
			if (File.Exists(path))
				attrsToCheck = new FileInfo(path).Attributes;
			else
				attrsToCheck = new DirectoryInfo(path).Attributes;
			return attrsToCheck.HasFlag(FileAttributes.ReparsePoint);
		}

		/// <summary>
		/// Properly unlinks directory <paramref name="di"/> if it is a symlink
		/// </summary>
		/// <param name="di"><see cref="DirectoryInfo"/> for the directory in question</param>
		/// <returns><see langword="true"/> if <paramref name="di"/> was a symlink and deleted, <see langword="false"/> otherwise</returns>
		static bool CheckDeleteSymlinkDir(DirectoryInfo di)
		{
			if (IsSymlink(di.FullName))
			{
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
		public Task CreateSymlink(string link, string target)
		{
			return Task.Factory.StartNew(() =>
			{
				link = ResolvePath(link);
				target = ResolvePath(target);
				if (!NativeMethods.CreateSymbolicLink(link, target, File.Exists(target) ? NativeMethods.SymbolicLink.File : NativeMethods.SymbolicLink.Directory))
					throw new SymlinkException(link, target, Marshal.GetLastWin32Error());
			});
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
		public Task<bool> FileExists(string path)
		{
			return Task.Factory.StartNew(() =>
			{
				path = ResolvePath(path);
				return File.Exists(path);
			});
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
		public Task<DirectoryInfo> CreateDirectory(string path)
		{
			return Task.Factory.StartNew(() =>
			{
				path = ResolvePath(path);
				return Directory.CreateDirectory(path);
			});
		}

		/// <inheritdoc />
		public Task<bool> DirectoryExists(string path)
		{
			return Task.Factory.StartNew(() =>
			{
				path = ResolvePath(path);
				return Directory.Exists(path);
			});
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

		/// <inheritdoc />
		public Task Unlink(string path)
		{
			return Task.Factory.StartNew(() =>
			{
				path = ResolvePath(path);
				var isDir = Directory.Exists(path);
				if (!isDir && !File.Exists(path))
					throw new FileNotFoundException(String.Format("File/Directory at {0} not found!", path));
				if (!IsSymlink(path))
					throw new InvalidOperationException("Cannot unlink a concrete file/directory!");
				if (isDir)
					Directory.Delete(path);
				else
					File.Delete(path);
			});
		}

		/// <inheritdoc />
		public Task Touch(string path)
		{
			return Task.Factory.StartNew(() =>
			{
				path = ResolvePath(path);
				File.Create(path).Close();
			});
		}

		/// <inheritdoc />
		public Task DownloadFile(string url, string path, CancellationToken cancellationToken)
		{
			return Task.Factory.StartNew(() =>
			{
				path = ResolvePath(path);
				Exception failed = null;
				using (var client = new WebClient())
				using (var waitHandle = new ManualResetEvent(false))
				{
					client.DownloadFileCompleted += (a, b) =>
					{
						failed = b.Error;
						waitHandle.Set();
					};
					client.DownloadFileAsync(new Uri(url), path);
					WaitHandle.WaitAny(new WaitHandle[] { waitHandle, cancellationToken.WaitHandle });
					if (cancellationToken.IsCancellationRequested)
					{
						client.CancelAsync();
						waitHandle.WaitOne();
						return;
					}
				}
				if (failed != null)
					throw failed;
			});
		}

		/// <inheritdoc />
		public Task<string> GetURL(string url)
		{
			return Task.Factory.StartNew(() =>
			{
				//get the latest version from the website
				var request = WebRequest.Create(url);
				var results = new List<string>();
				using (var response = request.GetResponse())
				using (var reader = new StreamReader(response.GetResponseStream()))
					return reader.ReadToEnd();
			});
		}

		/// <inheritdoc />
		public Task UnzipFile(string file, string destination)
		{
			return Task.Factory.StartNew(() => ZipFile.ExtractToDirectory(ResolvePath(file), ResolvePath(destination)));
		}
	}
}
