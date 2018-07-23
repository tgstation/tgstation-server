using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace Tgstation.Server.Host.Core
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
			throw new NotImplementedException();
		}

		/// <inheritdoc />
		public byte[] ReadFile(string path, CancellationToken cancellationToken)
		{
			throw new NotImplementedException();
		}

		/// <inheritdoc />
		public bool WriteFileChecked(string path, byte[] data, string previousSha1, CancellationToken cancellationToken)
		{
			throw new NotImplementedException();
		}
	}
}
