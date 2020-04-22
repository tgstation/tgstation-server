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
		static string NormalizeMention(string fromDiscord) => fromDiscord.Replace("!", String.Empty, StringComparison.Ordinal);

		/// <summary>
		/// Construct a <see cref="DiscordProvider"/>
		/// </summary>
		/// <param name="logger">The value of <see cref="Logger"/></param>
		/// <param name="botToken">The value of <see cref="botToken"/></param>
		/// <param name="reconnectInterval">The initial reconnect interval in minutes.</param>
		public DiscordProvider(ILogger<DiscordProvider> logger, string botToken, uint reconnectInterval)
			: base(logger, reconnectInterval)
		{
			this.botToken = botToken ?? throw new ArgumentNullException(nameof(botToken));
			client = new DiscordSocketClient();
			client.MessageReceived += Client_MessageReceived;
			mappedChannels = new List<ulong>();
		}

		/// <inheritdoc />
		public override void Dispose()
		{
			client.Dispose();

			base.Dispose();
		}

		/// <summary>
		/// Handle a message recieved from Discord
		/// </summary>
		/// <param name="e">The <see cref="SocketMessage"/></param>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		async Task Client_MessageReceived(SocketMessage e)
		{
			if (e.Author.Id == client.CurrentUser.Id)
				return;

			var pm = e.Channel is IPrivateChannel;

			if (!pm && !mappedChannels.Contains(e.Channel.Id))
			{
				var mentionedUs = e.MentionedUsers.Any(x => x.Id == client.CurrentUser.Id);
				if (mentionedUs)
				{
					Logger.LogTrace("Ignoring mention from {0} ({1}) by {2} ({3}). Channel not mapped!", e.Channel.Id, e.Channel.Name, e.Author.Id, e.Author.Username);
					await SendMessage(e.Channel.Id, "I do not respond to this channel!", default).ConfigureAwait(false);
				}

				return;
			}

			var result = new Message
			{
				Content = e.Content,
				User = new ChatUser
				{
					RealId = e.Author.Id,
					Channel = new ChatChannel
					{
						RealId = e.Channel.Id,
						IsPrivateChannel = pm,
						ConnectionName = pm ? e.Author.Username : (e.Channel as ITextChannel)?.Guild.Name ?? "UNKNOWN",
						FriendlyName = e.Channel.Name

						// isAdmin and Tag populated by manager
					},
					FriendlyName = e.Author.Username,
					Mention = NormalizeMention(e.Author.Mention)
				}
			};
			EnqueueMessage(result);
		}

		/// <inheritdoc />
		public override async Task<bool> Connect(CancellationToken cancellationToken)
		{
			Logger.LogTrace("Connecting...");
			if (Connected)
			{
				Logger.LogTrace("Already connected not doing connection attempt!");
				return true;
			}

			try
			{
				await client.LoginAsync(TokenType.Bot, botToken, true).ConfigureAwait(false);

				Logger.LogTrace("Logged in.");
				cancellationToken.ThrowIfCancellationRequested();

				await client.StartAsync().ConfigureAwait(false);

				Logger.LogTrace("Started.");

				var channelsAvailable = new TaskCompletionSource<object>();
				client.Ready += () =>
				{
					channelsAvailable.TrySetResult(null);
					return Task.CompletedTask;
				};
				using (cancellationToken.Register(() => channelsAvailable.SetCanceled()))
					await channelsAvailable.Task.ConfigureAwait(false);
				Logger.LogDebug("Connection established!");
			}
			catch (OperationCanceledException)
			{
				throw;
			}
			catch (Exception e)
			{
				Logger.LogWarning("Error connecting to Discord: {0}", e);
				return false;
			}

			return true;
		}

		/// <inheritdoc />
		public override async Task Disconnect(CancellationToken cancellationToken)
		{
			Logger.LogTrace("Disconnecting...");
			if (!Connected)
			{
				Logger.LogTrace("Already disconnected not doing disconnection attempt!");
				return;
			}

			try
			{
				await client.StopAsync().ConfigureAwait(false);
				Logger.LogTrace("Stopped.");
				cancellationToken.ThrowIfCancellationRequested();
				await client.LogoutAsync().ConfigureAwait(false);
				Logger.LogDebug("Disconnected!");
			}
			catch (OperationCanceledException)
			{
				throw;
			}
			catch (Exception e)
			{
				Logger.LogWarning("Error disconnecting from discord: {0}", e);
			}
		}

		/// <inheritdoc />
		public override Task<IReadOnlyCollection<ChatChannel>> MapChannels(IEnumerable<Api.Models.ChatChannel> channels, CancellationToken cancellationToken)
		{
			if (channels == null)
				throw new ArgumentNullException(nameof(channels));

			if (!Connected)
			{
				Logger.LogWarning("Cannot map channels, provider disconnected!");
				return Task.FromResult<IReadOnlyCollection<ChatChannel>>(Array.Empty<ChatChannel>());
			}

			ChatChannel GetModelChannelFromDBChannel(Api.Models.ChatChannel channelFromDB)
			{
				if (!channelFromDB.DiscordChannelId.HasValue)
					throw new InvalidOperationException("ChatChannel missing DiscordChannelId!");

				var channelId = channelFromDB.DiscordChannelId.Value;
				var discordChannel = client.GetChannel(channelId);
				if (discordChannel is ITextChannel textChannel)
				{
					var channelModel = new ChatChannel
					{
						RealId = discordChannel.Id,
						IsAdminChannel = channelFromDB.IsAdminChannel == true,
						ConnectionName = textChannel.Guild.Name,
						FriendlyName = textChannel.Name,
						IsPrivateChannel = false,
						Tag = channelFromDB.Tag
					};
					Logger.LogTrace("Mapped channel {0}: {1}", channelModel.RealId, channelModel.FriendlyName);
					return channelModel;
				}

				Logger.LogWarning("Cound not map channel {0}! Incorrect type: {1}", channelId, discordChannel.GetType());
				return null;
			}

			var enumerator = channels.Select(x => GetModelChannelFromDBChannel(x)).Where(x => x != null).ToList();

			lock (this)
			{
				mappedChannels.Clear();
				mappedChannels.AddRange(enumerator.Select(x => x.RealId));
			}

			return Task.FromResult<IReadOnlyCollection<ChatChannel>>(enumerator);
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
				Logger.LogWarning("Error sending discord message: {0}", e);
			}
		}
	}
}
