using System;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using Tgstation.Server.Host.Startup;

namespace Tgstation.Server.Host.Watchdog
{
	/// <summary>
	/// <see cref="IServerFactory"/> for loading <see cref="IServer"/>s in a different <see cref="AssemblyLoadContext"/>
	/// </summary>
	sealed class IsolatedServerFactory : AssemblyLoadContext, IServerFactory
	{
		/// <summary>
		/// The path of the <see cref="Assembly"/> to load
		/// </summary>
		readonly string assemblyPath;

		/// <summary>
		/// Construct a <see cref="IsolatedServerFactory"/>
		/// </summary>
		/// <param name="assemblyPath">The value of <see cref="assemblyPath"/></param>
		public IsolatedServerFactory(string assemblyPath) => this.assemblyPath = assemblyPath ?? throw new ArgumentNullException(nameof(assemblyPath));

		/// <summary>
		/// Loads the <see cref="Assembly"/> at <see cref="assemblyPath"/> and creates an <see cref="IServer"/> from it
		/// </summary>
		/// <param name="args">The arguments for the <see cref="IServer"/></param>
		/// <param name="updatePath">The updatePath for the <see cref="IServer"/></param>
		/// <returns>A new <see cref="IServer"/></returns>
		public IServer CreateServer(string[] args, string updatePath)
		{
			var assembly = LoadFromAssemblyPath(assemblyPath);
			//find the IServerFactory implementation

			var serverFactoryInterfaceType = typeof(IServerFactory);
			var serverFactoryImplementationType = assembly.GetTypes().Where(x => serverFactoryInterfaceType.IsAssignableFrom(x)).First();

			var serverFactory = (IServerFactory)Activator.CreateInstance(serverFactoryImplementationType);
			return serverFactory.CreateServer(args, updatePath);
		}

		//honestly have no idea what this is for, but the examples i see just return null and it seems to work just fine
		/// <inheritdoc />
		protected override Assembly Load(AssemblyName assemblyName) => null;
	}
}
