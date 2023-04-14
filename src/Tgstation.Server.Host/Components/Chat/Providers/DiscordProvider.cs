using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Remora.Discord.API.Abstractions.Gateway.Commands;
using Remora.Discord.API.Abstractions.Gateway.Events;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Abstractions.Results;
using Remora.Discord.API.Objects;
using Remora.Discord.Gateway;
using Remora.Discord.Gateway.Extensions;
using Remora.Rest.Core;
using Remora.Rest.Results;
using Remora.Results;

using Tgstation.Server.Api.Models;
using Tgstation.Server.Host.Components.Interop;
using Tgstation.Server.Host.Extensions;
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
		/// The <see cref="ChannelType"/>s supported by the <see cref="DiscordProvider"/> for mapping.
		/// </summary>
		static readonly ChannelType[] SupportedGuildChannelTypes = new[]
		{
			ChannelType.GuildText,
			ChannelType.GuildAnnouncement,
			ChannelType.PrivateThread,
			ChannelType.PublicThread,
		};

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
		/// If the tgstation-server logo is shown in deployment embeds.
		/// </summary>
		readonly bool deploymentBranding;

		/// <summary>
		/// The <see cref="DiscordDMOutputDisplayType"/>.
		/// </summary>
		readonly DiscordDMOutputDisplayType outputDisplayType;

		/// <summary>
		/// The <see cref="CancellationTokenSource"/> for the <see cref="gatewayTask"/>.
		/// </summary>
		CancellationTokenSource gatewayCts;

		/// <summary>
		/// The <see cref="TaskCompletionSource"/> for the initial gateway connection event.
		/// </summary>
		TaskCompletionSource gatewayReadyTcs;

		/// <summary>
		/// The <see cref="Task"/> representing the lifetime of the client.
		/// </summary>
		Task<Result> gatewayTask;

		/// <summary>
		/// The bot's <see cref="Snowflake"/>.
		/// </summary>
		Snowflake currentUserId;

		/// <summary>
		/// If <see cref="serviceProvider"/> is being disposed.
		/// </summary>
		bool disposing;

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
						? $"[{revisionInformation.CommitSha[..7]}](https://github.com/{gitHubOwner}/{gitHubRepo}/commit/{revisionInformation.CommitSha})"
						: revisionInformation.CommitSha[..7],
					true),
				new EmbedField(
					"Branch Commit",
					gitHub
						? $"[{revisionInformation.OriginCommitSha[..7]}](https://github.com/{gitHubOwner}/{gitHubRepo}/commit/{revisionInformation.OriginCommitSha})"
						: revisionInformation.OriginCommitSha[..7],
					true),
			};

			fields.AddRange((revisionInformation.ActiveTestMerges ?? Enumerable.Empty<RevInfoTestMerge>())
				.Select(x => x.TestMerge)
				.Select(x => new EmbedField(
					$"#{x.Number}",
					$"[{x.TitleAtMerge}]({x.Url}) by _[@{x.Author}](https://github.com/{x.Author})_{Environment.NewLine}Commit: [{x.TargetCommitSha[..7]}](https://github.com/{gitHubOwner}/{gitHubRepo}/commit/{x.TargetCommitSha}){(String.IsNullOrWhiteSpace(x.Comment) ? String.Empty : $"{Environment.NewLine}_**{x.Comment}**_")}",
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
			deploymentBranding = csb.DeploymentBranding;

			serviceProvider = new ServiceCollection()
				.AddDiscordGateway(serviceProvider => botToken)
				.Configure<DiscordGatewayClientOptions>(options => options.Intents |= GatewayIntents.MessageContents)
				.AddSingleton<IDiscordResponders>(serviceProvider => this)
				.AddResponder<DiscordForwardingResponder>()
				.BuildServiceProvider();
		}

		/// <inheritdoc />
		public override async ValueTask DisposeAsync()
		{
			lock (serviceProvider)
			{
				// serviceProvider can recursively dispose us
				if (disposing)
					return;
				disposing = true;
			}

			await base.DisposeAsync();
			await serviceProvider.DisposeAsync();
			Logger.LogTrace("ServiceProvider disposed");

			// this line is purely here to shutup CA2213. It should always be null
			gatewayCts?.Dispose();

			disposing = false;
		}

		/// <inheritdoc />
		public override async Task SendMessage(Message replyTo, MessageContent message, ulong channelId, CancellationToken cancellationToken)
		{
			Optional<IMessageReference> replyToReference = default;
			Optional<IAllowedMentions> allowedMentions = default;
			if (replyTo != null && replyTo is DiscordMessage discordMessage)
			{
				replyToReference = discordMessage.MessageReference;
				allowedMentions = new AllowedMentions(
					Parse: new List<MentionType> // reset settings back to how discord acts if this is not passed (which is different than the default if empty)
					{
						MentionType.Everyone,
						MentionType.Roles,
						MentionType.Users,
					},
					MentionRepliedUser: false); // disable reply mentions
			}

			var embeds = ConvertEmbed(message.Embed);

			var channelsClient = serviceProvider.GetRequiredService<IDiscordRestChannelAPI>();
			async Task SendToChannel(Snowflake channelId)
			{
				var result = await channelsClient.CreateMessageAsync(
					channelId,
					message.Text,
					embeds: embeds,
					messageReference: replyToReference,
					allowedMentions: allowedMentions,
					ct: cancellationToken);

				if (!result.IsSuccess)
					Logger.LogWarning(
						"Failed to send to channel {channelId}: {result}",
						channelId,
						result.LogFormat());
			}

			try
			{
				if (channelId == 0)
				{
					IEnumerable<IChannel> unmappedTextChannels;
					var allAccessibleTextChannels = await GetAllAccessibleTextChannels(cancellationToken);
					lock (mappedChannels)
					{
						unmappedTextChannels = allAccessibleTextChannels
							.Where(x => !mappedChannels.Contains(x.ID.Value))
							.ToList();

						var remapRequired = unmappedTextChannels.Any()
							|| mappedChannels.Any(
								mappedChannel => !allAccessibleTextChannels.Any(
									accessibleTextChannel => accessibleTextChannel.ID == new Snowflake(mappedChannel)));

						if (remapRequired)
							EnqueueMessage(null);
					}

					// discord API confirmed weak boned: https://stackoverflow.com/a/52462336
					if (unmappedTextChannels.Any())
					{
						Logger.LogDebug("Dispatching to {count} unmapped channels...", unmappedTextChannels.Count());
						await Task.WhenAll(
							unmappedTextChannels.Select(
								x => SendToChannel(x.ID)));
					}

					return;
				}

				await SendToChannel(new Snowflake(channelId));
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
			var author = new EmbedAuthor(assemblyInformationProvider.VersionPrefix)
			{
				Url = "https://github.com/tgstation/tgstation-server",
				IconUrl = "https://avatars0.githubusercontent.com/u/1363778?s=280&v=4",
			};
			var embed = new Embed
			{
				Author = deploymentBranding ? author : default,
				Colour = Color.FromArgb(0xF1, 0xC4, 0x0F),
				Description = "TGS has begun deploying active repository code to production.",
				Fields = fields,
				Title = "Code Deployment",
				Footer = new EmbedFooter(
					$"In progress...{(estimatedCompletionTime.HasValue ? " ETA" : String.Empty)}"),
				Timestamp = estimatedCompletionTime ?? default,
			};

			Logger.LogTrace("Attempting to post deploy embed to channel {channelId}...", channelId);
			var channelsClient = serviceProvider.GetRequiredService<IDiscordRestChannelAPI>();

			var messageResponse = await channelsClient.CreateMessageAsync(
				new Snowflake(channelId),
				"DM: Deployment in Progress...",
				embeds: new List<IEmbed> { embed },
				ct: cancellationToken)
				;

			if (!messageResponse.IsSuccess)
				Logger.LogWarning("Failed to post deploy embed to channel {channelId}: {result}", channelId, messageResponse.LogFormat());

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
						;

					if (!createUpdatedMessageResponse.IsSuccess)
						Logger.LogWarning(
							"Creating updated deploy embed failed: {result}",
							createUpdatedMessageResponse.LogFormat());
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
						;

					if (!editResponse.IsSuccess)
					{
						Logger.LogWarning(
							"Updating deploy embed {messageId} failed, attempting new post: {result}",
							messageResponse.Entity.ID,
							editResponse.LogFormat());
						await CreateUpdatedMessage();
					}
				}
			};
		}

		/// <inheritdoc />
		public async Task<Result> RespondAsync(IMessageCreate messageCreateEvent, CancellationToken cancellationToken)
		{
			if (messageCreateEvent == null)
				throw new ArgumentNullException(nameof(messageCreateEvent));

			if ((messageCreateEvent.Type != MessageType.Default
				&& messageCreateEvent.Type != MessageType.InlineReply)
				|| messageCreateEvent.Author.ID == currentUserId)
				return Result.FromSuccess();

			var messageReference = new MessageReference
			{
				ChannelID = messageCreateEvent.ChannelID,
				GuildID = messageCreateEvent.GuildID,
				MessageID = messageCreateEvent.ID,
				FailIfNotExists = false,
			};

			if (basedMeme && messageCreateEvent.Content.Equals("Based on what?", StringComparison.OrdinalIgnoreCase))
			{
				// DCT: None available
				await SendMessage(
					new DiscordMessage
					{
						MessageReference = messageReference,
					},
					new MessageContent
					{
						Text = "https://youtu.be/LrNu-SuFF_o",
					},
					messageCreateEvent.ChannelID.Value,
					default);
				return Result.FromSuccess();
			}

			var channelsClient = serviceProvider.GetRequiredService<IDiscordRestChannelAPI>();
			var channelResponse = await channelsClient.GetChannelAsync(messageCreateEvent.ChannelID, cancellationToken);
			if (!channelResponse.IsSuccess)
			{
				Logger.LogWarning(
					"Failed to get channel {channelId} in response to message {messageId}!",
					messageCreateEvent.ChannelID,
					messageCreateEvent.ID);

				// we'll handle the errors ourselves
				return Result.FromSuccess();
			}

			var pm = channelResponse.Entity.Type == ChannelType.DM || channelResponse.Entity.Type == ChannelType.GroupDM;
			var shouldNotAnswer = !pm;
			if (shouldNotAnswer)
				lock (mappedChannels)
					shouldNotAnswer = !mappedChannels.Contains(messageCreateEvent.ChannelID.Value) && !mappedChannels.Contains(0);

			var content = NormalizeMentions(messageCreateEvent.Content);
			var mentionedUs = messageCreateEvent.Mentions.Any(x => x.ID == currentUserId)
				|| (!shouldNotAnswer && content.Split(' ').First().Equals(ChatManager.CommonMention, StringComparison.OrdinalIgnoreCase));

			if (shouldNotAnswer)
			{
				if (mentionedUs)
					Logger.LogTrace(
						"Ignoring mention from {channelId} ({channelName}) by {authorId} ({authorName}). Channel not mapped!",
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
				var messageGuildResponse = await guildsClient.GetGuildAsync(messageCreateEvent.GuildID.Value, false, cancellationToken);
				if (messageGuildResponse.IsSuccess)
					guildName = messageGuildResponse.Entity.Name;
				else
					Logger.LogWarning(
						"Failed to get channel {channelID} in response to message {messageID}: {result}",
						messageCreateEvent.ChannelID,
						messageCreateEvent.ID,
						messageGuildResponse.LogFormat());
			}

			var result = new DiscordMessage
			{
				MessageReference = messageReference,
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
						EmbedsSupported = true,

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
			if (readyEvent == null)
				throw new ArgumentNullException(nameof(readyEvent));

			Logger.LogTrace("Gatway ready. Version: {version}", readyEvent.Version);
			gatewayReadyTcs?.TrySetResult();
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
				gatewayReadyTcs = new TaskCompletionSource();

				using var gatewayConnectionAbortRegistration = cancellationToken.Register(() => gatewayReadyTcs.TrySetCanceled());
				gatewayCancellationToken.Register(() => Logger.LogTrace("Stopping gateway client..."));

				// reconnects keep happening until we stop or it faults, our auto-reconnector will handle the latter
				localGatewayTask = gatewayClient.RunAsync(gatewayCancellationToken);
				try
				{
					await Task.WhenAny(gatewayReadyTcs.Task, localGatewayTask);

					if (localGatewayTask.IsCompleted || cancellationToken.IsCancellationRequested)
						throw new JobException(ErrorCode.ChatCannotConnectProvider);

					var userClient = serviceProvider.GetRequiredService<IDiscordRestUserAPI>();

					using var localCombinedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, gatewayCancellationToken);
					var currentUserResult = await userClient.GetCurrentUserAsync(localCombinedCts.Token);
					if (!currentUserResult.IsSuccess)
					{
						Logger.LogWarning("Unable to retrieve current user: {result}", currentUserResult.LogFormat());
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
				await DisconnectImpl(default);
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
			var gatewayResult = await localGatewayTask;
			if (!gatewayResult.IsSuccess)
				Logger.LogWarning("Gateway issue: {result}", gatewayResult.LogFormat());

			localGatewayCts.Dispose();
		}

		/// <inheritdoc />
		protected override async Task<Dictionary<Models.ChatChannel, IEnumerable<ChannelRepresentation>>> MapChannelsImpl(IEnumerable<Models.ChatChannel> channels, CancellationToken cancellationToken)
		{
			if (channels == null)
				throw new ArgumentNullException(nameof(channels));

			var remapRequired = false;
			var guildsClient = serviceProvider.GetRequiredService<IDiscordRestGuildAPI>();

			async Task<Tuple<Models.ChatChannel, IEnumerable<ChannelRepresentation>>> GetModelChannelFromDBChannel(Models.ChatChannel channelFromDB)
			{
				if (!channelFromDB.DiscordChannelId.HasValue)
					throw new InvalidOperationException("ChatChannel missing DiscordChannelId!");

				var channelId = channelFromDB.DiscordChannelId.Value;
				var channelsClient = serviceProvider.GetRequiredService<IDiscordRestChannelAPI>();
				var discordChannelResponse = await channelsClient.GetChannelAsync(new Snowflake(channelId), cancellationToken);
				if (!discordChannelResponse.IsSuccess)
				{
					Logger.LogWarning(
						"Error retrieving discord channel {channelId}: {result}",
						channelId,
						discordChannelResponse.LogFormat());

					remapRequired |= !(discordChannelResponse.Error is RestResultError<RestError> restResultError
						&& (restResultError.Error?.Code == DiscordError.MissingAccess
						|| restResultError.Error?.Code == DiscordError.UnknownChannel));
					return null;
				}

				var channelType = discordChannelResponse.Entity.Type;
				if (!SupportedGuildChannelTypes.Contains(channelType))
				{
					Logger.LogWarning("Cound not map channel {channelId}! Incorrect type: {channelType}", channelId, discordChannelResponse.Entity.Type);
					return null;
				}

				var guildId = discordChannelResponse.Entity.GuildID.Value;

				var guildsResponse = await guildsClient.GetGuildAsync(
					guildId,
					false,
					cancellationToken);
				if (!guildsResponse.IsSuccess)
				{
					Logger.LogWarning(
						"Error retrieving discord guild {guildID}: {result}",
						guildId,
						guildsResponse.LogFormat());
					remapRequired |= true;
					return null;
				}

				var connectionName = guildsResponse.Entity.Name;

				var channelModel = new ChannelRepresentation
				{
					RealId = channelId,
					IsAdminChannel = channelFromDB.IsAdminChannel == true,
					ConnectionName = guildsResponse.Entity.Name,
					FriendlyName = discordChannelResponse.Entity.Name.Value,
					IsPrivateChannel = false,
					Tag = channelFromDB.Tag,
					EmbedsSupported = true,
				};

				Logger.LogTrace("Mapped channel {realId}: {friendlyName}", channelModel.RealId, channelModel.FriendlyName);
				return Tuple.Create<Models.ChatChannel, IEnumerable<ChannelRepresentation>>(
					channelFromDB,
					new List<ChannelRepresentation> { channelModel });
			}

			var tasks = channels
				.Where(x => x.DiscordChannelId != 0)
				.Select(GetModelChannelFromDBChannel)
				.ToList();

			await Task.WhenAll(tasks);

			var channelIdZeroModel = channels.FirstOrDefault(x => x.DiscordChannelId == 0);
			if (channelIdZeroModel != null)
			{
				Logger.LogInformation("Mapping ALL additional accessible text channels");
				var allAccessibleChannels = await GetAllAccessibleTextChannels(cancellationToken);
				var unmappedTextChannels = allAccessibleChannels
					.Where(x => !tasks.Any(task => task.Result != null && new Snowflake(task.Result.Item1.DiscordChannelId.Value) == x.ID));

				async Task<Tuple<Models.ChatChannel, IEnumerable<ChannelRepresentation>>> CreateMappingsForUnmappedChannels()
				{
					var unmappedTasks =
						unmappedTextChannels.Select(
							async unmappedTextChannel =>
							{
								var fakeChannelModel = new Models.ChatChannel
								{
									DiscordChannelId = unmappedTextChannel.ID.Value,
									IsAdminChannel = channelIdZeroModel.IsAdminChannel,
									Tag = channelIdZeroModel.Tag,
								};

								var tuple = await GetModelChannelFromDBChannel(fakeChannelModel);
								return tuple?.Item2.First();
							})
						.ToList();

					// Add catch-all channel
					unmappedTasks.Add(Task.FromResult(
						new ChannelRepresentation
						{
							IsAdminChannel = channelIdZeroModel.IsAdminChannel.Value,
							ConnectionName = "(Unknown Discord Guilds)",
							EmbedsSupported = true,
							FriendlyName = "(Unknown Discord Channels)",
							RealId = 0,
							Tag = channelIdZeroModel.Tag,
						}));

					await Task.WhenAll(unmappedTasks);
					return Tuple.Create<Models.ChatChannel, IEnumerable<ChannelRepresentation>>(
						channelIdZeroModel,
						unmappedTasks.Select(x => x.Result).Where(x => x != null).ToList());
				}

				var task = CreateMappingsForUnmappedChannels();
				await task;
				tasks.Add(task);
			}

			var enumerator = tasks
				.Select(x => x.Result)
				.Where(x => x != null)
				.ToList();

			lock (mappedChannels)
			{
				mappedChannels.Clear();
				mappedChannels.AddRange(enumerator.SelectMany(x => x.Item2).Select(x => x.RealId));
			}

			if (remapRequired)
			{
				Logger.LogWarning("Some channels failed to load with unknown errors. We will request that these be remapped, but it may result in communication spam. Please check prior logs and report an issue if this occurs.");
				EnqueueMessage(null);
			}

			return new Dictionary<Models.ChatChannel, IEnumerable<ChannelRepresentation>>(enumerator.Select(x => new KeyValuePair<Models.ChatChannel, IEnumerable<ChannelRepresentation>>(x.Item1, x.Item2)));
		}

		/// <summary>
		/// Get all text <see cref="IChannel"/>s accessible to and supported by the bot.
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in an <see cref="IEnumerable{T}"/> of accessible and compatible <see cref="IChannel"/>s.</returns>
		async Task<IEnumerable<IChannel>> GetAllAccessibleTextChannels(CancellationToken cancellationToken)
		{
			var usersClient = serviceProvider.GetRequiredService<IDiscordRestUserAPI>();
			var currentGuildsResponse = await usersClient.GetCurrentUserGuildsAsync(ct: cancellationToken);
			if (!currentGuildsResponse.IsSuccess)
			{
				Logger.LogWarning(
					"Error retrieving current discord guilds: {result}",
					currentGuildsResponse.LogFormat());
				return Enumerable.Empty<IChannel>();
			}

			var guildsClient = serviceProvider.GetRequiredService<IDiscordRestGuildAPI>();

			async Task<IEnumerable<IChannel>> GetGuildChannels(IPartialGuild guild)
			{
				var channelsTask = guildsClient.GetGuildChannelsAsync(guild.ID.Value, cancellationToken);
				var threads = await guildsClient.ListActiveGuildThreadsAsync(guild.ID.Value, cancellationToken);
				if (!threads.IsSuccess)
					Logger.LogWarning(
						"Error retrieving discord guild threads {guildId} ({guildName}): {result}",
						guild.ID,
						guild.Name,
						threads.LogFormat());

				var channels = await channelsTask;
				if (!channels.IsSuccess)
					Logger.LogWarning(
						"Error retrieving discord guild channels {guildId} ({guildName}): {result}",
						guild.ID,
						guild.Name,
						channels.LogFormat());

				if (!channels.IsSuccess && !threads.IsSuccess)
					return Enumerable.Empty<IChannel>();

				if (channels.IsSuccess && threads.IsSuccess)
					return channels.Entity.Concat(threads.Entity.Threads ?? Enumerable.Empty<IChannel>());

				return channels.Entity ?? threads.Entity?.Threads ?? Enumerable.Empty<IChannel>();
			}

			var guildsChannelsTasks = currentGuildsResponse.Entity
				.Select(GetGuildChannels)
				.ToList();

			await Task.WhenAll(guildsChannelsTasks);

			var allAccessibleChannels = guildsChannelsTasks
				.SelectMany(task => task.Result)
				.Where(guildChannel => SupportedGuildChannelTypes.Contains(guildChannel.Type));

			return allAccessibleChannels;
		}

		/// <summary>
		/// Convert a <see cref="ChatEmbed"/> to an <see cref="IEmbed"/> parameters.
		/// </summary>
		/// <param name="embed">The <see cref="ChatEmbed"/> to convert.</param>
		/// <returns>The parameter for sending a single <see cref="IEmbed"/>.</returns>
		#pragma warning disable CA1502
		Optional<IReadOnlyList<IEmbed>> ConvertEmbed(ChatEmbed embed)
		{
			if (embed == null)
				return default;

			var embedErrors = new List<string>();
			Optional<Color> colour = default;
			if (embed.Colour != null)
				if (Int32.TryParse(embed.Colour[1..], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var argb))
					colour = Color.FromArgb(argb);
				else
					embedErrors.Add(
						String.Format(
							CultureInfo.InvariantCulture,
							"Invalid embed colour: {0}",
							embed.Colour));

			if (embed.Author != null && String.IsNullOrWhiteSpace(embed.Author.Name))
			{
				embedErrors.Add("Null or whitespace embed author name!");
				embed.Author = null;
			}

			List<IEmbedField> fields = null;
			if (embed.Fields != null)
			{
				fields = new List<IEmbedField>();
				var i = -1;
				foreach (var field in embed.Fields)
				{
					++i;
					var invalid = false;
					if (String.IsNullOrWhiteSpace(field.Name))
					{
						embedErrors.Add(
							String.Format(
								CultureInfo.InvariantCulture,
								"Null or whitespace field author at index {0}!",
								i));
						invalid = true;
					}

					if (String.IsNullOrWhiteSpace(field.Value))
					{
						embedErrors.Add(
							String.Format(
								CultureInfo.InvariantCulture,
								"Null or whitespace field author at index {0}!",
								i));
						invalid = true;
					}

					if (invalid)
						continue;

					fields.Add(new EmbedField(field.Name, field.Value)
					{
						IsInline = field.IsInline ?? default(Optional<bool>),
					});
				}
			}

			if (embed.Footer != null && String.IsNullOrWhiteSpace(embed.Footer.Text))
			{
				embedErrors.Add("Null or whitespace embed footer text!");
				embed.Footer = null;
			}

			if (embed.Image != null && String.IsNullOrWhiteSpace(embed.Image.Url))
			{
				embedErrors.Add("Null or whitespace embed image url!");
				embed.Image = null;
			}

			if (embed.Thumbnail != null && String.IsNullOrWhiteSpace(embed.Thumbnail.Url))
			{
				embedErrors.Add("Null or whitespace embed thumbnail url!");
				embed.Thumbnail = null;
			}

			Optional<DateTimeOffset> timestampOptional = default;
			if (embed.Timestamp != null)
				if (DateTimeOffset.TryParse(embed.Timestamp, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var timestamp))
					timestampOptional = timestamp.ToUniversalTime();
				else
					embedErrors.Add(
						String.Format(
							CultureInfo.InvariantCulture,
							"Invalid embed timestamp: {0}",
							embed.Timestamp));

			var discordEmbed = new Embed
			{
				Author = embed.Author != null
					? new EmbedAuthor(embed.Author.Name)
					{
						IconUrl = embed.Author.IconUrl ?? default(Optional<string>),
						ProxyIconUrl = embed.Author.ProxyIconUrl ?? default(Optional<string>),
						Url = embed.Author.Url ?? default(Optional<string>),
					}
					: default(Optional<IEmbedAuthor>),
				Colour = colour,
				Description = embed.Description ?? default(Optional<string>),
				Fields = fields ?? default(Optional<IReadOnlyList<IEmbedField>>),
				Footer = embed.Footer != null
					? new EmbedFooter(embed.Footer.Text)
					{
						IconUrl = embed.Footer.IconUrl ?? default(Optional<string>),
						ProxyIconUrl = embed.Footer.ProxyIconUrl ?? default(Optional<string>),
					}
					: default,
				Image = embed.Image != null
					? new EmbedImage(embed.Image.Url)
					{
						Width = embed.Image.Width ?? default(Optional<int>),
						Height = embed.Image.Height ?? default(Optional<int>),
						ProxyUrl = embed.Image.ProxyUrl ?? default(Optional<string>),
					}
					: default(Optional<IEmbedImage>),
				Provider = embed.Provider != null
					? new EmbedProvider
					{
						Name = embed.Provider.Name ?? default(Optional<string>),
						Url = embed.Provider.Url ?? default(Optional<string>),
					}
					: default(Optional<IEmbedProvider>),
				Thumbnail = embed.Thumbnail != null
					? new EmbedThumbnail(embed.Thumbnail.Url)
					{
						Width = embed.Thumbnail.Width ?? default(Optional<int>),
						Height = embed.Thumbnail.Height ?? default(Optional<int>),
						ProxyUrl = embed.Thumbnail.ProxyUrl ?? default(Optional<string>),
					}
					: default(Optional<IEmbedThumbnail>),
				Timestamp = timestampOptional,
				Title = embed.Title ?? default(Optional<string>),
				Url = embed.Url ?? default(Optional<string>),
				Video = embed.Video != null
					? new EmbedVideo
					{
						Url = embed.Video.Url ?? default(Optional<string>),
						Width = embed.Video.Width ?? default(Optional<int>),
						Height = embed.Video.Height ?? default(Optional<int>),
						ProxyUrl = embed.Video.ProxyUrl ?? default(Optional<string>),
					}
					: default(Optional<IEmbedVideo>),
			};

			var result = new List<IEmbed> { discordEmbed };

			if (embedErrors.Count > 0)
			{
				var joinedErrors = String.Join(Environment.NewLine, embedErrors);
				Logger.LogError("Embed description contains errors:{newLine}{issues}", Environment.NewLine, joinedErrors);
				result.Add(new Embed
				{
					Title = "TGS Embed Errors",
					Description = joinedErrors,
					Colour = Color.Red,
					Footer = new EmbedFooter("Please report this to your codebase's maintainers."),
					Timestamp = DateTimeOffset.UtcNow,
				});
			}

			return result;
		}
		#pragma warning restore CA1502
	}
#pragma warning restore CA1506
}
