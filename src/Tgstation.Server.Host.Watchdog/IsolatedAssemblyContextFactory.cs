using Tgstation.Server.Host.Startup;

namespace Tgstation.Server.Host.Watchdog
{
	/// <inheritdoc />
	sealed class IsolatedAssemblyContextFactory : IIsolatedAssemblyContextFactory
	{
		/// <inheritdoc />
		public IServerFactory CreateIsolatedServerFactory(string assemblyPath) => new IsolatedServerFactory(assemblyPath);
	}
}
