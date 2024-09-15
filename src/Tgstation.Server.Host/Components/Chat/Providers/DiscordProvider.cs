using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

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
using Tgstation.Server.Api.Models.Internal;
using Tgstation.Server.Common.Extensions;
using Tgstation.Server.Host.Components.Interop;
using Tgstation.Server.Host.Configuration;
using Tgstation.Server.Host.Extensions;
using Tgstation.Server.Host.Jobs;
using Tgstation.Server.Host.Models;
using Tgstation.Server.Host.System;
using Tgstation.Server.Host.Utils;

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
		static readonly ChannelType[] SupportedGuildChannelTypes =
		[
			ChannelType.GuildText,
			ChannelType.GuildAnnouncement,
			ChannelType.PrivateThread,
			ChannelType.PublicThread,
		];

		/// <summary>
		/// The <see cref="IAssemblyInformationProvider"/> for the <see cref="DiscordProvider"/>.
		/// </summary>
		readonly IAssemblyInformationProvider assemblyInformationProvider;

		/// <summary>
		/// The <see cref="GeneralConfiguration"/> <see cref="IOptionsMonitor{TOptions}"/> for the <see cref="DiscordProvider"/>.
		/// </summary>
		readonly IOptionsMonitor<GeneralConfiguration> generalConfigurationOptions;

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
		CancellationTokenSource? gatewayCts;

		/// <summary>
		/// The <see cref="TaskCompletionSource"/> for the initial gateway connection event.
		/// </summary>
		TaskCompletionSource? gatewayReadyTcs;

		/// <summary>
		/// The <see cref="Task"/> representing the lifetime of the client.
		/// </summary>
		Task<Result>? gatewayTask;

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
		/// Initializes a new instance of the <see cref="DiscordProvider"/> class.
		/// </summary>
		/// <param name="jobManager">The <see cref="IJobManager"/> for the <see cref="Provider"/>.</param>
		/// <param name="asyncDelayer">The <see cref="IAsyncDelayer"/> for the <see cref="Provider"/>.</param>
		/// <param name="logger">The <see cref="ILogger"/> for the <see cref="Provider"/>.</param>
		/// <param name="assemblyInformationProvider">The value of <see cref="assemblyInformationProvider"/>.</param>
		/// <param name="chatBot">The <see cref="ChatBot"/> for the <see cref="Provider"/>.</param>
		/// <param name="generalConfigurationOptions">The value of <see cref="generalConfigurationOptions"/>.</param>
		public DiscordProvider(
			IJobManager jobManager,
			IAsyncDelayer asyncDelayer,
			ILogger<DiscordProvider> logger,
			IAssemblyInformationProvider assemblyInformationProvider,
			IOptionsMonitor<GeneralConfiguration> generalConfigurationOptions,
			ChatBot chatBot)
			: base(jobManager, asyncDelayer, logger, chatBot)
		{
			this.assemblyInformationProvider = assemblyInformationProvider ?? throw new ArgumentNullException(nameof(assemblyInformationProvider));
			this.generalConfigurationOptions = generalConfigurationOptions ?? throw new ArgumentNullException(nameof(generalConfigurationOptions));

			mappedChannels = new List<ulong>();
			connectDisconnectLock = new object();

			var csb = new DiscordConnectionStringBuilder(chatBot.ConnectionString!);
			var botToken = csb.BotToken!;
			outputDisplayType = csb.DMOutputDisplay;
			deploymentBranding = csb.DeploymentBranding;

			serviceProvider = new ServiceCollection()
				.AddDiscordGateway(serviceProvider => botToken)
				.Configure<DiscordGatewayClientOptions>(options => options.Intents |= GatewayIntents.MessageContents)
				.AddSingleton(serviceProvider => (IDiscordResponders)this)
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
		public override async ValueTask SendMessage(Message? replyTo, MessageContent message, ulong channelId, CancellationToken cancellationToken)
		{
			ArgumentNullException.ThrowIfNull(message);

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
			async ValueTask SendToChannel(Snowflake channelId)
			{
				if (message.Text == null)
				{
					Logger.LogWarning(
						"Failed to send to channel {channelId}: Message was null!",
						channelId);

					await channelsClient.CreateMessageAsync(
						channelId,
						"TGS: Could not send message to Discord. Message was `null`!",
						messageReference: replyToReference,
						allowedMentions: allowedMentions,
						ct: cancellationToken);

					return;
				}

				var result = await channelsClient.CreateMessageAsync(
					channelId,
					message.Text,
					embeds: embeds,
					messageReference: replyToReference,
					allowedMentions: allowedMentions,
					ct: cancellationToken);

				if (!result.IsSuccess)
				{
					Logger.LogWarning(
						"Failed to send to channel {channelId}: {result}",
						channelId,
						result.LogFormat());

					if (result.Error is RestResultError<RestError> restError && restError.Error.Code == DiscordError.InvalidFormBody)
						await channelsClient.CreateMessageAsync(
							channelId,
							"TGS: Could not send message to Discord. Body was malformed or too long",
							messageReference: replyToReference,
							allowedMentions: allowedMentions,
							ct: cancellationToken);
				}
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
						await ValueTaskExtensions.WhenAll(
							unmappedTextChannels.Select(
								x => SendToChannel(x.ID)));
					}

					return;
				}

				await SendToChannel(new Snowflake(channelId));
			}
			catch (Exception e) when (e is not OperationCanceledException)
			{
				Logger.LogWarning(e, "Error sending discord message!");
			}
		}

		/// <inheritdoc />
		public override async ValueTask<Func<string?, string, ValueTask<Func<bool, ValueTask>>>> SendUpdateMessage(
			Models.RevisionInformation revisionInformation,
			EngineVersion engineVersion,
			DateTimeOffset? estimatedCompletionTime,
			string? gitHubOwner,
			string? gitHubRepo,
			ulong channelId,
			bool localCommitPushed,
			CancellationToken cancellationToken)
		{
			ArgumentNullException.ThrowIfNull(revisionInformation);
			ArgumentNullException.ThrowIfNull(engineVersion);

			localCommitPushed |= revisionInformation.CommitSha == revisionInformation.OriginCommitSha;

			var fields = BuildUpdateEmbedFields(revisionInformation, engineVersion, gitHubOwner, gitHubRepo, localCommitPushed);
			Optional<IEmbedAuthor> author = new EmbedAuthor(assemblyInformationProvider.VersionPrefix)
			{
				Url = "https://github.com/tgstation/tgstation-server",
				IconUrl = "https://cdn.discordapp.com/attachments/1114451486374637629/1151650846019432448/tgs.png", // 404's in browsers but works in Discord
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

			var prefix = GetEngineCompilerPrefix(engineVersion.Engine!.Value);
			var messageResponse = await channelsClient.CreateMessageAsync(
				new Snowflake(channelId),
				$"{prefix}: Deployment in progress...",
				embeds: new List<IEmbed> { embed },
				ct: cancellationToken);

			if (!messageResponse.IsSuccess)
				Logger.LogWarning("Failed to post deploy embed to channel {channelId}: {result}", channelId, messageResponse.LogFormat());

			return async (errorMessage, dreamMakerOutput) =>
			{
				var completionString = errorMessage == null ? "Pending" : "Failed";

				Embed CreateUpdatedEmbed(string message, Color color) => new()
				{
					Author = embed.Author,
					Colour = color,
					Description = message,
					Fields = fields,
					Title = embed.Title,
					Footer = new EmbedFooter(
						completionString),
					Timestamp = DateTimeOffset.UtcNow,
				};

				if (errorMessage == null)
					embed = CreateUpdatedEmbed(
						"The deployment completed successfully and will be available at the next server reboot.",
						Color.Blue);
				else
					embed = CreateUpdatedEmbed(
						"The deployment failed.",
						Color.Red);

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
							"Compiler Output",
							$"```{Environment.NewLine}{dreamMakerOutput}{Environment.NewLine}```",
							false));
				}

				if (errorMessage != null)
					fields.Add(new EmbedField(
						"Error Message",
						errorMessage,
						false));

				var updatedMessageText = errorMessage == null ? $"{prefix}: Deployment pending reboot..." : $"{prefix}: Deployment failed!";

				IMessage? updatedMessage = null;
				async ValueTask CreateUpdatedMessage()
				{
					var createUpdatedMessageResponse = await channelsClient.CreateMessageAsync(
						new Snowflake(channelId),
						updatedMessageText,
						embeds: new List<IEmbed> { embed },
						ct: cancellationToken);

					if (!createUpdatedMessageResponse.IsSuccess)
						Logger.LogWarning(
							"Creating updated deploy embed failed: {result}",
							createUpdatedMessageResponse.LogFormat());
					else
						updatedMessage = createUpdatedMessageResponse.Entity;
				}

				if (!messageResponse.IsSuccess)
					await CreateUpdatedMessage();
				else
				{
					var editResponse = await channelsClient.EditMessageAsync(
						new Snowflake(channelId),
						messageResponse.Entity.ID,
						updatedMessageText,
						embeds: new List<IEmbed> { embed },
						ct: cancellationToken);

					if (!editResponse.IsSuccess)
					{
						Logger.LogWarning(
							"Updating deploy embed {messageId} failed, attempting new post: {result}",
							messageResponse.Entity.ID,
							editResponse.LogFormat());
						await CreateUpdatedMessage();
					}
					else
						updatedMessage = editResponse.Entity;
				}

				return async (active) =>
				{
					if (updatedMessage == null || errorMessage != null)
						return;

					if (active)
					{
						completionString = "Succeeded";
						updatedMessageText = $"{prefix}: Deployment succeeded!";
						embed = CreateUpdatedEmbed(
							"The deployment completed successfully and was applied to server.",
							Color.Green);
					}
					else
					{
						completionString = "Inactive";
						embed = CreateUpdatedEmbed(
							"This deployment has been superceeded by a new one.",
							Color.Gray);
					}

					var editResponse = await channelsClient.EditMessageAsync(
						new Snowflake(channelId),
						updatedMessage.ID,
						updatedMessageText,
						embeds: new List<IEmbed> { embed },
						ct: cancellationToken);

					if (!editResponse.IsSuccess)
						Logger.LogWarning(
							"Finalizing deploy embed {messageId} failed: {result}",
							messageResponse.Entity.ID,
							editResponse.LogFormat());
				};
			};
		}

		/// <inheritdoc />
		public async Task<Result> RespondAsync(IMessageCreate messageCreateEvent, CancellationToken cancellationToken)
		{
			ArgumentNullException.ThrowIfNull(messageCreateEvent);

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

			var result = new DiscordMessage(
				new ChatUser(
					new ChannelRepresentation(
						pm ? messageCreateEvent.Author.Username : guildName,
						channelResponse.Entity.Name.Value!,
						messageCreateEvent.ChannelID.Value)
					{
						IsPrivateChannel = pm,
						EmbedsSupported = true,

						// isAdmin and Tag populated by manager
					},
					messageCreateEvent.Author.Username,
					NormalizeMentions($"<@{messageCreateEvent.Author.ID}>"),
					messageCreateEvent.Author.ID.Value),
				content,
				messageReference);

			EnqueueMessage(result);
			return Result.FromSuccess();
		}

		/// <inheritdoc />
		public Task<Result> RespondAsync(IReady readyEvent, CancellationToken cancellationToken)
		{
			ArgumentNullException.ThrowIfNull(readyEvent);

			Logger.LogTrace("Gatway ready. Version: {version}", readyEvent.Version);
			gatewayReadyTcs?.TrySetResult();
			return Task.FromResult(Result.FromSuccess());
		}

		/// <inheritdoc />
		protected override async ValueTask Connect(CancellationToken cancellationToken)
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

				using var gatewayConnectionAbortRegistration = cancellationToken.Register(() => gatewayReadyTcs.TrySetCanceled(cancellationToken));
				gatewayCancellationToken.Register(() => Logger.LogTrace("Stopping gateway client..."));

				// reconnects keep happening until we stop or it faults, our auto-reconnector will handle the latter
				localGatewayTask = gatewayClient.RunAsync(gatewayCancellationToken);
				try
				{
					await Task.WhenAny(gatewayReadyTcs.Task, localGatewayTask);

					cancellationToken.ThrowIfCancellationRequested();
					if (localGatewayTask.IsCompleted)
						throw new JobException(ErrorCode.ChatCannotConnectProvider);

					var userClient = serviceProvider.GetRequiredService<IDiscordRestUserAPI>();

					using var localCombinedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, gatewayCancellationToken);
					var localCombinedCancellationToken = localCombinedCts.Token;
					var currentUserResult = await userClient.GetCurrentUserAsync(localCombinedCancellationToken);
					if (!currentUserResult.IsSuccess)
					{
						localCombinedCancellationToken.ThrowIfCancellationRequested();
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
				await DisconnectImpl(CancellationToken.None);
				throw;
			}
		}

		/// <inheritdoc />
		protected override async ValueTask DisconnectImpl(CancellationToken cancellationToken)
		{
			Task<Result> localGatewayTask;
			CancellationTokenSource localGatewayCts;
			lock (connectDisconnectLock)
			{
				localGatewayTask = gatewayTask!;
				localGatewayCts = gatewayCts!;
				gatewayTask = null;
				gatewayCts = null;
				if (localGatewayTask == null)
					return;
			}

			localGatewayCts.Cancel();
			var gatewayResult = await localGatewayTask;

			Logger.LogTrace("Gateway task complete");
			if (!gatewayResult.IsSuccess)
				Logger.LogWarning("Gateway issue: {result}", gatewayResult.LogFormat());

			localGatewayCts.Dispose();
		}

		/// <inheritdoc />
		protected override async ValueTask<Dictionary<Models.ChatChannel, IEnumerable<ChannelRepresentation>>> MapChannelsImpl(IEnumerable<Models.ChatChannel> channels, CancellationToken cancellationToken)
		{
			ArgumentNullException.ThrowIfNull(channels);

			var remapRequired = false;
			var guildsClient = serviceProvider.GetRequiredService<IDiscordRestGuildAPI>();
			var guildTasks = new ConcurrentDictionary<Snowflake, Task<Result<IGuild>>>();

			async ValueTask<Tuple<Models.ChatChannel, IEnumerable<ChannelRepresentation>>?> GetModelChannelFromDBChannel(Models.ChatChannel channelFromDB)
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

					var remapConditional = !(discordChannelResponse.Error is RestResultError<RestError> restResultError
						&& (restResultError.Error?.Code == DiscordError.MissingAccess
						|| restResultError.Error?.Code == DiscordError.UnknownChannel));

					if (remapConditional)
					{
						Logger.Log(
							remapRequired
								? LogLevel.Trace
								: LogLevel.Debug,
							"Error on channel {channelId} is not an access/thread issue. Will retry remap...",
							channelId);
						remapRequired = true;
					}

					return null;
				}

				var channelType = discordChannelResponse.Entity.Type;
				if (!SupportedGuildChannelTypes.Contains(channelType))
				{
					Logger.LogWarning("Cound not map channel {channelId}! Incorrect type: {channelType}", channelId, discordChannelResponse.Entity.Type);
					return null;
				}

				var guildId = discordChannelResponse.Entity.GuildID.Value;

				var added = false;
				var guildsResponse = await guildTasks.GetOrAdd(
					guildId,
					localGuildId =>
					{
						added = true;
						return guildsClient.GetGuildAsync(
							localGuildId,
							false,
							cancellationToken);
					});
				if (!guildsResponse.IsSuccess)
				{
					if (added)
					{
						Logger.LogWarning(
							"Error retrieving discord guild {guildID}: {result}",
							guildId,
							guildsResponse.LogFormat());
						remapRequired = true;
					}

					return null;
				}

				var connectionName = guildsResponse.Entity.Name;

				var channelModel = new ChannelRepresentation(
					guildsResponse.Entity.Name,
					discordChannelResponse.Entity.Name.Value!,
					channelId)
				{
					IsAdminChannel = channelFromDB.IsAdminChannel == true,
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
				.Select(GetModelChannelFromDBChannel);

			var channelTuples = await ValueTaskExtensions.WhenAll(tasks.ToList());

			var list = channelTuples
				.Where(x => x != null)
				.Cast<Tuple<Models.ChatChannel, IEnumerable<ChannelRepresentation>>>() // NRT my beloathed
				.ToList();

			var channelIdZeroModel = channels.FirstOrDefault(x => x.DiscordChannelId == 0);
			if (channelIdZeroModel != null)
			{
				Logger.LogInformation("Mapping ALL additional accessible text channels");
				var allAccessibleChannels = await GetAllAccessibleTextChannels(cancellationToken);
				var unmappedTextChannels = allAccessibleChannels
					.Where(x => !tasks.Any(task => task.Result != null && new Snowflake(task.Result.Item1.DiscordChannelId!.Value) == x.ID));

				async ValueTask<Tuple<Models.ChatChannel, IEnumerable<ChannelRepresentation>>> CreateMappingsForUnmappedChannels()
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
					unmappedTasks.Add(Task.FromResult<ChannelRepresentation?>(
						new ChannelRepresentation(
							"(Unknown Discord Guilds)",
							"(Unknown Discord Channels)",
							0)
						{
							IsAdminChannel = channelIdZeroModel.IsAdminChannel!.Value,
							EmbedsSupported = true,
							Tag = channelIdZeroModel.Tag,
						}));

					await Task.WhenAll(unmappedTasks);
					return Tuple.Create<Models.ChatChannel, IEnumerable<ChannelRepresentation>>(
						channelIdZeroModel,
						unmappedTasks
							.Select(x => x.Result)
							.Where(x => x != null)
							.Cast<ChannelRepresentation>() // NRT my beloathed
							.ToList());
				}

				var task = CreateMappingsForUnmappedChannels();
				var tuple = await task;
				list.Add(tuple);
			}

			lock (mappedChannels)
			{
				mappedChannels.Clear();
				mappedChannels.AddRange(list.SelectMany(x => x.Item2).Select(x => x.RealId));
			}

			if (remapRequired)
			{
				Logger.LogWarning("Some channels failed to load with unknown errors. We will request that these be remapped, but it may result in communication spam. Please check prior logs and report an issue if this occurs.");
				EnqueueMessage(null);
			}

			return new Dictionary<Models.ChatChannel, IEnumerable<ChannelRepresentation>>(list.Select(x => new KeyValuePair<Models.ChatChannel, IEnumerable<ChannelRepresentation>>(x.Item1, x.Item2)));
		}

		/// <summary>
		/// Get all text <see cref="IChannel"/>s accessible to and supported by the bot.
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in an <see cref="IEnumerable{T}"/> of accessible and compatible <see cref="IChannel"/>s.</returns>
		async ValueTask<IEnumerable<IChannel>> GetAllAccessibleTextChannels(CancellationToken cancellationToken)
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

			async ValueTask<IEnumerable<IChannel>> GetGuildChannels(IPartialGuild guild)
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
				.Select(GetGuildChannels);

			var guildsChannels = await ValueTaskExtensions.WhenAll(guildsChannelsTasks, currentGuildsResponse.Entity.Count);

			var allAccessibleChannels = guildsChannels
				.SelectMany(channels => channels)
				.Where(guildChannel => SupportedGuildChannelTypes.Contains(guildChannel.Type));

			return allAccessibleChannels;
		}

		/// <summary>
		/// Create a <see cref="List{T}"/> of <see cref="IEmbedField"/>s for a discord update embed.
		/// </summary>
		/// <param name="revisionInformation">The <see cref="RevisionInformation"/> of the deployment.</param>
		/// <param name="engineVersion">The <see cref="EngineVersion"/> of the deployment.</param>
		/// <param name="gitHubOwner">The repository GitHub owner, if any.</param>
		/// <param name="gitHubRepo">The repository GitHub name, if any.</param>
		/// <param name="localCommitPushed"><see langword="true"/> if the local deployment commit was pushed to the remote repository.</param>
		/// <returns>A new <see cref="List{T}"/> of <see cref="IEmbedField"/>s to use.</returns>
		List<IEmbedField> BuildUpdateEmbedFields(
			Models.RevisionInformation revisionInformation,
			EngineVersion engineVersion,
			string? gitHubOwner,
			string? gitHubRepo,
			bool localCommitPushed)
		{
			bool gitHub = gitHubOwner != null && gitHubRepo != null;
			var engineField = engineVersion.Engine!.Value switch
			{
				EngineType.Byond => new EmbedField(
					"BYOND Version",
					$"{engineVersion.Version!.Major}.{engineVersion.Version.Minor}{(engineVersion.CustomIteration.HasValue ? $".{engineVersion.CustomIteration.Value}" : String.Empty)}",
					true),
				EngineType.OpenDream => new EmbedField(
					"OpenDream Version",
					$"[{engineVersion.SourceSHA![..7]}]({generalConfigurationOptions.CurrentValue.OpenDreamGitUrl}/commit/{engineVersion.SourceSHA})",
					true),
				_ => throw new InvalidOperationException($"Invaild EngineType: {engineVersion.Engine.Value}"),
			};

			var revisionSha = revisionInformation.CommitSha!;
			var revisionOriginSha = revisionInformation.OriginCommitSha!;
			var fields = new List<IEmbedField>
			{
				engineField,
			};

			if (gitHubOwner == null || gitHubRepo == null)
				return fields;

			fields.Add(
				new EmbedField(
					"Local Commit",
					localCommitPushed && gitHub
						? $"[{revisionSha[..7]}](https://github.com/{gitHubOwner}/{gitHubRepo}/commit/{revisionSha})"
						: revisionSha[..7],
					true));

			fields.Add(
				new EmbedField(
					"Branch Commit",
					gitHub
						? $"[{revisionOriginSha[..7]}](https://github.com/{gitHubOwner}/{gitHubRepo}/commit/{revisionOriginSha})"
						: revisionOriginSha[..7],
					true));

			fields.AddRange((revisionInformation.ActiveTestMerges ?? Enumerable.Empty<RevInfoTestMerge>())
				.Select(x => x.TestMerge)
				.Select(x => new EmbedField(
					$"#{x.Number}",
					$"[{x.TitleAtMerge}]({x.Url}) by _[@{x.Author}](https://github.com/{x.Author})_{Environment.NewLine}Commit: [{x.TargetCommitSha![..7]}](https://github.com/{gitHubOwner}/{gitHubRepo}/commit/{x.TargetCommitSha}){(String.IsNullOrWhiteSpace(x.Comment) ? String.Empty : $"{Environment.NewLine}_**{x.Comment}**_")}",
					false)));

			return fields;
		}

		/// <summary>
		/// Convert a <see cref="ChatEmbed"/> to an <see cref="IEmbed"/> parameters.
		/// </summary>
		/// <param name="embed">The <see cref="ChatEmbed"/> to convert.</param>
		/// <returns>The parameter for sending a single <see cref="IEmbed"/>.</returns>
#pragma warning disable CA1502
		Optional<IReadOnlyList<IEmbed>> ConvertEmbed(ChatEmbed? embed)
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

			List<IEmbedField>? fields = null;
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
								"Null or whitespace field name at index {0}!",
								i));
						invalid = true;
					}

					if (String.IsNullOrWhiteSpace(field.Value))
					{
						embedErrors.Add(
							String.Format(
								CultureInfo.InvariantCulture,
								"Null or whitespace field value at index {0}!",
								i));
						invalid = true;
					}

					if (invalid)
						continue;

					fields.Add(new EmbedField(field.Name!, field.Value!)
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
					? new EmbedAuthor(embed.Author.Name!)
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
					? (Optional<IEmbedFooter>)new EmbedFooter(embed.Footer.Text!)
					{
						IconUrl = embed.Footer.IconUrl ?? default(Optional<string>),
						ProxyIconUrl = embed.Footer.ProxyIconUrl ?? default(Optional<string>),
					}
					: default,
				Image = embed.Image != null
					? new EmbedImage(embed.Image.Url!)
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
					? new EmbedThumbnail(embed.Thumbnail.Url!)
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
