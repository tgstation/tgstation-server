using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Host.Models;
using Tgstation.Server.Host.System;

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
				return NormalizeMentions(client.CurrentUser.Mention);
			}
		}

		/// <summary>
		/// The <see cref="IAssemblyInformationProvider"/> for the <see cref="DiscordProvider"/>.
		/// </summary>
		readonly IAssemblyInformationProvider assemblyInformationProvider;

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
		static string NormalizeMentions(string fromDiscord) => fromDiscord.Replace("<!@", "<@", StringComparison.Ordinal);

		/// <summary>
		/// Construct a <see cref="DiscordProvider"/>
		/// </summary>
		/// <param name="assemblyInformationProvider">The value of <see cref="assemblyInformationProvider"/>.</param>
		/// <param name="logger">The value of <see cref="Logger"/></param>
		/// <param name="botToken">The value of <see cref="botToken"/></param>
		/// <param name="reconnectInterval">The initial reconnect interval in minutes.</param>
		public DiscordProvider(
			IAssemblyInformationProvider assemblyInformationProvider,
			ILogger<DiscordProvider> logger,
			string botToken,
			uint reconnectInterval)
			: base(logger, reconnectInterval)
		{
			this.assemblyInformationProvider = assemblyInformationProvider ?? throw new ArgumentNullException(nameof(assemblyInformationProvider));
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
				Content = NormalizeMentions(e.Content),
				User = new ChatUser
				{
					RealId = e.Author.Id,
					Channel = new ChannelRepresentation
					{
						RealId = e.Channel.Id,
						IsPrivateChannel = pm,
						ConnectionName = pm ? e.Author.Username : (e.Channel as ITextChannel)?.Guild.Name ?? "UNKNOWN",
						FriendlyName = e.Channel.Name

						// isAdmin and Tag populated by manager
					},
					FriendlyName = e.Author.Username,
					Mention = NormalizeMentions(e.Author.Mention)
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
		protected override async Task DisconnectImpl(CancellationToken cancellationToken)
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
		public override Task<IReadOnlyCollection<ChannelRepresentation>> MapChannels(IEnumerable<Api.Models.ChatChannel> channels, CancellationToken cancellationToken)
		{
			if (channels == null)
				throw new ArgumentNullException(nameof(channels));

			if (!Connected)
			{
				Logger.LogWarning("Cannot map channels, provider disconnected!");
				return Task.FromResult<IReadOnlyCollection<ChannelRepresentation>>(Array.Empty<ChannelRepresentation>());
			}

			ChannelRepresentation GetModelChannelFromDBChannel(Api.Models.ChatChannel channelFromDB)
			{
				if (!channelFromDB.DiscordChannelId.HasValue)
					throw new InvalidOperationException("ChatChannel missing DiscordChannelId!");

				var channelId = channelFromDB.DiscordChannelId.Value;
				var discordChannel = client.GetChannel(channelId);
				if (discordChannel is ITextChannel textChannel)
				{
					var channelModel = new ChannelRepresentation
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

				Logger.LogWarning("Cound not map channel {0}! Incorrect type: {1}", channelId, discordChannel?.GetType());
				return null;
			}

			var enumerator = channels.Select(x => GetModelChannelFromDBChannel(x)).Where(x => x != null).ToList();

			lock (client)
			{
				mappedChannels.Clear();
				mappedChannels.AddRange(enumerator.Select(x => x.RealId));
			}

			return Task.FromResult<IReadOnlyCollection<ChannelRepresentation>>(enumerator);
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

		/// <inheritdoc />
		public override async Task<Func<string, string, Task>> SendUpdateMessage(
			RevisionInformation revisionInformation,
			Version byondVersion,
			DateTimeOffset? estimatedCompletionTime,
			string gitHubOwner,
			string gitHubRepo,
			ulong channelId,
			bool localCommitPushed,
			CancellationToken cancellationToken)
		{
			bool gitHub = gitHubOwner != null && gitHubRepo != null;

			localCommitPushed |= revisionInformation.CommitSha == revisionInformation.OriginCommitSha;

			var fields = new List<EmbedFieldBuilder>
			{
				new EmbedFieldBuilder
				{
					Name = "BYOND Version",
					Value = $"{byondVersion.Major}.{byondVersion.Minor}",
					IsInline = true
				},
				new EmbedFieldBuilder
				{
					Name = "Local Commit",
					Value = localCommitPushed && gitHub
						? $"[{revisionInformation.CommitSha.Substring(0, 7)}](https://github.com/{gitHubOwner}/{gitHubRepo}/commit/{revisionInformation.CommitSha})"
						: revisionInformation.CommitSha.Substring(0, 7),
					IsInline = true
				},
				new EmbedFieldBuilder
				{
					Name = "Branch Commit",
					Value = gitHub
						? $"[{revisionInformation.OriginCommitSha.Substring(0, 7)}](https://github.com/{gitHubOwner}/{gitHubRepo}/commit/{revisionInformation.OriginCommitSha})"
						: revisionInformation.OriginCommitSha.Substring(0, 7),
					IsInline = true
				}
			};

			fields.AddRange((revisionInformation.ActiveTestMerges ?? Enumerable.Empty<RevInfoTestMerge>())
				.Select(x => x.TestMerge)
				.Select(x => new EmbedFieldBuilder
				{
					Name = $"#{x.Number}",
					Value = $"[{x.TitleAtMerge}]({x.Url}) by _[@{x.Author}](https://github.com/{x.Author})_{Environment.NewLine}Commit: [{x.PullRequestRevision.Substring(0, 7)}](https://github.com/{gitHubOwner}/{gitHubRepo}/commit/{x.PullRequestRevision}){(String.IsNullOrWhiteSpace(x.Comment) ? String.Empty : $"{Environment.NewLine}_**{x.Comment}**_")}"
				}));

			var builder = new EmbedBuilder
			{
				Author = new EmbedAuthorBuilder
				{
					Name = assemblyInformationProvider.VersionPrefix,
					Url = "https://github.com/tgstation/tgstation-server",
					IconUrl = "https://avatars0.githubusercontent.com/u/1363778?s=280&v=4"
				},
				Color = Color.Gold,
				Description = "TGS has begun deploying active repository code to production.",
				Fields = fields,
				Title = "Code Deployment",
				Footer = new EmbedFooterBuilder
				{
					Text = "In progress... ETA"
				},
				Timestamp = estimatedCompletionTime
			};

			Logger.LogTrace("Attempting to post deploy embed to channel {0}...", channelId);
			if (!(client.GetChannel(channelId) is IMessageChannel channel))
			{
				Logger.LogTrace("Channel ID {0} does not exist or is not an IMessageChannel!", channelId);
				return (errorMessage, dreamMakerOutput) => Task.CompletedTask;
			}

			var message = await channel.SendMessageAsync(
				"DM: Deployment in Progress...",
				false,
				builder.Build(),
				new RequestOptions
				{
					CancelToken = cancellationToken
				})
				.ConfigureAwait(false);

			return async (errorMessage, dreamMakerOutput) =>
			{
				var completionString = errorMessage == null ? "Succeeded" : "Failed";
				builder.Footer.Text = completionString;
				builder.Color = errorMessage == null ? Color.Green : Color.Red;
				builder.Timestamp = DateTimeOffset.Now;
				builder.Description = errorMessage == null
					? "The deployment completed successfully and will be available at the next server reboot."
					: "The deployment failed.";

				if (dreamMakerOutput != null)
					builder.AddField(new EmbedFieldBuilder
					{
						Name = "DreamMaker Output",
						Value = $"```{Environment.NewLine}{dreamMakerOutput}{Environment.NewLine}```"
					});

				if (errorMessage != null)
					builder.AddField(new EmbedFieldBuilder
					{
						Name = "Error Message",
						Value = errorMessage
					});

				try
				{
					await message.ModifyAsync(
						props => props.Embed = builder.Build())
						.ConfigureAwait(false);
				}
				catch (Exception ex)
				{
					Logger.LogWarning("Updating deploy embed {0} failed, attempting new post! Exception: {1}", message.Id, ex);
					try
					{
						await channel.SendMessageAsync(
							$"DM: Deployment {completionString}!",
							false,
							builder.Build())
							.ConfigureAwait(false);
					}
					catch (Exception ex2)
					{
						Logger.LogWarning("Posting completion deploy embed failed! Exception: {0}", ex2);
					}
				}
			};
		}
	}
}
