using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Remora.Discord.API.Abstractions.Gateway.Events;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Discord.Core;
using Remora.Discord.Gateway;
using Remora.Discord.Gateway.Extensions;
using Remora.Results;

using Tgstation.Server.Api.Models;
using Tgstation.Server.Host.Jobs;
using Tgstation.Server.Host.Models;
using Tgstation.Server.Host.System;

namespace Tgstation.Server.Host.Components.Chat.Providers
{
	/// <summary>
	/// <see cref="IProvider"/> for the Discord app.
	/// </summary>
	#pragma warning disable CA1506
	sealed class DiscordProvider : Provider, IDiscordResponders
	{
		/// <inheritdoc />
		public override bool Connected => gatewayTask?.IsCompleted == false;

		/// <inheritdoc />
		public override string BotMention
		{
			get
			{
				if (!Connected)
					throw new InvalidOperationException("Provider not connected");
				return NormalizeMentions($"<@{currentUserId}>");
			}
		}

		/// <summary>
		/// The <see cref="IAssemblyInformationProvider"/> for the <see cref="DiscordProvider"/>.
		/// </summary>
		readonly IAssemblyInformationProvider assemblyInformationProvider;

		/// <summary>
		/// The <see cref="ServiceProvider"/> containing Discord services.
		/// </summary>
		readonly ServiceProvider serviceProvider;

		/// <summary>
		/// <see cref="List{T}"/> of mapped channel <see cref="Snowflake"/>s.
		/// </summary>
		readonly List<ulong> mappedChannels;

		/// <summary>
		/// Lock <see cref="object"/> used to sychronize connect/disconnect operations.
		/// </summary>
		readonly object connectDisconnectLock;

		/// <summary>
		/// <see cref="bool"/> to enable based mode. Will auto reply with a youtube link to a video that says "based on the hardware that's installed in it" to anyone saying 'based on what?' case-insensitive.
		/// </summary>
		readonly bool basedMeme;

		/// <summary>
		/// The <see cref="DiscordDMOutputDisplayType"/>.
		/// </summary>
		readonly DiscordDMOutputDisplayType outputDisplayType;

		/// <summary>
		/// The <see cref="CancellationTokenSource"/> for the <see cref="gatewayTask"/>.
		/// </summary>
		CancellationTokenSource gatewayCts;

		/// <summary>
		/// The <see cref="TaskCompletionSource{TResult}"/> for the initial gateway connection event.
		/// </summary>
		TaskCompletionSource<object> gatewayReadyTcs;

		/// <summary>
		/// The <see cref="Task"/> representing the lifetime of the client.
		/// </summary>
		Task<Result> gatewayTask;

		/// <summary>
		/// The bot's <see cref="Snowflake"/>.
		/// </summary>
		Snowflake currentUserId;

		/// <summary>
		/// Normalize a discord mention string.
		/// </summary>
		/// <param name="fromDiscord">The mention <see cref="string"/> provided by the Discord library.</param>
		/// <returns>The normalized mention <see cref="string"/>.</returns>
		static string NormalizeMentions(string fromDiscord) => fromDiscord.Replace("<@!", "<@", StringComparison.Ordinal);

