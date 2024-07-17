#if NET6_0_OR_GREATER
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
#endif

namespace Tgstation.Server.Host.Common
{
#if NET6_0_OR_GREATER
	/// <summary>
	/// Helper functions for working with dotnet.exe.
	/// </summary>
	public static class DotnetHelper
	{
		/// <summary>
		/// Gets the path to the dotnet executable.
		/// </summary>
		/// <param name="isWindows">If the current system is a Windows OS.</param>
		/// <returns>The path to the dotnet executable.</returns>
		public static IEnumerable<string> GetPotentialDotnetPaths(bool isWindows)
		{
			var enviromentPath = Environment.GetEnvironmentVariable("PATH");
			if (enviromentPath == null)
				return Enumerable.Empty<string>();

			var paths = enviromentPath.Split(';');

			var exeName = "dotnet";
			IEnumerable<string> enumerator;
			if (isWindows)
			{
				exeName += ".exe";
				enumerator = new List<string>(paths)
				{
					"C:/Program Files/dotnet",
					"C:/Program Files (x86)/dotnet",
				};
			}
			else
				enumerator = paths
					.Select(x => x.Split(':'))
					.SelectMany(x => x)
					.Concat(new List<string>(2)
					{
						"/usr/bin",
						"/usr/share/bin",
						"/usr/local/share/dotnet",
					});

			enumerator = enumerator.Select(x => Path.Combine(x, exeName));

			return enumerator;
		}
	}
#endif
}
