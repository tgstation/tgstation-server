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
		public IEnumerable<string> GetDirectories(string path, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			foreach (var I in Directory.EnumerateDirectories(path))
			{
				yield return I;
				cancellationToken.ThrowIfCancellationRequested();
			}
		}

		/// <inheritdoc />
		public IEnumerable<string> GetFiles(string path, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			foreach (var I in Directory.EnumerateFiles(path))
			{
				yield return I;
				cancellationToken.ThrowIfCancellationRequested();
			}
		}

		/// <inheritdoc />
		public byte[] ReadFile(string path) => File.ReadAllBytes(path);

		/// <inheritdoc />
		public bool WriteFileChecked(string path, byte[] data, string previousSha1, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			using (var file = File.Open(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None))
			{
				using (var readMs = new MemoryStream())
				{
					cancellationToken.ThrowIfCancellationRequested();
					file.CopyTo(readMs);
					if (readMs.Length != 0 && previousSha1 == null)
						return false;   //no sha1? no write
					//suppressed due to only using for consistency checks
#pragma warning disable CA5350 // Do not use insecure cryptographic algorithm SHA1.
					using (var sha1 = new SHA1Managed())
#pragma warning restore CA5350 // Do not use insecure cryptographic algorithm SHA1.
					{
						var sha1String = String.Join("", sha1.ComputeHash(readMs).Select(b => b.ToString("x2", CultureInfo.InvariantCulture)));
						if (sha1String != previousSha1)
							return false;
					}
				}

				cancellationToken.ThrowIfCancellationRequested();
				file.Seek(0, SeekOrigin.Begin);

				cancellationToken.ThrowIfCancellationRequested();
				file.SetLength(data.Length);

				cancellationToken.ThrowIfCancellationRequested();
				file.Write(data, 0, data.Length);

				return true;
			}
		}
	}
}
