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
	sealed class DiscordProvider : Provider
	{
		/// <inheritdoc />
		public override bool Connected => client.ConnectionState == ConnectionState.Connected;

		/// <inheritdoc />
		public override string BotMention
		{
			get
			{
				if (!Connected)
					throw new InvalidOperationException("Provider not connected");
				return NormalizeMention(client.CurrentUser.Mention);
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
		/// The token used for connecting to discord
		/// </summary>
		readonly string botToken;

		/// <summary>
		/// <see cref="List{T}"/> of mapped <see cref="ITextChannel"/> <see cref="IEntity{TId}.Id"/>s
		/// </summary>
		readonly List<ulong> mappedChannels;

		/// <summary>
		/// Normalize a discord mention string
		/// </summary>
		/// <param name="fromDiscord">The mention <see cref="string"/> provided by the Discord library</param>
		/// <returns>The normalized mention <see cref="string"/></returns>
		static string NormalizeMention(string fromDiscord) => fromDiscord.Replace("!", "", StringComparison.Ordinal);

		/// <summary>
		/// Construct a <see cref="DiscordProvider"/>
		/// </summary>
		/// <param name="logger">The value of <see cref="logger"/></param>
		/// <param name="botToken">The value of <see cref="botToken"/></param>
		public DiscordProvider(ILogger<DiscordProvider> logger, string botToken)
		{
			this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
			this.botToken = botToken ?? throw new ArgumentNullException(nameof(botToken));
			client = new DiscordSocketClient();
			client.MessageReceived += Client_MessageReceived;
			mappedChannels = new List<ulong>();
		}

		/// <inheritdoc />
		public override void Dispose() => client.Dispose();

		/// <summary>
		/// Handle a message recieved from Discord
		/// </summary>
		/// <param name="e">The <see cref="SocketMessage"/></param>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		Task Client_MessageReceived(SocketMessage e)
		{
			if (e.Author.Id == client.CurrentUser.Id)
				return Task.CompletedTask;

			var pm = e.Channel is IPrivateChannel;

			if (!pm && !mappedChannels.Contains(e.Channel.Id))
				return e.MentionedUsers.Any(x => x.Id == client.CurrentUser.Id) ? SendMessage(e.Channel.Id, "I do not respond to this channel!", default) : Task.CompletedTask;

			var result = new Message
			{
				Content = e.Content,
				User = new User
				{
					RealId = e.Author.Id,
					Channel = new Channel
					{
						RealId = e.Channel.Id,
						IsPrivate = pm,
						ConnectionName = pm ? e.Author.Username : (e.Channel as ITextChannel)?.Guild.Name ?? "UNKNOWN",
						FriendlyName = e.Channel.Name
						//isAdmin and Tag populated by manager
					},
					FriendlyName = e.Author.Username,
					Mention = NormalizeMention(e.Author.Mention)
				}
			};
			EnqueueMessage(result);
			return Task.CompletedTask;
		}

		/// <inheritdoc />
		public override async Task<bool> Connect(CancellationToken cancellationToken)
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
					channelsAvailable.TrySetResult(null);
					return Task.CompletedTask;
				};
				using (cancellationToken.Register(() => channelsAvailable.SetCanceled()))
					await channelsAvailable.Task.ConfigureAwait(false);
			}
			catch (OperationCanceledException)
			{
				throw;
			}
			catch (Exception e)
			{
				logger.LogWarning("Error connecting to Discord: {0}", e);
				return false;
			}

			return true;
		}

		public override async Task Disconnect(CancellationToken cancellationToken)
		{
			if (!Connected)
				return;

			try
			{
				await client.StopAsync().ConfigureAwait(false);
				cancellationToken.ThrowIfCancellationRequested();
				await client.LogoutAsync().ConfigureAwait(false);
			}
			catch (OperationCanceledException)
			{
				throw;
			}
			catch (Exception e)
			{
				logger.LogWarning("Error disconnecting from discord: {0}", e);
			}
		}

		/// <inheritdoc />
		public override Task<IReadOnlyList<Channel>> MapChannels(IEnumerable<ChatChannel> channels, CancellationToken cancellationToken)
		{
			if (channels == null)
				throw new ArgumentNullException(nameof(channels));

			if (!Connected)
				throw new InvalidOperationException("Provider not connected!");

			Channel GetChannelForChatChannel(ChatChannel channel)
			{
				if (!channel.DiscordChannelId.HasValue)
					throw new InvalidOperationException("ChatChannel missing DiscordChannelId!");

				if (!(client.GetChannel(channel.DiscordChannelId.Value) is ITextChannel discordChannel))
					return null;

				return new Channel
				{
					RealId = discordChannel.Id,
					IsAdmin = channel.IsAdminChannel == true,
					ConnectionName = discordChannel.Guild.Name,
					FriendlyName = discordChannel.Name,
					IsPrivate = false,
					Tag = channel.Tag
				};
			};

			var enumerator = channels.Select(x => GetChannelForChatChannel(x)).Where(x => x != null).ToList();

			lock (this)
			{
				mappedChannels.Clear();
				mappedChannels.AddRange(enumerator.Select(x => x.RealId));
			}

			return Task.FromResult<IReadOnlyList<Channel>>(enumerator);
		}

		/// <inheritdoc />
		public override async Task SendMessage(ulong channelId, string message, CancellationToken cancellationToken)
		{
			try
			{
				var channel = client.GetChannel(channelId) as IMessageChannel;
				await (channel?.SendMessageAsync(message, false, null, new RequestOptions
				{
					CancelToken = cancellationToken
				}) ?? Task.CompletedTask).ConfigureAwait(false);
			}
			catch (OperationCanceledException)
			{
				throw;
			}
			catch (Exception e)
			{
				logger.LogWarning("Error sending discord message: {0}", e);
			}
		}
	}
}
