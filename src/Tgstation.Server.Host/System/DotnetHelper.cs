using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Tgstation.Server.Api.Models;
using Tgstation.Server.Host.IO;
using Tgstation.Server.Host.Jobs;

namespace Tgstation.Server.Host.System
{
	/// <summary>
	/// Helper methods for working with the dotnet executable.
	/// </summary>
	static class DotnetHelper
	{
		/// <summary>
		/// Locate a dotnet executable to use.
		/// </summary>
		/// <param name="platformIdentifier">The <see cref="IPlatformIdentifier"/> to use.</param>
		/// <param name="ioManager">The <see cref="IIOManager"/> to use.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A dotnet executable path to use.</returns>
		public static async ValueTask<string> GetDotnetPath(IPlatformIdentifier platformIdentifier, IIOManager ioManager, CancellationToken cancellationToken)
		{
			ArgumentNullException.ThrowIfNull(platformIdentifier);
			ArgumentNullException.ThrowIfNull(ioManager);

			var dotnetPaths = Common.DotnetHelper.GetPotentialDotnetPaths(platformIdentifier.IsWindows)
				.ToList();
			var tasks = dotnetPaths
				.Select(path => ioManager.FileExists(path, cancellationToken))
				.ToList();

			await Task.WhenAll(tasks);

			var selectedPathIndex = tasks.FindIndex(pathValidTask => pathValidTask.Result);

			if (selectedPathIndex == -1)
				throw new JobException(ErrorCode.CantFindDotnet);

			var dotnetPath = dotnetPaths[selectedPathIndex];

			return dotnetPath;
		}
	}
}
