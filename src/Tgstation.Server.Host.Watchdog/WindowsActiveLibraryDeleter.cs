using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.InteropServices;

namespace Tgstation.Server.Host.Watchdog
{
	/// <summary>
	/// See <see cref="IActiveLibraryDeleter"/> for Windows systems
	/// </summary>
	sealed class WindowsActiveLibraryDeleter : IActiveLibraryDeleter
	{
		/// <summary>
		/// Set a directory located at <paramref name="path"/> to be deleted on reboot
		/// </summary>
		/// <param name="path">The file to delete on reboot</param>
		[ExcludeFromCodeCoverage]
		static void DeleteDirectoryOnReboot(string path)
		{
			if (!NativeMethods.MoveFileEx(path, null, NativeMethods.MoveFileFlags.DelayUntilReboot))
				throw new Win32Exception(Marshal.GetLastWin32Error());
		}

		/// <inheritdoc />
		public void DeleteActiveLibrary(string assemblyPath)
		{
			if (assemblyPath == null)
				throw new ArgumentNullException(nameof(assemblyPath));
			
			var tmpLocation = Path.Combine(Path.GetDirectoryName(assemblyPath), Guid.NewGuid().ToString());
			Directory.Move(assemblyPath, tmpLocation);
			DeleteDirectoryOnReboot(tmpLocation);
		}
	}
}
