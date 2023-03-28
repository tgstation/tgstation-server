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
using Remora.Discord.API.Objects;
using Remora.Discord.Gateway;
using Remora.Discord.Gateway.Extensions;
using Remora.Rest.Core;
using Remora.Results;

using Tgstation.Server.Api.Models;
using Tgstation.Server.Host.Components.Interop;
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
		/// The bot's username at the time of connection.
		/// </summary>
		string initialUserName;

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
			if (replyTo != null && replyTo is DiscordMessage discordMessage)
			{
				replyToReference = discordMessage.MessageReference;
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
					ct: cancellationToken);

				if (!result.IsSuccess)
					Logger.LogWarning(
						"Failed to send to channel {0}: {1}",
						channelId,
						result.Error);
			}

			try
			{
				if (channelId == 0)
				{
					var usersClient = serviceProvider.GetRequiredService<IDiscordRestUserAPI>();
					var currentGuildsResponse = await usersClient.GetCurrentUserGuildsAsync(ct: cancellationToken);
					if (!currentGuildsResponse.IsSuccess)
					{
						Logger.LogWarning(
							"Error retrieving current discord guilds: {0}",
							currentGuildsResponse.Error.Message);
						return;
					}

					var guildsClient = serviceProvider.GetRequiredService<IDiscordRestGuildAPI>();

					var guildsChannelsTasks = currentGuildsResponse.Entity.Select(
						guild => guildsClient.GetGuildChannelsAsync(guild.ID.Value, cancellationToken));

					await Task.WhenAll(guildsChannelsTasks);

					var unmappedTextChannels = guildsChannelsTasks
						.Select(task => task.Result)
						.SelectMany(guildChannels => guildChannels.Entity)
						.Where(guildChannel => guildChannel.Type == ChannelType.GuildText);

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

			Logger.LogTrace("Attempting to post deploy embed to channel {0}...", channelId);
			var channelsClient = serviceProvider.GetRequiredService<IDiscordRestChannelAPI>();

			var messageResponse = await channelsClient.CreateMessageAsync(
				new Snowflake(channelId),
				"DM: Deployment in Progress...",
				embeds: new List<IEmbed> { embed },
				ct: cancellationToken)
				;

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
						;

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
						;

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
					shouldNotAnswer = !mappedChannels.Contains(messageCreateEvent.ChannelID.Value) && !mappedChannels.Contains(0);

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
				var messageGuildResponse = await guildsClient.GetGuildAsync(messageCreateEvent.GuildID.Value, false, cancellationToken);
				if (messageGuildResponse.IsSuccess)
					guildName = messageGuildResponse.Entity.Name;
				else
					Logger.LogWarning(
						"Failed to get channel {0} in response to message {1}!",
						messageCreateEvent.ChannelID,
						messageCreateEvent.ID);
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
						Logger.LogWarning("Unable to retrieve current user: {0}", currentUserResult.Error.Message);
						throw new JobException(ErrorCode.ChatCannotConnectProvider);
					}

					currentUserId = currentUserResult.Entity.ID;
					initialUserName = currentUserResult.Entity.Username;
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
				Logger.LogWarning("Gateway issue: {0}", gatewayResult.Error.Message);

			localGatewayCts.Dispose();
		}

		/// <inheritdoc />
		protected override async Task<IReadOnlyCollection<Tuple<Models.ChatChannel, ChannelRepresentation>>> MapChannelsImpl(IEnumerable<Models.ChatChannel> channels, CancellationToken cancellationToken)
		{
			if (channels == null)
				throw new ArgumentNullException(nameof(channels));

			var remapRequired = false;

			async Task<Tuple<Models.ChatChannel, ChannelRepresentation>> GetModelChannelFromDBChannel(Models.ChatChannel channelFromDB)
			{
				if (!channelFromDB.DiscordChannelId.HasValue)
					throw new InvalidOperationException("ChatChannel missing DiscordChannelId!");

				var channelId = channelFromDB.DiscordChannelId.Value;
				string connectionName;
				string friendlyName;
				if (channelId == 0)
				{
					connectionName = initialUserName;
					friendlyName = "(Unmapped accessible channels)";
				}
				else
				{
					var channelsClient = serviceProvider.GetRequiredService<IDiscordRestChannelAPI>();
					var discordChannelResponse = await channelsClient.GetChannelAsync(new Snowflake(channelId), cancellationToken);
					if (!discordChannelResponse.IsSuccess)
					{
						Logger.LogWarning("Error retrieving discord channel {0}: {1}", channelId, discordChannelResponse.Error.Message);
						remapRequired = true;
						return null;
					}

					var channelType = discordChannelResponse.Entity.Type;
					if (channelType != ChannelType.GuildText && channelType != ChannelType.GuildAnnouncement)
					{
						Logger.LogWarning("Cound not map channel {0}! Incorrect type: {1}", channelId, discordChannelResponse.Entity.Type);
						return null;
					}

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
						remapRequired = true;
						return null;
					}

					connectionName = guildsResponse.Entity.Name;
				}

				var channelModel = new ChannelRepresentation
				{
					RealId = channelId,
					IsAdminChannel = channelFromDB.IsAdminChannel == true,
					ConnectionName = connectionName,
					FriendlyName = friendlyName,
					IsPrivateChannel = false,
					Tag = channelFromDB.Tag,
					EmbedsSupported = true,
				};

				Logger.LogTrace("Mapped channel {0}: {1}", channelModel.RealId, channelModel.FriendlyName);
				return Tuple.Create(channelFromDB, channelModel);
			}

			var tasks = channels
				.Select(x => GetModelChannelFromDBChannel(x))
				.ToList();

			await Task.WhenAll(tasks);

			var enumerator = tasks
				.Select(x => x.Result)
				.Where(x => x != null)
				.ToList();

			lock (mappedChannels)
			{
				mappedChannels.Clear();
				mappedChannels.AddRange(enumerator.Select(x => x.Item2.RealId));
			}

			if (remapRequired)
				EnqueueMessage(null);

			return enumerator;
		}

		/// <summary>
		/// Convert a <see cref="ChatEmbed"/> to an <see cref="IEmbed"/> parameters.
		/// </summary>
		/// <param name="embed">The <see cref="ChatEmbed"/> to convert.</param>
		/// <returns>The parameter for sending a single <see cref="IEmbed"/>.</returns>
		#pragma warning disable CA1502
		private Optional<IReadOnlyList<IEmbed>> ConvertEmbed(ChatEmbed embed)
		{
			if (embed == null)
				return default;

			List<string> embedErrors = new List<string>();
			Optional<Color> colour = default;
			if (embed.Colour != null)
				if (Int32.TryParse(embed.Colour.Substring(1), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var argb))
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
