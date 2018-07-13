using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Api.Models;

namespace Tgstation.Server.Host.Components.Chat
{
	/// <inheritdoc />
	sealed class Chat : IChat
	{
		/// <inheritdoc />
		public bool IrcConnected => throw new System.NotImplementedException();

		/// <inheritdoc />
		public bool DiscordConnected => throw new System.NotImplementedException();

		/// <inheritdoc />
		public Task ChangeChannels(IEnumerable<ChatChannel> newChannels, CancellationToken cancellationToken)
		{
			throw new System.NotImplementedException();
		}

		/// <inheritdoc />
		public Task ChangeSettings(Api.Models.Internal.ChatSettings newSettings, CancellationToken cancellationToken)
		{
			throw new System.NotImplementedException();
		}

		/// <inheritdoc />
		public Task SendMessage(string message, IEnumerable<long> channelIds, CancellationToken cancellationToken)
		{
			throw new System.NotImplementedException();
		}

		/// <inheritdoc />
		public Task SendWatchdogMessage(string message, CancellationToken cancellationToken)
		{
			throw new System.NotImplementedException();
		}

		/// <inheritdoc />
		public Task StartAsync(CancellationToken cancellationToken)
		{
			throw new System.NotImplementedException();
		}

		/// <inheritdoc />
		public Task StopAsync(CancellationToken cancellationToken)
		{
			throw new System.NotImplementedException();
		}

		/// <inheritdoc />
		public Task<IChatJsonTrackingContext> TrackJsons(string basePath, string channelsJsonName, string commandsJsonName, CancellationToken cancellationToken)
		{
			throw new System.NotImplementedException();
		}
	}
}
