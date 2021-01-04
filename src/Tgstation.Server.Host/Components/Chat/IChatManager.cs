using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Api.Models.Internal;

namespace Tgstation.Server.Host.Components.Chat
{
	/// <summary>
	/// For managing connected chat services
	/// </summary>
	public interface IChatManager : IHostedService, IAsyncDisposable
	{
		/// <summary>
		/// Registers a <paramref name="customCommandHandler"/> to use
		/// </summary>
		/// <param name="customCommandHandler">A <see cref="ICustomCommandHandler"/></param>
		void RegisterCommandHandler(ICustomCommandHandler customCommandHandler);

		/// <summary>
		/// Change chat settings. If the <see cref="Api.Models.EntityId.Id"/> is not currently in use, a new connection will be made instead
		/// </summary>
		/// <param name="newSettings">The new <see cref="Models.ChatBot"/></param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task"/> representing the running operation. Will complete immediately if the <see cref="ChatBot.Enabled"/> property of <paramref name="newSettings"/> is <see langword="false"/></returns>
		Task ChangeSettings(Models.ChatBot newSettings, CancellationToken cancellationToken);

		/// <summary>
		/// Disconnects and deletes a given connection
		/// </summary>
		/// <param name="connectionId">The <see cref="Api.Models.EntityId.Id"/> of the connection</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		Task DeleteConnection(long connectionId, CancellationToken cancellationToken);

		/// <summary>
		/// Change chat channels
		/// </summary>
		/// <param name="connectionId">The <see cref="Api.Models.EntityId.Id"/> of the connection</param>
		/// <param name="newChannels">An <see cref="IEnumerable{T}"/> of the new list of <see cref="Api.Models.ChatChannel"/>s</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		Task ChangeChannels(long connectionId, IEnumerable<Api.Models.ChatChannel> newChannels, CancellationToken cancellationToken);

		/// <summary>
		/// Queue a chat <paramref name="message"/> to a given set of <paramref name="channelIds"/>.
		/// </summary>
		/// <param name="message">The message being sent.</param>
		/// <param name="channelIds">The <see cref="Models.ChatChannel.Id"/>s of the <see cref="Models.ChatChannel"/>s to send to.</param>
		void QueueMessage(string message, IEnumerable<ulong> channelIds);

		/// <summary>
		/// Queue a chat <paramref name="message"/> to configured watchdog channels.
		/// </summary>
		/// <param name="message">The message being sent.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task"/> representing the running operation.</returns>
		Task QueueWatchdogMessage(string message, CancellationToken cancellationToken);

		/// <summary>
		/// Send the message for a deployment to configured deployment channels.
		/// </summary>
		/// <param name="revisionInformation">The <see cref="RevisionInformation"/> of the deployment.</param>
		/// <param name="byondVersion">The BYOND <see cref="Version"/> of the deployment.</param>
		/// <param name="estimatedCompletionTime">The optional <see cref="DateTimeOffset"/> the deployment is expected to be completed at.</param>
		/// <param name="gitHubOwner">The repository GitHub owner, if any.</param>
		/// <param name="gitHubRepo">The repository GitHub name, if any.</param>
		/// <param name="localCommitPushed"><see langword="true"/> if the local deployment commit was pushed to the remote repository.</param>
		/// <returns>An <see cref="Action{T1, T2}"/> to call to update the message at the deployment's conclusion. Parameters: Error message if any, DreamMaker output if any.</returns>
		Action<string, string> QueueDeploymentMessage(
			Models.RevisionInformation revisionInformation,
			Version byondVersion,
			DateTimeOffset? estimatedCompletionTime,
			string gitHubOwner,
			string gitHubRepo,
			bool localCommitPushed);

		/// <summary>
		/// Start tracking <see cref="Commands.CustomCommand"/>s and <see cref="ChannelRepresentation"/>s.
		/// </summary>
		/// <returns>A new <see cref="IChatTrackingContext"/>.</returns>
		IChatTrackingContext CreateTrackingContext();
	}
}
