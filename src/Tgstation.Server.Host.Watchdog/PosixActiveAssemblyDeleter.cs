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
		public void DeleteActiveAssembly(Assembly assembly) => File.Delete(assembly?.Location ?? throw new ArgumentNullException(nameof(assembly))); //glory of inodes
	}
}
