using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;

namespace Tgstation.Server.Host.IO
{
	/// <inheritdoc />
	sealed class SynchronousIOManager : ISynchronousIOManager
	{
		/// <inheritdoc />
		public bool CreateDirectory(string path, CancellationToken cancellationToken)
		{
			if (IsDirectory(path))
				return true;
			cancellationToken.ThrowIfCancellationRequested();
			Directory.CreateDirectory(path);
			return false;
		}

		/// <inheritdoc />
		public bool DeleteDirectory(string path)
		{
			if (File.Exists(path))
				return false;

			if (!Directory.Exists(path))
				return true;

			if (Directory.EnumerateFileSystemEntries(path).Any())
				return false;

			Directory.Delete(path);
			return true;
		}

		/// <inheritdoc />
		public IEnumerable<string> GetDirectories(string path, CancellationToken cancellationToken)
		{
			foreach (var directoryName in Directory.EnumerateDirectories(path))
			{
				yield return Path.GetFileName(directoryName);
				cancellationToken.ThrowIfCancellationRequested();
			}
		}

		/// <inheritdoc />
		public IEnumerable<string> GetFiles(string path, CancellationToken cancellationToken)
		{
			foreach (var fileName in Directory.EnumerateFiles(path))
			{
				yield return Path.GetFileName(fileName);
				cancellationToken.ThrowIfCancellationRequested();
			}
		}

		/// <inheritdoc />
		public bool IsDirectory(string path)
		{
			if (path == null)
				throw new ArgumentNullException(nameof(path));
			return Directory.Exists(path);
		}

		/// <inheritdoc />
		public byte[] ReadFile(string path)
		{
			if (path == null)
				throw new ArgumentNullException(nameof(path));
			return File.ReadAllBytes(path);
		}

		/// <inheritdoc />
		public bool WriteFileChecked(string path, Stream data, ref string sha1InOut, CancellationToken cancellationToken)
		{
			if (path == null)
				throw new ArgumentNullException(nameof(path));
			if (data == null)
				throw new ArgumentNullException(nameof(data));

			cancellationToken.ThrowIfCancellationRequested();
			var directory = Path.GetDirectoryName(path);

			Directory.CreateDirectory(directory);
			cancellationToken.ThrowIfCancellationRequested();
			using (var file = File.Open(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None))
			{
				cancellationToken.ThrowIfCancellationRequested();

				// no sha1? no write
				if (file.Length != 0 && sha1InOut == null)
					return false;

				// suppressed due to only using for consistency checks
				using (var sha1 = SHA1.Create())
				{
					string GetSha1(Stream dataToHash) => dataToHash != null && dataToHash.Length != 0 ? String.Join(String.Empty, sha1.ComputeHash(dataToHash).Select(b => b.ToString("x2", CultureInfo.InvariantCulture))) : null;
					var originalSha1 = GetSha1(file);
					if (originalSha1 != sha1InOut)
					{
						sha1InOut = originalSha1;
						return false;
					}

					sha1InOut = GetSha1(data);
				}

				cancellationToken.ThrowIfCancellationRequested();

				if (data.Length != 0)
				{
					file.Seek(0, SeekOrigin.Begin);
					data.Seek(0, SeekOrigin.Begin);

					cancellationToken.ThrowIfCancellationRequested();
					file.SetLength(data.Length);
					data.CopyTo(file);
				}
			}

			if (data.Length == 0)
				File.Delete(path);
			return true;
		}
	}
}
