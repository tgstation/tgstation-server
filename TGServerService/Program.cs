using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;

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
				if (excludeRoot != null && excludeRoot.Count > 0)
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

		public static string EncryptData(byte[] data, out string sentropy)
		{
			// Generate additional entropy (will be used as the Initialization vector)
			byte[] entropy = new byte[20];
			using (var rng = new RNGCryptoServiceProvider())
				rng.GetBytes(entropy);

			byte[] ciphertext = ProtectedData.Protect(data, entropy, DataProtectionScope.CurrentUser);

			sentropy = Convert.ToBase64String(entropy, 0, entropy.Length);
			return Convert.ToBase64String(ciphertext, 0, ciphertext.Length);
		}

		public static byte[] DecryptData(string data, string entropy)
		{
			return ProtectedData.Unprotect(Convert.FromBase64String(data), Convert.FromBase64String(entropy), DataProtectionScope.CurrentUser);
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
