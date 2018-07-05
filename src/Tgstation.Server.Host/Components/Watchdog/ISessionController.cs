using System.Threading;
using System.Threading.Tasks;

namespace Tgstation.Server.Host.Components.Watchdog
{
	/// <summary>
	/// Handles communication with a <see cref="ISession"/>
	/// </summary>
	interface ISessionController : ISessionBase
	{
		/// <summary>
		/// If the <see cref="IDmbProvider.PrimaryDirectory"/> of <see cref="Dmb"/> is being used
		/// </summary>
		bool IsPrimary { get; }

		/// <summary>
		/// If the DMAPI was validated. This field may only be access once <see cref="ISessionBase.Lifetime"/> completes
		/// </summary>
		bool ApiValidated { get; }

		/// <summary>
		/// The <see cref="IDmbProvider"/> being used
		/// </summary>
		IDmbProvider Dmb { get; }

		/// <summary>
		/// The current port DreamDaemon is listening on
		/// </summary>
		ushort? Port { get; }

		/// <summary>
		/// The current <see cref="RebootState"/>
		/// </summary>
		RebootState RebootState { get; }

		/// <summary>
		/// If the port should close when /world/Reboot() is called. Defaults to <see langword="true"/> 
		/// </summary>
		bool ClosePortOnReboot { get; set; }

		/// <summary>
		/// A <see cref="Task"/> that completes when the server calls /world/Reboot()
		/// </summary>
		Task OnReboot { get; }

		/// <summary>
		/// Releases the <see cref="ISession"/> without terminating it. Also calls <see cref="IDisposable.Dispose"/>
		/// </summary>
		/// <returns><see cref="ReattachInformation"/> which can be used to create a new <see cref="ISessionController"/> similar to this one</returns>
		ReattachInformation Release();

		/// <summary>
		/// Sends a command to DreamDaemon through /world/Topic()
		/// </summary>
		/// <param name="cancellatonToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the result of /world/Topic()</returns>
		Task<string> SendCommand(string command, CancellationToken cancellationToken);

		/// <summary>
		/// Closes the world's port
		/// </summary>
		/// <param name="cancellatonToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in <see langword="true"/> if the operation succeeded, <see langword="false"/> otherwise</returns>
		Task<bool> ClosePort(CancellationToken cancellationToken);

		/// <summary>
		/// Causes the world to start listening on a <paramref name="newPort"/>
		/// </summary>
		/// <param name="cancellatonToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in <see langword="true"/> if the operation succeeded, <see langword="false"/> otherwise</returns>
		Task<bool> SetPort(ushort newPort, CancellationToken cancellatonToken);

		/// <summary>
		/// Attempts to change the current <see cref="RebootState"/> to <paramref name="newRebootState"/>
		/// </summary>
		/// <param name="newRebootState">The new <see cref="RebootState"/></param>
		/// <param name="cancellatonToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in <see langword="true"/> if the operation succeeded, <see langword="false"/> otherwise</returns>
		Task<bool> SetRebootState(RebootState newRebootState, CancellationToken cancellationToken);
    }
}
