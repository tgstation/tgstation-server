using System;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Host.Components.Deployment;
using Tgstation.Server.Host.Components.Interop.Topic;
using Tgstation.Server.Host.System;

namespace Tgstation.Server.Host.Components.Session
{
	/// <summary>
	/// Handles communication with a DreamDaemon <see cref="IProcess"/>
	/// </summary>
	interface ISessionController : IProcessBase, IRenameNotifyee, IAsyncDisposable
	{
		/// <summary>
		/// A <see cref="Task"/> that completes when DreamDaemon starts pumping the windows message queue after loading a .dmb or when it crashes
		/// </summary>
		Task<LaunchResult> LaunchResult { get; }

		/// <summary>
		/// If the DreamDaemon instance sent a
		/// </summary>
		bool TerminationWasRequested { get; }

		/// <summary>
		/// The DMAPI <see cref="Session.ApiValidationStatus"/>
		/// </summary>
		ApiValidationStatus ApiValidationStatus { get; }

		/// <summary>
		/// The DMAPI <see cref="Version"/>.
		/// </summary>
		Version DMApiVersion { get; }

		/// <summary>
		/// The <see cref="IDmbProvider"/> being used
		/// </summary>
		IDmbProvider Dmb { get; }

		/// <summary>
		/// The current port DreamDaemon is listening on
		/// </summary>
		ushort? Port { get; }

		/// <summary>
		/// If the port should be rotated off when the world reboots
		/// </summary>
		bool ClosePortOnReboot { get; set; }

		/// <summary>
		/// The current <see cref="RebootState"/>
		/// </summary>
		RebootState RebootState { get; }

		/// <summary>
		/// A <see cref="Task"/> that completes when the server calls /world/Reboot()
		/// </summary>
		Task OnReboot { get; }

		/// <summary>
		/// A <see cref="Task"/> that completes when the server calls /world/TgsInitializationsComplete()
		/// </summary>
		Task OnPrime { get; }

		/// <summary>
		/// If the DMAPI may be used this session.
		/// </summary>
		bool DMApiAvailable { get; }

		/// <summary>
		/// Releases the <see cref="IProcess"/> without terminating it. Also calls <see cref="IDisposable.Dispose"/>
		/// </summary>
		/// <returns>A <see cref="Task{TResult}"/> resulting in <see cref="ReattachInformation"/> which can be used to create a new <see cref="ISessionController"/>.</returns>
		Task<ReattachInformation> Release();

		/// <summary>
		/// Sends a command to DreamDaemon through /world/Topic()
		/// </summary>
		/// <param name="parameters">The <see cref="TopicParameters"/> to send.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="TopicResponse"/> of /world/Topic()</returns>
		Task<CombinedTopicResponse> SendCommand(TopicParameters parameters, CancellationToken cancellationToken);

		/// <summary>
		/// Causes the world to start listening on a <paramref name="newPort"/>
		/// </summary>
		/// <param name="newPort">The port to change to</param>
		/// <param name="cancellatonToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in <see langword="true"/> if the operation succeeded, <see langword="false"/> otherwise</returns>
		Task<bool> SetPort(ushort newPort, CancellationToken cancellatonToken);

		/// <summary>
		/// Attempts to change the current <see cref="RebootState"/> to <paramref name="newRebootState"/>
		/// </summary>
		/// <param name="newRebootState">The new <see cref="RebootState"/></param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in <see langword="true"/> if the operation succeeded, <see langword="false"/> otherwise</returns>
		Task<bool> SetRebootState(RebootState newRebootState, CancellationToken cancellationToken);

		/// <summary>
		/// Changes <see cref="RebootState"/> to <see cref="RebootState.Normal"/> without telling the DMAPI
		/// </summary>
		void ResetRebootState();

		/// <summary>
		/// Enables the reading of custom chat commands from the <see cref="ISessionController"/>
		/// </summary>
		void EnableCustomChatCommands();

		/// <summary>
		/// Replace <see cref="Dmb"/> with a given <paramref name="newProvider"/>, disposing the old one.
		/// </summary>
		/// <param name="newProvider">The new <see cref="IDmbProvider"/>.</param>
		void ReplaceDmbProvider(IDmbProvider newProvider);
	}
}
