using System;
using System.IO;
using System.Reflection;

namespace Tgstation.Server.Host.Watchdog
{
	/// <summary>
	/// See <see cref="IActiveAssemblyDeleter"/> for POSIX systems
	/// </summary>
	sealed class PosixActiveAssemblyDeleter : IActiveAssemblyDeleter
	{
		/// <inheritdoc />
		public void DeleteActiveAssembly(string assemblyPath) => File.Delete(assemblyPath ?? throw new ArgumentNullException(nameof(assemblyPath))); //glory of inodes
	}
}
