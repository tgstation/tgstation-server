using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.InteropServices;

namespace Tgstation.Server.Host.Watchdog
{
	/// <summary>
	/// See <see cref="IActiveAssemblyDeleter"/> for Windows systems
	/// </summary>
	sealed class WindowsActiveAssemblyDeleter : IActiveAssemblyDeleter
	{
		/// <summary>
		/// Set a file located at <paramref name="path"/> to be deleted on reboot
		/// </summary>
		/// <param name="path">The file to delete on reboot</param>
		[ExcludeFromCodeCoverage]
		static void DeleteFileOnReboot(string path)
		{
			if (!NativeMethods.MoveFileEx(path, null, NativeMethods.MoveFileFlags.DelayUntilReboot))
				throw new Win32Exception(Marshal.GetLastWin32Error());
		}

		/// <inheritdoc />
		public void DeleteActiveAssembly(string assemblyPath)
		{
			if (assemblyPath == null)
				throw new ArgumentNullException(nameof(assemblyPath));
			
			//Can't use Path.GetTempFileName() because it may cross drives, which won't actually rename the file
			var tmpLocation = String.Concat(assemblyPath, Guid.NewGuid());
			File.Move(assemblyPath, tmpLocation);
			DeleteFileOnReboot(tmpLocation);
		}
	}
}
