using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Tgstation.Server.Host.Components.Chat.Providers
{
	/// <summary>
	/// For interacting with a chat service
	/// </summary>
	interface IProvider : IDisposable
	{
		/// <summary>
		/// If the <see cref="IProvider"/>
		/// </summary>
		bool Connected { get; }

		/// <summary>
		/// The <see cref="string"/> that indicates the <see cref="IProvider"/> was mentioned
		/// </summary>
		string BotMention { get; }

		/// <summary>
		/// Get a <see cref="Task{TResult}"/> resulting in the next <see cref="Message"/> the <see cref="IProvider"/> recieves or <see langword="null"/> on a disconnect
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the next available <see cref="Message"/> or <see langword="null"/> if the <see cref="IProvider"/> needed to reconnect.</returns>
		/// <remarks>Note that private messages will come in the form of <see cref="ChannelRepresentation"/>s not returned in <see cref="MapChannels(IEnumerable{Api.Models.ChatChannel}, CancellationToken)"/>. Do not <see cref="IDisposable.Dispose"/> the <see cref="IProvider"/> on continuations run from the returned <see cref="Task"/>.</remarks>
		Task<Message> NextMessage(CancellationToken cancellationToken);

		/// <summary>
		/// Attempt to connect the <see cref="IProvider"/>
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in <see langword="true"/> on success, <see langword="false"/> otherwise</returns>
		Task<bool> Connect(CancellationToken cancellationToken);

		/// <summary>
		/// Gracefully disconnects the provider. Implies a call to <see cref="IDisposable.Dispose"/>
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		Task Disconnect(CancellationToken cancellationToken);

		/// <summary>
		/// Get the <see cref="ChannelRepresentation"/>s for given <paramref name="channels"/>
		/// </summary>
		/// <param name="channels">The <see cref="Api.Models.ChatChannel"/>s to map</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in a <see cref="IReadOnlyCollection{T}"/> of the <see cref="ChannelRepresentation"/>s representing <paramref name="channels"/></returns>
		Task<IReadOnlyCollection<ChannelRepresentation>> MapChannels(IEnumerable<Api.Models.ChatChannel> channels, CancellationToken cancellationToken);

		/// <summary>
		/// Send a message to the <see cref="IProvider"/>
		/// </summary>
		/// <param name="channelId">The <see cref="ChannelRepresentation.RealId"/> to send to</param>
		/// <param name="message">The message contents</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		Task SendMessage(ulong channelId, string message, CancellationToken cancellationToken);

		/// <summary>
		/// Set the interval at which the provider tries to reconnect.
		/// </summary>
		/// <param name="reconnectInterval">The reconnection interval in minutes.</param>
		/// <returns>A <see cref="Task"/> representing the running operation.</returns>
		Task SetReconnectInterval(uint reconnectInterval);
	}
}
