using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Api.Models;

namespace Tgstation.Server.Host.Components.Chat.Providers
{
	/// <summary>
	/// <see cref="IProvider"/> for the Discord app
	/// </summary>
	sealed class DiscordProvider : IProvider
	{
		/// <inheritdoc />
		public bool Connected { get; private set; }

		/// <inheritdoc />
		public string BotMention
		{
			get
			{
				if (!Connected)
					throw new InvalidOperationException("Provider not connected");
				return client.CurrentUser.Mention;
			}
		}

		/// <summary>
		/// The <see cref="ILogger"/> for the <see cref="DiscordProvider"/>
		/// </summary>
		readonly ILogger<DiscordProvider> logger;

		/// <summary>
		/// The <see cref="DiscordSocketClient"/> for the <see cref="DiscordProvider"/>
		/// </summary>
		readonly DiscordSocketClient client;

		/// <summary>
		/// The name used for populating <see cref="Channel.ConnectionName"/>
		/// </summary>
		readonly string connectionName;

		/// <summary>
		/// The token used for connecting to discord
		/// </summary>
		readonly string botToken;
		
		/// <summary>
		/// <see cref="Queue{T}"/> of received <see cref="Message"/>s
		/// </summary>
		readonly Queue<Message> messageQueue;

		/// <summary>
		/// <see cref="List{T}"/> of mapped <see cref="ITextChannel"/> <see cref="IEntity{TId}.Id"/>s
		/// </summary>
		readonly List<ulong> mappedChannels;

		/// <summary>
		/// <see cref="TaskCompletionSource{TResult}"/> that completes while <see cref="messageQueue"/> isn't empty
		/// </summary>
		TaskCompletionSource<object> nextMessage;

		/// <summary>
		/// Construct a <see cref="DiscordProvider"/>
		/// </summary>
		/// <param name="logger">The value of <see cref="logger"/></param>
		/// <param name="connectionName">The value of <see cref="connectionName"/></param>
		/// <param name="botToken">The value of <see cref="botToken"/></param>
		public DiscordProvider(ILogger<DiscordProvider> logger, string connectionName, string botToken)
		{
			this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
			this.connectionName = connectionName ?? throw new ArgumentNullException(nameof(connectionName));
			this.botToken = botToken ?? throw new ArgumentNullException(nameof(botToken));
			client = new DiscordSocketClient();
			client.MessageReceived += Client_MessageReceived;
			nextMessage = new TaskCompletionSource<object>();
			mappedChannels = new List<ulong>();
			messageQueue = new Queue<Message>();
		}

		/// <inheritdoc />
		public void Dispose() => client.Dispose();

		/// <summary>
		/// Handle a message recieved from Discord
		/// </summary>
		/// <param name="e">The <see cref="SocketMessage"/></param>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		Task Client_MessageReceived(SocketMessage e)
		{
			if (e.Author.Id != client.CurrentUser.Id)
				return Task.CompletedTask;

			var pm = e.Channel is IPrivateChannel;

			if (!pm && !mappedChannels.Contains(e.Channel.Id))
				return Task.CompletedTask;

			var result = new Message {
				Content = e.Content,
				User = new User
				{
					RealId = e.Author.Id,
					Channel = new Channel
					{
						RealId = e.Channel.Id,
						IsAdmin = false,
						IsPrivate = true,
						ConnectionName = connectionName,
						FriendlyName = e.Channel.Name
					},
					FriendlyName = e.Author.Username,
					Mention = e.Author.Mention
				}
			};

			lock (this)
			{
				messageQueue.Enqueue(result);
				nextMessage.TrySetResult(null);
			}
			return Task.CompletedTask;
		}

		/// <inheritdoc />
		public async Task<Message> NextMessage(CancellationToken cancellationToken)
		{
			var cancelTcs = new TaskCompletionSource<object>();
			using (cancellationToken.Register(() => cancelTcs.SetCanceled()))
				await Task.WhenAny(nextMessage.Task, cancelTcs.Task).ConfigureAwait(false);
			lock (this)
			{
				var result = messageQueue.Dequeue();
				if (messageQueue.Count == 0)
					nextMessage = new TaskCompletionSource<object>();
				return result;
			}
		}

		/// <inheritdoc />
		public async Task<bool> Connect(CancellationToken cancellationToken)
		{
			if (Connected)
				return true;

			try
			{
				await client.LoginAsync(TokenType.Bot, botToken, true).ConfigureAwait(false);

				cancellationToken.ThrowIfCancellationRequested();

				await client.StartAsync().ConfigureAwait(false);

				var channelsAvailable = new TaskCompletionSource<object>();
				client.Ready += () =>
				{
					channelsAvailable.SetResult(null);
					return Task.CompletedTask;
				};
				using (cancellationToken.Register(() => channelsAvailable.SetCanceled()))
					await channelsAvailable.Task.ConfigureAwait(false);
			}
			catch (Exception e)
			{
				logger.LogWarning("Error connecting to Discord: {0}", e);
				return false;
			}

			Connected = true;
			return true;
		}

		public async Task Disconnect(CancellationToken cancellationToken)
		{
			if (!Connected)
				return;

			try
			{
				await client.StopAsync().ConfigureAwait(false);
				cancellationToken.ThrowIfCancellationRequested();
				await client.LogoutAsync().ConfigureAwait(false);
			}
			catch (Exception e)
			{
				logger.LogWarning("Error disconnecting from discord: {0}", e);
			}
			Connected = false;
		}

		/// <inheritdoc />
		public Task<IReadOnlyList<Channel>> MapChannels(IEnumerable<ChatChannel> channels, CancellationToken cancellationToken)
		{
			if (channels == null)
				throw new ArgumentNullException(nameof(channels));

			if (!Connected)
				throw new InvalidOperationException("Provider not connected!");

			Channel GetChannelForChatChannel(ChatChannel channel)
			{
				if (!channel.DiscordChannelId.HasValue)
					throw new InvalidOperationException("ChatChannel missing DiscordChannelId!");

				var discordChannel = client.GetChannel(channel.DiscordChannelId.Value);

				if (discordChannel == null)
					return null;

				return new Channel
				{
					RealId = discordChannel.Id,
					IsAdmin = channel.IsAdminChannel,
					ConnectionName = connectionName,
					FriendlyName = (discordChannel as ITextChannel)?.Name ?? "UNKNOWN",
					IsPrivate = false
				};
			};

			var enumerator = channels.Select(x => GetChannelForChatChannel(x)).Where(x => x != null);

			lock (this)
			{
				mappedChannels.Clear();
				mappedChannels.AddRange(enumerator.Select(x => x.RealId));
			}

			return Task.FromResult<IReadOnlyList<Channel>>(enumerator.ToList());
		}

		/// <inheritdoc />
		public async Task SendMessage(ulong channelId, string message, CancellationToken cancellationToken) {
			try
			{
				await ((client.GetChannel(channelId) as ITextChannel)?.SendMessageAsync(message, false, null, new RequestOptions
				{
					CancelToken = cancellationToken
				}) ?? Task.CompletedTask).ConfigureAwait(false);
			}
			catch (Exception e)
			{
				logger.LogWarning("Error sending discord message: {0}", e);
			}
		}
	}
}
