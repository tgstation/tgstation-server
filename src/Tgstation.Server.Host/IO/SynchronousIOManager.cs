﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;

using Microsoft.Extensions.Logging;

namespace Tgstation.Server.Host.IO
{
	/// <inheritdoc />
	sealed class SynchronousIOManager : ISynchronousIOManager
	{
		/// <summary>
		/// The <see cref="ILogger"/> for the <see cref="SynchronousIOManager"/>.
		/// </summary>
		readonly ILogger<SynchronousIOManager> logger;

		/// <summary>
		/// Initializes a new instance of the <see cref="SynchronousIOManager"/> class.
		/// </summary>
		/// <param name="logger">The value of <see cref="logger"/>.</param>
		public SynchronousIOManager(ILogger<SynchronousIOManager> logger)
		{
			this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
		}

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
			ArgumentNullException.ThrowIfNull(path);
			return Directory.Exists(path);
		}

		/// <inheritdoc />
		public byte[] ReadFile(string path)
		{
			ArgumentNullException.ThrowIfNull(path);
			return File.ReadAllBytes(path);
		}

		/// <inheritdoc />
		public bool WriteFileChecked(string path, Stream data, ref string? sha1InOut, CancellationToken cancellationToken)
		{
			ArgumentNullException.ThrowIfNull(path);
			ArgumentNullException.ThrowIfNull(data);

			cancellationToken.ThrowIfCancellationRequested();
			var directory = Path.GetDirectoryName(path) ?? throw new ArgumentException("path cannot be rooted!", nameof(path));
			Directory.CreateDirectory(directory);

			var newFile = !File.Exists(path);

			cancellationToken.ThrowIfCancellationRequested();

			logger.LogTrace("Starting checked write to {path} ({fileType} file)", path, newFile ? "New" : "Pre-existing");

			using (var file = File.Open(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None))
			{
				cancellationToken.ThrowIfCancellationRequested();

				// no sha1? no write
				if (file.Length != 0 && sha1InOut == null)
				{
					logger.LogDebug("Aborting checked write due to missing SHA!");
					return false;
				}

				// suppressed due to only using for consistency checks
				using (var sha1 = SHA1.Create())
				{
					string? GetSha1(Stream dataToHash)
					{
						if (dataToHash == null)
							return null;

						byte[] sha1Computed = dataToHash.Length != 0
							? sha1.ComputeHash(dataToHash)
							: sha1.ComputeHash(Array.Empty<byte>());

						return String.Join(String.Empty, sha1Computed.Select(b => b.ToString("x2", CultureInfo.InvariantCulture)));
					}

					var originalSha1 = GetSha1(file);

					if (!newFile)
						logger.LogTrace("Existing SHA calculated to be {sha}", originalSha1);

					if (originalSha1 != sha1InOut && !(newFile && sha1InOut == null))
					{
						sha1InOut = originalSha1;
						return false;
					}

					sha1InOut = GetSha1(data);
				}

				cancellationToken.ThrowIfCancellationRequested();

				if (data.Length != 0)
				{
					logger.LogDebug("Writing file of length {size}", data.Length);
					file.Seek(0, SeekOrigin.Begin);
					data.Seek(0, SeekOrigin.Begin);

					cancellationToken.ThrowIfCancellationRequested();
					file.SetLength(data.Length);
					data.CopyTo(file);
				}
			}

			if (data.Length == 0)
			{
				logger.LogDebug("Stream is empty, deleting file");
				File.Delete(path);
			}

			return true;
		}
	}
}