		/// <summary>
		/// Create a <see cref="List{T}"/> of <see cref="IEmbedField"/>s for a discord update embed.
		/// </summary>
		/// <param name="revisionInformation">The <see cref="RevisionInformation"/> of the deployment.</param>
		/// <param name="byondVersion">The BYOND <see cref="Version"/> of the deployment.</param>
		/// <param name="gitHubOwner">The repository GitHub owner, if any.</param>
		/// <param name="gitHubRepo">The repository GitHub name, if any.</param>
		/// <param name="localCommitPushed"><see langword="true"/> if the local deployment commit was pushed to the remote repository.</param>
		/// <returns>A new <see cref="List{T}"/> of <see cref="IEmbedField"/>s to use.</returns>
		static List<IEmbedField> BuildUpdateEmbedFields(
			Models.RevisionInformation revisionInformation,
			Version byondVersion,
			string gitHubOwner,
			string gitHubRepo,
			bool localCommitPushed)
		{
			bool gitHub = gitHubOwner != null && gitHubRepo != null;
			var fields = new List<IEmbedField>
			{
				new EmbedField(
					"BYOND Version",
					$"{byondVersion.Major}.{byondVersion.Minor}{(byondVersion.Build > 0 ? $".{byondVersion.Build}" : String.Empty)}",
					true),
				new EmbedField(
					"Local Commit",
					localCommitPushed && gitHub
						? $"[{revisionInformation.CommitSha.Substring(0, 7)}](https://github.com/{gitHubOwner}/{gitHubRepo}/commit/{revisionInformation.CommitSha})"
						: revisionInformation.CommitSha.Substring(0, 7),
					true),
				new EmbedField(
					"Branch Commit",
					gitHub
						? $"[{revisionInformation.OriginCommitSha.Substring(0, 7)}](https://github.com/{gitHubOwner}/{gitHubRepo}/commit/{revisionInformation.OriginCommitSha})"
						: revisionInformation.OriginCommitSha.Substring(0, 7),
					true),
			};

			fields.AddRange((revisionInformation.ActiveTestMerges ?? Enumerable.Empty<RevInfoTestMerge>())
				.Select(x => x.TestMerge)
				.Select(x => new EmbedField(
					$"#{x.Number}",
					$"[{x.TitleAtMerge}]({x.Url}) by _[@{x.Author}](https://github.com/{x.Author})_{Environment.NewLine}Commit: [{x.TargetCommitSha.Substring(0, 7)}](https://github.com/{gitHubOwner}/{gitHubRepo}/commit/{x.TargetCommitSha}){(String.IsNullOrWhiteSpace(x.Comment) ? String.Empty : $"{Environment.NewLine}_**{x.Comment}**_")}",
					false)));

			return fields;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="DiscordProvider"/> class.
		/// </summary>
		/// <param name="jobManager">The <see cref="IJobManager"/> for the <see cref="Provider"/>.</param>
		/// <param name="assemblyInformationProvider">The value of <see cref="assemblyInformationProvider"/>.</param>
		/// <param name="logger">The <see cref="ILogger"/> for the <see cref="Provider"/>.</param>
		/// <param name="chatBot">The <see cref="ChatBot"/> for the <see cref="Provider"/>.</param>
		public DiscordProvider(
			IJobManager jobManager,
			IAssemblyInformationProvider assemblyInformationProvider,
			ILogger<DiscordProvider> logger,
			ChatBot chatBot)
			: base(jobManager, logger, chatBot)
		{
			this.assemblyInformationProvider = assemblyInformationProvider ?? throw new ArgumentNullException(nameof(assemblyInformationProvider));

			mappedChannels = new List<ulong>();
			connectDisconnectLock = new object();

			var csb = new DiscordConnectionStringBuilder(chatBot.ConnectionString);
			var botToken = csb.BotToken;
			basedMeme = csb.BasedMeme;
			outputDisplayType = csb.DMOutputDisplay;

			serviceProvider = new ServiceCollection()
				.AddDiscordGateway(serviceProvider => botToken)
				.AddSingleton<IDiscordResponders>(serviceProvider => this)
				.AddResponder<DiscordForwardingResponder>()
				.BuildServiceProvider();
		}

		/// <inheritdoc />
		public override async ValueTask DisposeAsync()
		{
			await base.DisposeAsync().ConfigureAwait(false);
			await serviceProvider.DisposeAsync().ConfigureAwait(false);

			// this line is purely here to shutup CA2213
			gatewayCts?.Dispose();
		}

		/// <inheritdoc />
		public override async Task<IReadOnlyCollection<ChannelRepresentation>> MapChannels(IEnumerable<Api.Models.ChatChannel> channels, CancellationToken cancellationToken)
		{
			if (channels == null)
				throw new ArgumentNullException(nameof(channels));

			if (!Connected)
			{
				Logger.LogWarning("Cannot map channels, provider disconnected!");
				return Array.Empty<ChannelRepresentation>();
			}

			var usersClient = serviceProvider.GetRequiredService<IDiscordRestUserAPI>();
			var currentUserResponse = await usersClient.GetCurrentUserAsync(cancellationToken).ConfigureAwait(false);

			if (!currentUserResponse.IsSuccess)
			{
				Logger.LogWarning("Error retrieving current Discord user: {0}", currentUserResponse.Error.Message);
				return Array.Empty<ChannelRepresentation>();
			}

			async Task<ChannelRepresentation> GetModelChannelFromDBChannel(Api.Models.ChatChannel channelFromDB)
			{
				if (!channelFromDB.DiscordChannelId.HasValue)
					throw new InvalidOperationException("ChatChannel missing DiscordChannelId!");

				var channelId = channelFromDB.DiscordChannelId.Value;
				ulong discordChannelId;
				string connectionName;
				string friendlyName;
				if (channelId == 0)
				{
					connectionName = currentUserResponse.Entity.Username;
					friendlyName = "(Unmapped accessible channels)";
					discordChannelId = 0;
				}
				else
				{
					var channelsClient = serviceProvider.GetRequiredService<IDiscordRestChannelAPI>();
					var discordChannelResponse = await channelsClient.GetChannelAsync(new Snowflake(channelId), cancellationToken);
					if (!discordChannelResponse.IsSuccess)
					{
						Logger.LogWarning("Error retrieving discord channel {0}: {1}", channelId, discordChannelResponse.Error.Message);
						return null;
					}

					if (discordChannelResponse.Entity.Type != ChannelType.GuildText)
					{
						Logger.LogWarning("Cound not map channel {0}! Incorrect type: {1}", channelId, discordChannelResponse.Entity.Type);
						return null;
					}

					discordChannelId = discordChannelResponse.Entity.ID.Value;
					friendlyName = discordChannelResponse.Entity.Name.Value;

					var guildsClient = serviceProvider.GetRequiredService<IDiscordRestGuildAPI>();
					var guildsResponse = await guildsClient.GetGuildAsync(
						discordChannelResponse.Entity.GuildID.Value,
						false,
						cancellationToken);
					if (!guildsResponse.IsSuccess)
					{
						Logger.LogWarning(
							"Error retrieving discord guild {0}: {1}",
							discordChannelResponse.Entity.GuildID.Value,
							discordChannelResponse.Error.Message);
						return null;
					}

					connectionName = guildsResponse.Entity.Name;
				}

				var channelModel = new ChannelRepresentation
				{
					RealId = discordChannelId,
					IsAdminChannel = channelFromDB.IsAdminChannel == true,
					ConnectionName = connectionName,
					FriendlyName = friendlyName,
					IsPrivateChannel = false,
					Tag = channelFromDB.Tag,
				};

				Logger.LogTrace("Mapped channel {0}: {1}", channelModel.RealId, channelModel.FriendlyName);
				return channelModel;
			}

			var tasks = channels
				.Select(x => GetModelChannelFromDBChannel(x))
				.Where(x => x != null)
				.ToList();

			await Task.WhenAll(tasks);

			var enumerator = tasks
				.Select(x => x.Result)
				.ToList();

			lock (mappedChannels)
			{
				mappedChannels.Clear();
				mappedChannels.AddRange(enumerator.Select(x => x.RealId));
			}

			return enumerator;
		}

		/// <inheritdoc />
		public override async Task SendMessage(ulong channelId, string message, CancellationToken cancellationToken)
		{
			var channelsClient = serviceProvider.GetRequiredService<IDiscordRestChannelAPI>();
			async Task SendToChannel(Snowflake channelId)
			{
				var result = await channelsClient.CreateMessageAsync(
					channelId,
					message,
					ct: cancellationToken);

				if (!result.IsSuccess)
					Logger.LogWarning(
						"Failed to send to channel {0}: {1}",
						channelId,
						result.Error.Message);
			}

			try
			{
				if (channelId == 0)
				{
					var usersClient = serviceProvider.GetRequiredService<IDiscordRestUserAPI>();
					var currentGuildsResponse = await usersClient.GetCurrentUserGuildsAsync(ct: cancellationToken).ConfigureAwait(false);
					if (!currentGuildsResponse.IsSuccess)
					{
						Logger.LogWarning(
							"Error retrieving current discord guilds: {0}",
							currentGuildsResponse.Error.Message);
						return;
					}

					var unmappedTextChannels = currentGuildsResponse
						.Entity
						.SelectMany(x => x.Channels.Value);

					lock (mappedChannels)
						unmappedTextChannels = unmappedTextChannels
							.Where(x => !mappedChannels.Contains(x.ID.Value))
							.ToList();

					// discord API confirmed weak boned: https://stackoverflow.com/a/52462336
					if (unmappedTextChannels.Any())
					{
						Logger.LogTrace("Dispatching to {0} unmapped channels...", unmappedTextChannels.Count());
						await Task.WhenAll(
							unmappedTextChannels.Select(
								x => SendToChannel(x.ID)))
							.ConfigureAwait(false);
					}

					return;
				}

				await SendToChannel(new Snowflake(channelId)).ConfigureAwait(false);
			}
			catch (Exception e)
			{
				if (e is OperationCanceledException)
					cancellationToken.ThrowIfCancellationRequested();
				Logger.LogWarning(e, "Error sending discord message!");
			}
		}

		/// <inheritdoc />
		public override async Task<Func<string, string, Task>> SendUpdateMessage(
			Models.RevisionInformation revisionInformation,
			Version byondVersion,
			DateTimeOffset? estimatedCompletionTime,
			string gitHubOwner,
			string gitHubRepo,
			ulong channelId,
			bool localCommitPushed,
			CancellationToken cancellationToken)
		{
			localCommitPushed |= revisionInformation.CommitSha == revisionInformation.OriginCommitSha;

			var fields = BuildUpdateEmbedFields(revisionInformation, byondVersion, gitHubOwner, gitHubRepo, localCommitPushed);
			var embed = new Embed
			{
				Author = new EmbedAuthor
				{
					Name = assemblyInformationProvider.VersionPrefix,
					Url = "https://github.com/tgstation/tgstation-server",
					IconUrl = "https://avatars0.githubusercontent.com/u/1363778?s=280&v=4",
				},
				Colour = Color.FromArgb(0xF1, 0xC4, 0x0F),
				Description = "TGS has begun deploying active repository code to production.",
				Fields = fields,
				Title = "Code Deployment",
				Footer = new EmbedFooter(
					$"In progress...{(estimatedCompletionTime.HasValue ? " ETA" : String.Empty)}"),
				Timestamp = estimatedCompletionTime ?? default,
			};

			Logger.LogTrace("Attempting to post deploy embed to channel {0}...", channelId);
			var channelsClient = serviceProvider.GetRequiredService<IDiscordRestChannelAPI>();

			var messageResponse = await channelsClient.CreateMessageAsync(
				new Snowflake(channelId),
				"DM: Deployment in Progress...",
				embeds: new List<IEmbed> { embed },
				ct: cancellationToken)
				.ConfigureAwait(false);

			if (!messageResponse.IsSuccess)
				Logger.LogWarning("Failed to post deploy embed to channel {0}: {1}", channelId, messageResponse.Error.Message);

			return async (errorMessage, dreamMakerOutput) =>
			{
				var completionString = errorMessage == null ? "Succeeded" : "Failed";

				embed = new Embed
				{
					Author = embed.Author,
					Colour = errorMessage == null ? Color.Green : Color.Red,
					Description = errorMessage == null
					? "The deployment completed successfully and will be available at the next server reboot."
					: "The deployment failed.",
					Fields = fields,
					Title = embed.Title,
					Footer = new EmbedFooter(
						completionString),
					Timestamp = DateTimeOffset.UtcNow,
				};

				var showDMOutput = outputDisplayType switch
				{
					DiscordDMOutputDisplayType.Always => true,
					DiscordDMOutputDisplayType.Never => false,
					DiscordDMOutputDisplayType.OnError => errorMessage != null,
					_ => throw new InvalidOperationException($"Invalid DiscordDMOutputDisplayType: {outputDisplayType}"),
				};

				if (dreamMakerOutput != null)
				{
					// https://github.com/discord-net/Discord.Net/blob/8349cd7e1eb92e9a3baff68082c30a7b43e8e9b7/src/Discord.Net.Core/Entities/Messages/EmbedBuilder.cs#L431
					const int MaxFieldValueLength = 1024;
					showDMOutput = showDMOutput && dreamMakerOutput.Length < MaxFieldValueLength - (6 + Environment.NewLine.Length);
					if (showDMOutput)
						fields.Add(new EmbedField(
							"DreamMaker Output",
							$"```{Environment.NewLine}{dreamMakerOutput}{Environment.NewLine}```",
							false));
				}

				if (errorMessage != null)
					fields.Add(new EmbedField(
						"Error Message",
						errorMessage,
						false));

				var updatedMessage = $"DM: Deployment {completionString}!";

				async Task CreateUpdatedMessage()
				{
					var createUpdatedMessageResponse = await channelsClient.CreateMessageAsync(
						new Snowflake(channelId),
						updatedMessage,
						embeds: new List<IEmbed> { embed },
						ct: cancellationToken)
						.ConfigureAwait(false);

					if (!createUpdatedMessageResponse.IsSuccess)
						Logger.LogWarning(
							"Creating updated deploy embed failed! Error: {0}",
							createUpdatedMessageResponse.Error.Message);
				}

				if (!messageResponse.IsSuccess)
					await CreateUpdatedMessage();
				else
				{
					var editResponse = await channelsClient.EditMessageAsync(
						new Snowflake(channelId),
						messageResponse.Entity.ID,
						updatedMessage,
						embeds: new List<IEmbed> { embed },
						ct: cancellationToken)
						.ConfigureAwait(false);

					if (!editResponse.IsSuccess)
					{
						Logger.LogWarning(
							"Updating deploy embed {0} failed, attempting new post! Error: {1}",
							messageResponse.Entity.ID,
							editResponse.Error.Message);
						await CreateUpdatedMessage();
					}
				}
			};
		}

		/// <inheritdoc />
		public async Task<Result> RespondAsync(IMessageCreate messageCreateEvent, CancellationToken cancellationToken)
		{
			if ((messageCreateEvent.Type != MessageType.Default
				&& messageCreateEvent.Type != MessageType.InlineReply)
				|| messageCreateEvent.Author.ID == currentUserId)
				return Result.FromSuccess();

			if (basedMeme && messageCreateEvent.Content.Equals("Based on what?", StringComparison.OrdinalIgnoreCase))
			{
				// DCT: None available
				await SendMessage(
					messageCreateEvent.ChannelID.Value,
					"https://youtu.be/LrNu-SuFF_o",
					default)
					.ConfigureAwait(false);
				return Result.FromSuccess();
			}

			var channelsClient = serviceProvider.GetRequiredService<IDiscordRestChannelAPI>();
			var channelResponse = await channelsClient.GetChannelAsync(messageCreateEvent.ChannelID, cancellationToken).ConfigureAwait(false);
			if (!channelResponse.IsSuccess)
			{
				Logger.LogWarning(
					"Failed to get channel {0} in response to message {1}!",
					messageCreateEvent.ChannelID,
					messageCreateEvent.ID);

				// we'll handle the errors ourselves
				return Result.FromSuccess();
			}

			var pm = channelResponse.Entity.Type == ChannelType.DM || channelResponse.Entity.Type == ChannelType.GroupDM;
			var shouldNotAnswer = !pm;
			if (shouldNotAnswer)
				lock (mappedChannels)
					shouldNotAnswer = !mappedChannels.Contains(messageCreateEvent.ChannelID.Value);

			var content = NormalizeMentions(messageCreateEvent.Content);
			var mentionedUs = messageCreateEvent.Mentions.Any(x => x.ID == currentUserId)
				|| (!shouldNotAnswer && content.Split(' ').First().Equals(ChatManager.CommonMention, StringComparison.OrdinalIgnoreCase));

			if (shouldNotAnswer)
			{
				if (mentionedUs)
					Logger.LogTrace(
						"Ignoring mention from {0} ({1}) by {2} ({3}). Channel not mapped!",
						messageCreateEvent.ChannelID,
						channelResponse.Entity.Name,
						messageCreateEvent.Author.ID,
						messageCreateEvent.Author.Username);

				return Result.FromSuccess();
			}

			string guildName = "UNKNOWN";
			if (!pm)
			{
				var guildsClient = serviceProvider.GetRequiredService<IDiscordRestGuildAPI>();
				var messageGuildResponse = await guildsClient.GetGuildAsync(messageCreateEvent.GuildID.Value, false, cancellationToken).ConfigureAwait(false);
				if (messageGuildResponse.IsSuccess)
					guildName = messageGuildResponse.Entity.Name;
				else
					Logger.LogWarning(
						"Failed to get channel {0} in response to message {1}!",
						messageCreateEvent.ChannelID,
						messageCreateEvent.ID);
			}

			var result = new Message
			{
				Content = content,
				User = new ChatUser
				{
					RealId = messageCreateEvent.Author.ID.Value,
					Channel = new ChannelRepresentation
					{
						RealId = messageCreateEvent.ChannelID.Value,
						IsPrivateChannel = pm,
						ConnectionName = pm ? messageCreateEvent.Author.Username : guildName,
						FriendlyName = channelResponse.Entity.Name.Value,

						// isAdmin and Tag populated by manager
					},
					FriendlyName = messageCreateEvent.Author.Username,
					Mention = NormalizeMentions($"<@{messageCreateEvent.Author.ID}>"),
				},
			};

			EnqueueMessage(result);
			return Result.FromSuccess();
		}

		/// <inheritdoc />
		public Task<Result> RespondAsync(IReady readyEvent, CancellationToken cancellationToken)
		{
			gatewayReadyTcs?.TrySetResult(null);
			return Task.FromResult(Result.FromSuccess());
		}

		/// <inheritdoc />
		protected override async Task Connect(CancellationToken cancellationToken)
		{
			try
			{
				lock (connectDisconnectLock)
				{
					if (gatewayCts != null)
						throw new InvalidOperationException("Discord gateway still active!");

					gatewayCts = new CancellationTokenSource();
				}

				var gatewayCancellationToken = gatewayCts.Token;
				var gatewayClient = serviceProvider.GetRequiredService<DiscordGatewayClient>();

				Task<Result> localGatewayTask;
				gatewayReadyTcs = new TaskCompletionSource<object>();

				using var gatewayConnectionAbortRegistration = cancellationToken.Register(() => gatewayReadyTcs.TrySetCanceled());

				// reconnects keep happening until we stop or it faults, our auto-reconnector will handle the latter
				localGatewayTask = gatewayClient.RunAsync(gatewayCancellationToken);
				try
				{
					await Task.WhenAny(gatewayReadyTcs.Task, localGatewayTask).ConfigureAwait(false);

					if (localGatewayTask.IsCompleted || cancellationToken.IsCancellationRequested)
						throw new JobException(ErrorCode.ChatCannotConnectProvider);

					var userClient = serviceProvider.GetRequiredService<IDiscordRestUserAPI>();

					using var localCombinedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, gatewayCancellationToken);
					var currentUserResult = await userClient.GetCurrentUserAsync(localCombinedCts.Token).ConfigureAwait(false);
					if (!currentUserResult.IsSuccess)
					{
						Logger.LogWarning("Unable to retrieve current user: {0}", currentUserResult.Error.Message);
						throw new JobException(ErrorCode.ChatCannotConnectProvider);
					}

					currentUserId = currentUserResult.Entity.ID;
				}
				finally
				{
					gatewayTask = localGatewayTask;
				}
			}
			catch
			{
				// will handle cleanup
				// DCT: Musn't abort
				await DisconnectImpl(default).ConfigureAwait(false);
				throw;
			}
		}

		/// <inheritdoc />
		protected override async Task DisconnectImpl(CancellationToken cancellationToken)
		{
			Task<Result> localGatewayTask;
			CancellationTokenSource localGatewayCts;
			lock (connectDisconnectLock)
			{
				localGatewayTask = gatewayTask;
				localGatewayCts = gatewayCts;
				gatewayTask = null;
				gatewayCts = null;
				if (localGatewayTask == null)
					return;
			}

			localGatewayCts.Cancel();
			var gatewayResult = await localGatewayTask.ConfigureAwait(false);
			if (!gatewayResult.IsSuccess)
				Logger.LogWarning("Gateway issue: {0}", gatewayResult.Error.Message);

			localGatewayCts.Dispose();
		}
	}
	#pragma warning restore CA1506
}
