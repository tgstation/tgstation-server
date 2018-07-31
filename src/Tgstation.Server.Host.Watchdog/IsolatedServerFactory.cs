using Microsoft.Extensions.DependencyModel;
using Microsoft.Extensions.DependencyModel.Resolution;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
using Tgstation.Server.Host.Startup;

namespace Tgstation.Server.Host.Watchdog
{
	/// <summary>
	/// <see cref="IServerFactory"/> for loading <see cref="IServer"/>s in a different <see cref="AssemblyLoadContext"/>
	/// </summary>
	sealed class IsolatedServerFactory : AssemblyLoadContext, IServerFactory
	{
		const string DllExtension = "dll";

		static readonly string assemblyFileName = String.Join(".", nameof(Tgstation), nameof(Server), nameof(Host), DllExtension);

		/// <summary>
		/// The path of the <see cref="Assembly"/> to load
		/// </summary>
		readonly string assemblyPath;

		/// <summary>
		/// The runtime identifier to load dependencies from
		/// </summary>
		readonly string runtimeIdentifier;

		/// <summary>
		/// The <see cref="DependencyContext"/> of the <see cref="Assembly"/> being loaded
		/// </summary>
		DependencyContext dependencyContext;

		/// <summary>
		/// The <see cref="ICompilationAssemblyResolver"/> for the <see cref="IsolatedServerFactory"/>
		/// </summary>
		ICompilationAssemblyResolver compilationAssemblyResolver;

		/// <summary>
		/// Construct a <see cref="IsolatedServerFactory"/>
		/// </summary>
		/// <param name="assemblyPath">The value of <see cref="assemblyPath"/></param>
		/// <param name="runtimeIdentifier">The value of <see cref="runtimeIdentifier"/></param>
		public IsolatedServerFactory(string assemblyPath, string runtimeIdentifier)
		{
			this.assemblyPath = assemblyPath ?? throw new ArgumentNullException(nameof(assemblyPath));
			this.runtimeIdentifier = runtimeIdentifier ?? throw new ArgumentNullException(nameof(runtimeIdentifier));

			Resolving += IsolatedServerFactory_Resolving;
		}

		//https://stackoverflow.com/a/40921746/3976486
		//https://samcragg.wordpress.com/2017/06/30/resolving-assemblies-in-net-core/
		Assembly IsolatedServerFactory_Resolving(AssemblyLoadContext context, AssemblyName assemblyName)
		{
			if (assemblyName.Name.EndsWith("resources", StringComparison.Ordinal))
				return null;
			
			var library = dependencyContext.RuntimeLibraries.FirstOrDefault(x => String.Equals(x.Name, assemblyName.Name, StringComparison.OrdinalIgnoreCase));

			if (library != null)
			{
				var wrapper = new CompilationLibrary(
					library.Type,
					library.Name,
					library.Version,
					library.Hash,
					library.RuntimeAssemblyGroups.SelectMany(x => x.AssetPaths),
					library.Dependencies,
					library.Serviceable
					);

				var assemblies = new List<string>();
				compilationAssemblyResolver.TryResolveAssemblyPaths(wrapper, assemblies);
				if (assemblies.Count > 0)
					return context.LoadFromAssemblyPath(assemblies.First());
			}

			//could be not a "true" dependency i.e. CodeAnaylsis
			var foundDll = Directory.GetFileSystemEntries(assemblyPath, String.Join(".", assemblyName.Name, DllExtension), SearchOption.AllDirectories).FirstOrDefault();
			if (foundDll != default)
				return context.LoadFromAssemblyPath(foundDll);
			return context.LoadFromAssemblyName(assemblyName);
		}

		/// <summary>
		/// Loads the <see cref="Assembly"/> at <see cref="assemblyPath"/> and creates an <see cref="IServer"/> from it
		/// </summary>
		/// <param name="args">The arguments for the <see cref="IServer"/></param>
		/// <param name="updatePath">The updatePath for the <see cref="IServer"/></param>
		/// <returns>A new <see cref="IServer"/></returns>
		public IServer CreateServer(string[] args, string updatePath)
		{
			var assembly = LoadFromAssemblyPath(Path.Combine(assemblyPath, assemblyFileName));
			dependencyContext = DependencyContext.Load(assembly);

			var resolvers = new List<ICompilationAssemblyResolver>();

			var fallbacks = dependencyContext.RuntimeGraph.Where(x => x.Runtime == runtimeIdentifier).FirstOrDefault();
			
			if(fallbacks != default)
			{
				var runtimesDirectory = Path.Combine(assemblyPath, "runtimes");
				resolvers.Add(new AbsoluteCompilationAssemblyResolver(Path.Combine(runtimesDirectory, runtimeIdentifier)));
				resolvers.AddRange(fallbacks.Fallbacks.Select(x => new AbsoluteCompilationAssemblyResolver(Path.Combine(runtimesDirectory, x))));
			}

			resolvers.Add(new AppBaseCompilationAssemblyResolver(assemblyPath));

			compilationAssemblyResolver = new CompositeCompilationAssemblyResolver(resolvers.ToArray());


			//find the IServerFactory implementation
			var serverFactoryInterfaceType = typeof(IServerFactory);

			var serverFactoryImplementationType = assembly.GetTypes().Where(x => serverFactoryInterfaceType.IsAssignableFrom(x)).First();

			var serverFactory = (IServerFactory)Activator.CreateInstance(serverFactoryImplementationType);

			return serverFactory.CreateServer(args, updatePath);
		}

		//honestly have no idea what this is for, https://github.com/dotnet/coreclr/blob/master/Documentation/design-docs/assemblyloadcontext.md
		/// <inheritdoc />
		protected override Assembly Load(AssemblyName assemblyName) => null;
	}
}
