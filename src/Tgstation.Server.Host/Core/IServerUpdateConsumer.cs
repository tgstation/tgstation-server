namespace Tgstation.Server.Host.Core
{
	/// <summary>
	/// Represents a service that may take an updated <see cref="Host"/> assembly and run it, stopping the current assembly in the process
	/// </summary>
    interface IServerUpdateConsumer
    {
		/// <summary>
		/// Run a new <see cref="Host"/> assembly and stop the current one. This will likely trigger all active <see cref="System.Threading.CancellationToken"/>s
		/// </summary>
		/// <param name="updatePath">The path to the new <see cref="Host"/> assembly</param>
		void ApplyUpdate(string updatePath);
    }
}
