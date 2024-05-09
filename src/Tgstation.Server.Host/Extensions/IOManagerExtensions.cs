using System;

using Tgstation.Server.Host.IO;
using Tgstation.Server.Host.System;

namespace Tgstation.Server.Host.Extensions
{
	/// <summary>
	/// Extension methods for <see cref="IIOManager"/>.
	/// </summary>
	static class IOManagerExtensions
	{
		/// <summary>
		/// Gets the local application data folder used by TGS.
		/// </summary>
		/// <param name="ioManager">The <see cref="IIOManager"/> to use.</param>
		/// <param name="assemblyInformationProvider">The <see cref="IAssemblyInformationProvider"/> to use.</param>
		/// <returns>The path to the local application data directory used by TGS.</returns>
		public static string GetPathInLocalDirectory(this IIOManager ioManager, IAssemblyInformationProvider assemblyInformationProvider)
		{
			ArgumentNullException.ThrowIfNull(ioManager);
			ArgumentNullException.ThrowIfNull(assemblyInformationProvider);

			return ioManager.ConcatPath(
				Environment.GetFolderPath(
					Environment.SpecialFolder.LocalApplicationData, // we use local application data here instead of comman application data because we store stuff here we don't want other users interfering with
					Environment.SpecialFolderOption.DoNotVerify),
				assemblyInformationProvider.VersionPrefix);
		}
	}
}
