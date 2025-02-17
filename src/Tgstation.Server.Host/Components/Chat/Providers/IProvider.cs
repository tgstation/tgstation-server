using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Tgstation.Server.Api.Models.Internal;
using Tgstation.Server.Host.Components.Interop;
using Tgstation.Server.Host.Models;

namespace Tgstation.Server.Host.Components.Chat.Providers
{
	/// <summary>
	/// For interacting with a chat service.
	/// </summary>
	interface IProvider : IAsyncDisposable
	{
		/// <summary>
		/// If the <see cref="IProvider"/> is currently connected.
		/// </summary>
		bool Connected { get; }

		/// <summary>
		/// If the <see cref="IProvider"/> was disposed.
		/// </summary>
		bool Disposed { get; }

		/// <summary>
		/// The <see cref="string"/> that indicates the <see cref="IProvider"/> was mentioned.
		/// </summary>
		string BotMention { get; }

		/// <summary>
		/// A <see cref="Task"/> that completes once the <see cref="IProvider"/> finishes it's first connection attempt regardless of success.
		/// </summary>
		Task InitialConnectionJob { get; }

		/// <summary>
		/// Indicate to the provider that at least one <see cref="MapChannels(IEnumerable{ChatChannel}, CancellationToken)"/> call has successfully completed.
		/// </summary>
		void InitialMappingComplete();

		/// <summary>
		/// Get a <see cref="Task{TResult}"/> resulting in the next <see cref="Message"/> the <see cref="IProvider"/> receives or <see langword="null"/> on a disconnect.
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the next available <see cref="Message"/> or <see langword="null"/> if the <see cref="IProvider"/> needed to reconnect.</returns>
		/// <remarks>Note that private messages will come in the form of <see cref="ChannelRepresentation"/>s not returned in <see cref="MapChannels(IEnumerable{ChatChannel}, CancellationToken)"/>.</remarks>
		Task<Message?> NextMessage(CancellationToken cancellationToken);

		/// <summary>
		/// Gracefully disconnects the provider. Permanently stops the reconnection timer.
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask"/> representing the running operation.</returns>
		ValueTask Disconnect(CancellationToken cancellationToken);

		/// <summary>
		/// Get the <see cref="ChannelRepresentation"/>s for given <paramref name="channels"/>.
		/// </summary>
		/// <param name="channels">The <see cref="Api.Models.ChatChannel"/>s to map.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in a <see cref="Dictionary{TKey, TValue}"/> of the <see cref="ChatChannel"/>'s <see cref="ChannelRepresentation"/>s representing <paramref name="channels"/>.</returns>
		ValueTask<Dictionary<ChatChannel, IEnumerable<ChannelRepresentation>>> MapChannels(IEnumerable<ChatChannel> channels, CancellationToken cancellationToken);

		/// <summary>
		/// Send a message to the <see cref="IProvider"/>.
		/// </summary>
		/// <param name="replyTo">The optional <see cref="Message"/> to reply to.</param>
		/// <param name="message">The <see cref="MessageContent"/>.</param>
		/// <param name="channelId">The <see cref="ChannelRepresentation.RealId"/> to send to.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask"/> representing the running operation.</returns>
		ValueTask SendMessage(Message? replyTo, MessageContent message, ulong channelId, CancellationToken cancellationToken);

		/// <summary>
		/// Set the interval at which the provider starts jobs to try to reconnect.
		/// </summary>
		/// <param name="reconnectInterval">The reconnection interval in minutes.</param>
		/// <param name="connectNow">If a connection attempt should be made now.</param>
		/// <returns>A <see cref="Task"/> representing the running operation.</returns>
		Task SetReconnectInterval(uint reconnectInterval, bool connectNow);

		/// <summary>
		/// Send the message for a deployment.
		/// </summary>
		/// <param name="revisionInformation">The <see cref="RevisionInformation"/> of the deployment.</param>
		/// <param name="previousRevisionInformation">The <see cref="RevisionInformation"/> of the previous deployment if any.</param>
		/// <param name="engineVersion">The <see cref="Api.Models.EngineVersion"/> of the deployment.</param>
		/// <param name="estimatedCompletionTime">The optional <see cref="DateTimeOffset"/> the deployment is expected to be completed at.</param>
		/// <param name="gitHubOwner">The repository GitHub owner, if any.</param>
		/// <param name="gitHubRepo">The repository GitHub name, if any.</param>
		/// <param name="channelId">The <see cref="ChannelRepresentation.RealId"/> to send to.</param>
		/// <param name="localCommitPushed"><see langword="true"/> if the local deployment commit was pushed to the remote repository.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in a <see cref="Func{T1, T2, TResult}"/> to call to update the message at the deployment's conclusion. Parameters: Error message if any, DreamMaker output if any. Returns another callback which should be called to mark the deployment as active.</returns>
		ValueTask<Func<string?, string, ValueTask<Func<bool, ValueTask>>>> SendUpdateMessage(
			Models.RevisionInformation revisionInformation,
			Models.RevisionInformation? previousRevisionInformation,
			Api.Models.EngineVersion engineVersion,
			DateTimeOffset? estimatedCompletionTime,
			string? gitHubOwner,
			string? gitHubRepo,
			ulong channelId,
			bool localCommitPushed,
			CancellationToken cancellationToken);
	}
}
