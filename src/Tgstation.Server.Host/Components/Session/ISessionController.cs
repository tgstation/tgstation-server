using System;
using System.Threading;
using System.Threading.Tasks;

using Tgstation.Server.Api.Models;
using Tgstation.Server.Host.Components.Deployment;
using Tgstation.Server.Host.Components.Interop.Topic;
using Tgstation.Server.Host.System;

namespace Tgstation.Server.Host.Components.Session
{
	/// <summary>
	/// Handles communication with a DreamDaemon <see cref="IProcess"/>.
	/// </summary>
	interface ISessionController : IProcessBase, IRenameNotifyee, IAsyncDisposable
	{
		/// <summary>
		/// A <see cref="Task"/> that completes when DreamDaemon starts pumping the windows message queue after loading a .dmb or when it crashes.
		/// </summary>
		Task<LaunchResult> LaunchResult { get; }

		/// <summary>
		/// If the DreamDaemon instance sent a.
		/// </summary>
		bool TerminationWasIntentional { get; }

		/// <summary>
		/// The DMAPI <see cref="Session.ApiValidationStatus"/>.
		/// </summary>
		ApiValidationStatus ApiValidationStatus { get; }

		/// <summary>
		/// The DMAPI <see cref="Version"/>.
		/// </summary>
		Version? DMApiVersion { get; }

		/// <summary>
		/// Gets the <see cref="CompileJob"/> associated with the <see cref="ISessionController"/>.
		/// </summary>
		Models.CompileJob CompileJob { get; }

		/// <summary>
		/// Gets the <see cref="Api.Models.EngineVersion"/> associated with the <see cref="ISessionController"/>.
		/// </summary>
		EngineVersion EngineVersion { get; }

		/// <summary>
		/// Gets the <see cref="Session.ReattachInformation"/> associated with the <see cref="ISessionController"/>.
		/// </summary>
		ReattachInformation ReattachInformation { get; }

		/// <summary>
		/// If the <see cref="ISessionController"/> is currently processing a bridge request from TgsReboot().
		/// </summary>
		bool ProcessingRebootBridgeRequest { get; }

		/// <summary>
		/// The current <see cref="RebootState"/>.
		/// </summary>
		RebootState RebootState { get; }

		/// <summary>
		/// A <see cref="Task"/> that completes when the server calls /world/TgsNew().
		/// </summary>
		Task OnStartup { get; }

		/// <summary>
		/// A <see cref="Task"/> that completes when the server calls /world/TgsReboot().
		/// </summary>
		Task OnReboot { get; }

		/// <summary>
		/// A <see cref="Task"/> that must complete before a TgsReboot() bridge request can complete.
		/// </summary>
		Task RebootGate { set; }

		/// <summary>
		/// A <see cref="Task"/> that completes when the server calls /world/TgsInitializationComplete().
		/// </summary>
		Task OnPrime { get; }

		/// <summary>
		/// If the DMAPI may be used this session.
		/// </summary>
		bool DMApiAvailable { get; }

		/// <summary>
		/// The file extension to use for process dumps created from this session.
		/// </summary>
		string DumpFileExtension { get; }

		/// <summary>
		/// The number of times a startup bridge request has been received. <see langword="null"/> if <see cref="DMApiAvailable"/> is <see langword="false"/>.
		/// </summary>
		long? StartupBridgeRequestsReceived { get; }

		/// <summary>
		/// Releases the <see cref="IProcess"/> without terminating it. Also calls <see cref="IDisposable.Dispose"/>.
		/// </summary>
		/// <returns>A <see cref="ValueTask"/> representing the running operation.</returns>
		ValueTask Release();

		/// <summary>
		/// Sends a command to DreamDaemon through /world/Topic().
		/// </summary>
		/// <param name="parameters">The <see cref="TopicParameters"/> to send.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in the <see cref="TopicResponse"/> of /world/Topic().</returns>
		ValueTask<TopicResponse?> SendCommand(TopicParameters parameters, CancellationToken cancellationToken);

		/// <summary>
		/// Attempts to change the current <see cref="RebootState"/> to <paramref name="newRebootState"/>.
		/// </summary>
		/// <param name="newRebootState">The new <see cref="RebootState"/>.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in <see langword="true"/> if the operation succeeded, <see langword="false"/> otherwise.</returns>
		ValueTask<bool> SetRebootState(RebootState newRebootState, CancellationToken cancellationToken);

		/// <summary>
		/// Changes <see cref="RebootState"/> to <see cref="RebootState.Normal"/> without telling the DMAPI.
		/// </summary>
		void ResetRebootState();

		/// <summary>
		/// Replace the <see cref="IDmbProvider"/> in use with a given <paramref name="newProvider"/>, disposing the old one.
		/// </summary>
		/// <param name="newProvider">The new <see cref="IDmbProvider"/>.</param>
		/// <returns>An <see cref="IAsyncDisposable"/> to be disposed once certain that the original <see cref="IDmbProvider"/> is no longer in use.</returns>
		IAsyncDisposable ReplaceDmbProvider(IDmbProvider newProvider);
	}
}
