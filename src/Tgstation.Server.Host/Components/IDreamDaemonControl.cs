using System;
using System.Threading;
using System.Threading.Tasks;

namespace Tgstation.Server.Host.Components
{
	/// <summary>
	/// Handles communication with a <see cref="IDreamDaemonSession"/>
	/// </summary>
    interface IDreamDaemonControl : IDisposable
    {
		/// <summary>
		/// If the <see cref="IDmbProvider.PrimaryDirectory"/> of <see cref="Dmb"/> is being used
		/// </summary>
		bool IsPrimary { get; }

		/// <summary>
		/// The <see cref="IDmbProvider"/> being used
		/// </summary>
		IDmbProvider Dmb { get; }

		/// <summary>
		/// The current port DreamDaemon is listening on
		/// </summary>
		ushort Port { get; }

		/// <summary>
		/// Releases the <see cref="IDreamDaemonSession"/> without terminating it. Also calls <see cref="IDisposable.Dispose"/>
		/// </summary>
		/// <returns><see cref="DreamDaemonReattachInformation"/> which can be used to create a new <see cref="IDreamDaemonControl"/> similar to this one</returns>
		DreamDaemonReattachInformation Release();

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
    }
}
