using System;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Host.IO;

namespace Tgstation.Server.Host.Core
{
	/// <summary>
	/// Represents a service that may take an updated <see cref="Host"/> assembly and run it, stopping the current assembly in the process
	/// </summary>
	public interface IServerControl
	{
		/// <summary>
		/// Run a new <see cref="Host"/> assembly and stop the current one. This will likely trigger all active <see cref="CancellationToken"/>s
		/// </summary>
		/// <param name="version">The <see cref="Version"/> the <see cref="IServerControl"/> is updating to</param>
		/// <param name="updateZipData">The <see cref="byte"/>s of the .zip file that contains the new <see cref="Host"/> assembly</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <param name="ioManager">The <see cref="IIOManager"/> for the operation</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in <see langword="true"/> if live updates are supported, <see langword="false"/> otherwise</returns>
		Task<bool> ApplyUpdate(Version version, byte[] updateZipData, IIOManager ioManager, CancellationToken cancellationToken);

		/// <summary>
		/// Register a given <paramref name="handler"/> to run before stopping the server for a restart
		/// </summary>
		/// <param name="handler">The <see cref="IRestartHandler"/> to register</param>
		/// <returns>A new <see cref="IRestartRegistration"/> representing the scope of the registration</returns>
		IRestartRegistration RegisterForRestart(IRestartHandler handler);

		/// <summary>
		/// Restarts the <see cref="Host"/>
		/// </summary>
		/// <returns>A <see cref="Task{TResult}"/> resulting in <see langword="true"/> if live restarts are supported, <see langword="false"/> otherwise</returns>
		Task<bool> Restart();
	}
}
