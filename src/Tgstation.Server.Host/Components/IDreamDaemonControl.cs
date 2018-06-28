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
		/// Effectively swaps the values of <see cref="CurrentPort"/> and <see cref="NextPort"/> causing DreamDaemon to listen on the new <see cref="CurrentPort"/>
		/// </summary>
		/// <param name="cancellatonToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		Task SwapPorts(CancellatonToken cancellatonToken);
    }
}
