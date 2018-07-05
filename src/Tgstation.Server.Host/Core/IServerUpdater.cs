using System;

namespace Tgstation.Server.Host.Core
{
	/// <summary>
	/// Represents a service that may take an updated <see cref="Host"/> assembly and run it, stopping the current assembly in the process
	/// </summary>
    interface IServerUpdater
    {
		/// <summary>
		/// Run a new <see cref="Host"/> assembly and stop the current one. This will likely trigger all active <see cref="System.Threading.CancellationToken"/>s
		/// </summary>
		/// <param name="updatePath">The path to the new <see cref="Host"/> assembly</param>
		void ApplyUpdate(string updatePath);

		/// <summary>
		/// Register a given <paramref name="action"/> to run before stopping the server for updates
		/// </summary>
		/// <param name="action">The <see cref="Action"/> to run</param>
		void RegisterForUpdate(Action action);
    }
}
