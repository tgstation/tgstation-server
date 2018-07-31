using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.DependencyModel;
using Microsoft.Extensions.DependencyModel.Resolution;

namespace Tgstation.Server.Host.Watchdog
{
	/// <summary>
	/// <see cref="ICompilationAssemblyResolver"/> for an absolute <see cref="basePath"/>
	/// </summary>
	sealed class AbsoluteCompilationAssemblyResolver : ICompilationAssemblyResolver
	{
		/// <summary>
		/// The <see cref="System.Reflection.Assembly"/> base path
		/// </summary>
		readonly string basePath;

		/// <summary>
		/// Construct a <see cref="AbsoluteCompilationAssemblyResolver"/>
		/// </summary>
		/// <param name="basePath">The value of <see cref="basePath"/></param>
		public AbsoluteCompilationAssemblyResolver(string basePath)
		{
			this.basePath = basePath ?? throw new ArgumentNullException(nameof(basePath));
		}

		/// <inheritdoc />
		public bool TryResolveAssemblyPaths(CompilationLibrary library, List<string> assemblies)
		{
			var paths = new List<string>();

			foreach (var assembly in library.Assemblies)
			{
				var assemblyFile = Path.Combine(basePath, assembly);
				if (File.Exists(assemblyFile))
					paths.Add(assemblyFile);
				else
					return false;
			}

			// only modify the assemblies parameter if we've resolved all files
			assemblies?.AddRange(paths);
			return true;
		}
	}
}