using System;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Tgstation.Server.Host.Watchdog
{
	/// <summary>
	/// See <see cref="IActiveAssemblyDeleter"/> for Windows systems
	/// </summary>
	sealed class WindowsActiveAssemblyDeleter : IActiveAssemblyDeleter
	{
		/// <inheritdoc />
		public void DeleteActiveAssembly(Assembly assembly)
		{
			if (assembly == null)
				throw new ArgumentNullException(nameof(assembly));
			
			var location = assembly.Location;
			//Can't use Path.GetTempFileName() because it may cross drives, which won't actually rename the file
			//Also append the long path prefix just in case we're running on .NET framework
			var tmpLocation = String.Concat(@"\\?\", assembly.Location, Guid.NewGuid());
			File.Delete(tmpLocation);
			File.Move(location, tmpLocation);

			//this deletes the file at reboot, sadly it also triggers a restart notification on server os's but oh well
			if (!NativeMethods.MoveFileEx(tmpLocation, null, NativeMethods.MoveFileFlags.DelayUntilReboot))
				throw new Win32Exception(Marshal.GetLastWin32Error());
		}
	}
}
