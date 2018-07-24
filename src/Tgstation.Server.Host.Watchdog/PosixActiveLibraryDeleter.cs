using System;
using System.IO;

namespace Tgstation.Server.Host.Watchdog
{
	/// <summary>
	/// See <see cref="IActiveLibraryDeleter"/> for POSIX systems
	/// </summary>
	sealed class PosixActiveLibraryDeleter : IActiveLibraryDeleter
	{
		/// <inheritdoc />
		public void DeleteActiveLibrary(string assemblyPath) => Directory.Delete(assemblyPath ?? throw new ArgumentNullException(nameof(assemblyPath)), true); //glory of inodes
	}
}
