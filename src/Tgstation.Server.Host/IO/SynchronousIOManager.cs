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
			foreach (var I in Directory.EnumerateDirectories(path))
			{
				yield return Path.GetFileName(I);
				cancellationToken.ThrowIfCancellationRequested();
			}
		}

		/// <inheritdoc />
		public IEnumerable<string> GetFiles(string path, CancellationToken cancellationToken)
		{
			foreach (var I in Directory.EnumerateFiles(path))
			{
				yield return Path.GetFileName(I);
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
		public bool WriteFileChecked(string path, byte[] data, ref string sha1InOut, CancellationToken cancellationToken)
		{
			if (path == null)
				throw new ArgumentNullException(nameof(path));
			cancellationToken.ThrowIfCancellationRequested();
			var directory = Path.GetDirectoryName(path);
			Directory.CreateDirectory(directory);
			cancellationToken.ThrowIfCancellationRequested();
			using (var file = File.Open(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None))
			{
				cancellationToken.ThrowIfCancellationRequested();

				// as nice as it would be to not have to arrayify the memory stream, we have to
				// because, oddly enough sha1(memorystream) != sha1(memorystream.ToArray())
				// vOv
				byte[] originalBytes;
				using (var readMs = new MemoryStream())
				{
					file.CopyTo(readMs);
					originalBytes = readMs.ToArray();
				}

				// no sha1? no write
				if (originalBytes.Length != 0 && sha1InOut == null)
					return false;

				// suppressed due to only using for consistency checks
#pragma warning disable CA5350 // Do not use insecure cryptographic algorithm SHA1.
				using (var sha1 = new SHA1Managed())
#pragma warning restore CA5350 // Do not use insecure cryptographic algorithm SHA1.
				{
					string GetSha1(byte[] dataToHash) => dataToHash != null && dataToHash.Length != 0 ? String.Join(String.Empty, sha1.ComputeHash(dataToHash).Select(b => b.ToString("x2", CultureInfo.InvariantCulture))) : null;
					var originalSha1 = GetSha1(originalBytes);
					if (originalSha1 != sha1InOut)
					{
						sha1InOut = originalSha1;
						return false;
					}

					sha1InOut = GetSha1(data);
				}

				cancellationToken.ThrowIfCancellationRequested();

				if (data != null)
				{
					file.Seek(0, SeekOrigin.Begin);

					cancellationToken.ThrowIfCancellationRequested();
					file.SetLength(data.Length);
					file.Write(data, 0, data.Length);
				}
			}

			if (data == null)
				File.Delete(path);
			return true;
		}
	}
}
