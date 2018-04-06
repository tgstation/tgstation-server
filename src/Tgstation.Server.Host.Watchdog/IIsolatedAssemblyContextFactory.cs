using Tgstation.Server.Host.Startup;

namespace Tgstation.Server.Host.Watchdog
{
	/// <summary>
	/// For creating <see cref="IServerFactory"/>s in an isolated context from an <see cref="System.Reflection.Assembly"/>
	/// </summary>
	interface IIsolatedAssemblyContextFactory
	{
		/// <summary>
		/// Create a <see cref="IServerFactory"/> for an unloaded <see cref="System.Reflection.Assembly"/>
		/// </summary>
		/// <param name="assemblyPath">The path to the <see cref="System.Reflection.Assembly"/> to create the <see cref="IServer"/> from</param>
		/// <returns>A new <see cref="IServerFactory"/></returns>
		IServerFactory CreateIsolatedServerFactory(string assemblyPath);
	}
}