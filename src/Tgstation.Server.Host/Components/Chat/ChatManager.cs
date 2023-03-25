using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Serilog.Context;

using Tgstation.Server.Api.Models.Internal;
using Tgstation.Server.Host.Components.Chat.Commands;
using Tgstation.Server.Host.Components.Chat.Providers;
using Tgstation.Server.Host.Core;
using Tgstation.Server.Host.Extensions;

namespace Tgstation.Server.Host.Components.Chat
{
	/// <inheritdoc />
	// TODO: Decomplexify
#pragma warning disable CA1506
	sealed class ChatManager : IChatManager, IRestartHandler
	{
		/// <summary>
		/// The common bot mention.
		/// </summary>
		public const string CommonMention = "!tgs";

		/// <summary>
		/// The <see cref="IProviderFactory"/> for the <see cref="ChatManager"/>.
		/// </summary>
		readonly IProviderFactory providerFactory;

		/// <summary>
		/// The <see cref="ICommandFactory"/> for the <see cref="ChatManager"/>.
		/// </summary>
		readonly ICommandFactory commandFactory;

		/// <summary>
		/// The <see cref="IRestartRegistration"/> for the <see cref="ChatManager"/>.
		/// </summary>
		readonly IRestartRegistration restartRegistration;

		/// <summary>
		/// The <see cref="ILoggerFactory"/> for the <see cref="ChatManager"/>.
		/// </summary>
		readonly ILoggerFactory loggerFactory;

		/// <summary>
		/// The <see cref="ILogger"/> for the <see cref="ChatManager"/>.
		/// </summary>
		readonly ILogger<ChatManager> logger;

		/// <summary>
		/// Unchanging <see cref="ICommand"/>s in the <see cref="ChatManager"/> mapped by <see cref="ICommand.Name"/>.
		/// </summary>
		readonly IDictionary<string, ICommand> builtinCommands;

		/// <summary>
		/// Map of <see cref="IProvider"/>s in use, keyed by <see cref="ChatBotSettings"/> <see cref="Api.Models.EntityId.Id"/>.
		/// </summary>
		readonly IDictionary<long, IProvider> providers;

		/// <summary>
		/// Map of <see cref="ChannelRepresentation.RealId"/>s to <see cref="ChannelMapping"/>s.
		/// </summary>
		readonly IDictionary<ulong, ChannelMapping> mappedChannels;

		/// <summary>
		/// The active <see cref="IChatTrackingContext"/>s for the <see cref="ChatManager"/>.
		/// </summary>
		readonly IList<IChatTrackingContext> trackingContexts;

		/// <summary>
		/// The <see cref="CancellationTokenSource"/> for <see cref="chatHandler"/>.
		/// </summary>
		readonly CancellationTokenSource handlerCts;

		/// <summary>
		/// The active <see cref="Models.ChatBot"/> for the <see cref="ChatManager"/>.
		/// </summary>
		readonly List<Models.ChatBot> activeChatBots;

		/// <summary>
		/// Used for various lock statements throughout this <see langword="class"/>.
		/// </summary>
		readonly object synchronizationLock;

		/// <summary>
		/// The <see cref="ICustomCommandHandler"/> for the <see cref="ChangeChannels(long, IEnumerable{Models.ChatChannel}, CancellationToken)"/>.
		/// </summary>
		ICustomCommandHandler customCommandHandler;

		/// <summary>
		/// The <see cref="Task"/> that monitors incoming chat messages.
		/// </summary>
		Task chatHandler;

		/// <summary>
		/// A <see cref="Task"/> that represents the <see cref="IProvider"/>s initial connection.
		/// </summary>
		Task initialProviderConnectionsTask;

		/// <summary>
		/// A <see cref="Task"/> that represents all sent messages.
		/// </summary>
		Task messageSendTask;

		/// <summary>
		/// The <see cref="TaskCompletionSource{TResult}"/> that completes when <see cref="ChatBotSettings"/>s change.
		/// </summary>
		TaskCompletionSource<object> connectionsUpdated;

		/// <summary>
		/// Used for remapping <see cref="ChannelRepresentation.RealId"/>s.
		/// </summary>
		ulong channelIdCounter;

		/// <summary>
		/// The number of <see cref="Message"/>s processed.
		/// </summary>
		long messagesProcessed;

		/// <summary>
		/// Initializes a new instance of the <see cref="ChatManager"/> class.
		/// </summary>
		/// <param name="providerFactory">The value of <see cref="providerFactory"/>.</param>
		/// <param name="commandFactory">The value of <see cref="commandFactory"/>.</param>
		/// <param name="serverControl">The <see cref="IServerControl"/> to populate <see cref="restartRegistration"/> with.</param>
		/// <param name="loggerFactory">The value of <see cref="loggerFactory"/>.</param>
		/// <param name="logger">The value of <see cref="logger"/>.</param>
		/// <param name="initialChatBots">The <see cref="IEnumerable{T}"/> used to populate <see cref="activeChatBots"/>.</param>
		public ChatManager(
			IProviderFactory providerFactory,
			ICommandFactory commandFactory,
			IServerControl serverControl,
			ILoggerFactory loggerFactory,
			ILogger<ChatManager> logger,
			IEnumerable<Models.ChatBot> initialChatBots)
		{
			this.providerFactory = providerFactory ?? throw new ArgumentNullException(nameof(providerFactory));
			this.commandFactory = commandFactory ?? throw new ArgumentNullException(nameof(commandFactory));
			if (serverControl == null)
				throw new ArgumentNullException(nameof(serverControl));
			this.loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
			this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
			activeChatBots = initialChatBots?.ToList() ?? throw new ArgumentNullException(nameof(initialChatBots));

			restartRegistration = serverControl.RegisterForRestart(this);

			synchronizationLock = new object();

			builtinCommands = new Dictionary<string, ICommand>();
			providers = new Dictionary<long, IProvider>();
			mappedChannels = new Dictionary<ulong, ChannelMapping>();
			trackingContexts = new List<IChatTrackingContext>();
			handlerCts = new CancellationTokenSource();
			connectionsUpdated = new TaskCompletionSource<object>();

			messageSendTask = Task.CompletedTask;
			channelIdCounter = 1;
		}

		/// <inheritdoc />
		public async ValueTask DisposeAsync()
		{
			logger.LogTrace("Disposing...");
			restartRegistration.Dispose();
			handlerCts.Dispose();
			foreach (var providerKvp in providers)
				await providerKvp.Value.DisposeAsync();

			await messageSendTask;
		}

		/// <inheritdoc />
		public async Task ChangeChannels(long connectionId, IEnumerable<Models.ChatChannel> newChannels, CancellationToken cancellationToken)
		{
			if (newChannels == null)
				throw new ArgumentNullException(nameof(newChannels));

			logger.LogTrace("ChangeChannels {connectionId}...", connectionId);
			var provider = await RemoveProviderChannels(connectionId, false, cancellationToken);
			if (provider == null)
				return;

			if (!provider.Connected)
			{
				logger.LogDebug("Cannot map channels, provider {providerId} disconnected!", connectionId);
				return;
			}

			var results = await provider.MapChannels(newChannels, cancellationToken);
			try
			{
				lock (activeChatBots)
				{
					var botToUpdate = activeChatBots.FirstOrDefault(bot => bot.Id == connectionId);
					if (botToUpdate != null)
						botToUpdate.Channels = newChannels
							.Select(apiModel => new Models.ChatChannel
							{
								DiscordChannelId = apiModel.DiscordChannelId,
								IrcChannel = apiModel.IrcChannel,
								IsAdminChannel = apiModel.IsAdminChannel,
								IsUpdatesChannel = apiModel.IsUpdatesChannel,
								IsWatchdogChannel = apiModel.IsWatchdogChannel,
								Tag = apiModel.Tag,
							})
							.ToList();
				}

				var newMappings = results.Select(tuple => new ChannelMapping
				{
					IsWatchdogChannel = tuple.Item1.IsWatchdogChannel == true,
					IsUpdatesChannel = tuple.Item1.IsUpdatesChannel == true,
					IsAdminChannel = tuple.Item1.IsAdminChannel == true,
					ProviderChannelId = tuple.Item2.RealId,
					ProviderId = connectionId,
					Channel = tuple.Item2,
				});

				ulong baseId;
				lock (synchronizationLock)
				{
					baseId = channelIdCounter;
					channelIdCounter += (ulong)results.Count;
				}

				Task trackingContextUpdateTask;
				lock (mappedChannels)
				{
					lock (providers)
						if (!providers.TryGetValue(connectionId, out IProvider verify) || verify != provider) // aborted again
							return;
					foreach (var newMapping in newMappings)
					{
						var newId = baseId++;
						logger.LogTrace("Mapping channel {connectionName}:{channelFriendlyName} as {newId}", newMapping.Channel.ConnectionName, newMapping.Channel.FriendlyName, newId);
						mappedChannels.Add(newId, newMapping);
						newMapping.Channel.RealId = newId;
					}

					lock (trackingContexts)
						trackingContextUpdateTask = Task.WhenAll(
							trackingContexts.Select(
								x => x.UpdateChannels(
									mappedChannels.Select(y => y.Value.Channel).ToList(),
									cancellationToken)));
				}

				await trackingContextUpdateTask;
			}
			finally
			{
				provider.InitialMappingComplete();
			}
		}

		/// <inheritdoc />
		public async Task ChangeSettings(Models.ChatBot newSettings, CancellationToken cancellationToken)
		{
			if (newSettings == null)
				throw new ArgumentNullException(nameof(newSettings));

			logger.LogTrace("ChangeSettings...");

			Task disconnectTask;
			IProvider provider = null;
			lock (providers)
			{
				// raw settings changes forces a rebuild of the provider
				if (providers.ContainsKey(newSettings.Id.Value))
					disconnectTask = DeleteConnection(newSettings.Id.Value, cancellationToken);
				else
					disconnectTask = Task.CompletedTask;
				if (newSettings.Enabled.Value)
				{
					provider = providerFactory.CreateProvider(newSettings);
					providers.Add(newSettings.Id.Value, provider);
				}
			}

			lock (mappedChannels)
				foreach (var oldMappedChannelId in mappedChannels.Where(x => x.Value.ProviderId == newSettings.Id).Select(x => x.Key).ToList())
					mappedChannels.Remove(oldMappedChannelId);

			await disconnectTask;

			lock (synchronizationLock)
			{
				// same thread shennanigans
				var oldOne = connectionsUpdated;
				connectionsUpdated = new TaskCompletionSource<object>();
				oldOne.SetResult(null);
			}

			var reconnectionUpdateTask = provider?.SetReconnectInterval(
				newSettings.ReconnectionInterval.Value,
				newSettings.Enabled.Value)
				?? Task.CompletedTask;
			lock (activeChatBots)
			{
				var originalChatBot = activeChatBots.FirstOrDefault(bot => bot.Id == newSettings.Id);
				if (originalChatBot != null)
					activeChatBots.Remove(originalChatBot);

				activeChatBots.Add(new Models.ChatBot
				{
					Id = newSettings.Id,
					ConnectionString = newSettings.ConnectionString,
					Enabled = newSettings.Enabled,
					Name = newSettings.Name,
					ReconnectionInterval = newSettings.ReconnectionInterval,
					Provider = newSettings.Provider,
					Channels = newSettings.Channels,
				});
			}

			await reconnectionUpdateTask;
		}

		/// <inheritdoc />
		public void QueueMessage(string message, IEnumerable<ulong> channelIds)
		{
			if (message == null)
				throw new ArgumentNullException(nameof(message));
			if (channelIds == null)
				throw new ArgumentNullException(nameof(channelIds));

			var task = SendMessage(channelIds, null, message, handlerCts.Token);
			AddMessageTask(task);
		}

		/// <inheritdoc />
		public async Task QueueWatchdogMessage(string message, CancellationToken cancellationToken)
		{
			List<ulong> wdChannels = null;
			message = String.Format(CultureInfo.InvariantCulture, "WD: {0}", message);

			if (!initialProviderConnectionsTask.IsCompleted)
				logger.LogTrace("Waiting for initial provider connections before sending watchdog message...");

			await initialProviderConnectionsTask.WithToken(cancellationToken);

			// so it doesn't change while we're using it
			lock (mappedChannels)
				wdChannels = mappedChannels.Where(x => x.Value.IsWatchdogChannel).Select(x => x.Key).ToList();

			QueueMessage(message, wdChannels);
		}

		/// <inheritdoc />
		public Action<string, string> QueueDeploymentMessage(
			Models.RevisionInformation revisionInformation,
			Version byondVersion,
			DateTimeOffset? estimatedCompletionTime,
			string gitHubOwner,
			string gitHubRepo,
			bool localCommitPushed)
		{
			List<ulong> wdChannels;
			lock (mappedChannels) // so it doesn't change while we're using it
				wdChannels = mappedChannels.Where(x => x.Value.IsUpdatesChannel).Select(x => x.Key).ToList();

			logger.LogTrace("Sending deployment message for RevisionInformation: {revisionInfoId}", revisionInformation.Id);

			var callbacks = new List<Func<string, string, Task>>();

			var task = Task.WhenAll(
				wdChannels.Select(
					async x =>
					{
						ChannelMapping channelMapping;
						lock (mappedChannels)
							if (!mappedChannels.TryGetValue(x, out channelMapping))
								return;
						IProvider provider;
						lock (providers)
							if (!providers.TryGetValue(channelMapping.ProviderId, out provider))
								return;
						try
						{
							var callback = await provider.SendUpdateMessage(
								revisionInformation,
								byondVersion,
								estimatedCompletionTime,
								gitHubOwner,
								gitHubRepo,
								channelMapping.ProviderChannelId,
								localCommitPushed,
								handlerCts.Token)
								;

							callbacks.Add(callback);
						}
						catch (Exception ex)
						{
							logger.LogWarning(
								ex,
								"Error sending deploy message to provider {providerId}!",
								channelMapping.ProviderId);
						}
					}));

			AddMessageTask(task);

			return (errorMessage, dreamMakerOutput) => AddMessageTask(
				Task.WhenAll(
					callbacks.Select(
						x => x(
							errorMessage,
							dreamMakerOutput))));
		}

		/// <inheritdoc />
		public async Task StartAsync(CancellationToken cancellationToken)
		{
			foreach (var tgsCommand in commandFactory.GenerateCommands())
				builtinCommands.Add(tgsCommand.Name.ToUpperInvariant(), tgsCommand);
			var initialChatBots = activeChatBots.ToList();
			await Task.WhenAll(initialChatBots.Select(x => ChangeSettings(x, cancellationToken)));
			initialProviderConnectionsTask = InitialConnection();
			chatHandler = MonitorMessages(handlerCts.Token);
		}

		/// <inheritdoc />
		public async Task StopAsync(CancellationToken cancellationToken)
		{
			handlerCts.Cancel();
			if (chatHandler != null)
				await chatHandler;
			await Task.WhenAll(providers.Select(x => x.Key).Select(x => DeleteConnection(x, cancellationToken)));
			await messageSendTask;
		}

		/// <inheritdoc />
		public IChatTrackingContext CreateTrackingContext()
		{
			if (customCommandHandler == null)
				throw new InvalidOperationException("RegisterCommandHandler() hasn't been called!");

			IChatTrackingContext context = null;
			lock (mappedChannels)
				context = new ChatTrackingContext(
					customCommandHandler,
					mappedChannels.Select(y => y.Value.Channel),
					loggerFactory.CreateLogger<ChatTrackingContext>(),
					() =>
					{
						lock (trackingContexts)
							trackingContexts.Remove(context);
					});

			lock (trackingContexts)
				trackingContexts.Add(context);

			return context;
		}

		/// <inheritdoc />
		public void RegisterCommandHandler(ICustomCommandHandler customCommandHandler)
		{
			if (this.customCommandHandler != null)
				throw new InvalidOperationException("RegisterCommandHandler() already called!");
			this.customCommandHandler = customCommandHandler ?? throw new ArgumentNullException(nameof(customCommandHandler));
		}

		/// <inheritdoc />
		public async Task DeleteConnection(long connectionId, CancellationToken cancellationToken)
		{
			logger.LogTrace("DeleteConnection {connectionId}", connectionId);
			var provider = await RemoveProviderChannels(connectionId, true, cancellationToken);
			if (provider != null)
				try
				{
					await provider.Disconnect(cancellationToken);
				}
				finally
				{
					await provider.DisposeAsync();
				}
			else
				logger.LogTrace("DeleteConnection: ID {connectionId} doesn't exist!", connectionId);
		}

		/// <inheritdoc />
		public Task HandleRestart(Version updateVersion, bool graceful, CancellationToken cancellationToken)
		{
			var message =
				updateVersion == null
				? $"TGS: {(graceful ? "Graceful r" : "R")}estart requested..."
				: $"TGS: Updating to version {updateVersion}...";
			List<ulong> wdChannels;
			lock (mappedChannels) // so it doesn't change while we're using it
				wdChannels = mappedChannels
					.Where(x => !x.Value.Channel.IsPrivateChannel)
					.Select(x => x.Key)
					.ToList();

			return SendMessage(wdChannels, null, message, cancellationToken);
		}

		/// <summary>
		/// Remove a <see cref="IProvider"/> from <see cref="mappedChannels"/> optionally removing the provider itself from <see cref="providers"/> and updating the <see cref="trackingContexts"/> as well.
		/// </summary>
		/// <param name="connectionId">The <see cref="Api.Models.EntityId.Id"/> of the <see cref="IProvider"/> to delete.</param>
		/// <param name="removeProvider">If the provider should be removed from <see cref="providers"/> and <see cref="trackingContexts"/> should be update.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="IProvider"/> being removed if it exists, <see langword="null"/> otherwise.</returns>
		async Task<IProvider> RemoveProviderChannels(long connectionId, bool removeProvider, CancellationToken cancellationToken)
		{
			logger.LogTrace("RemoveProviderChannels {connectionId}...", connectionId);
			IProvider provider;
			lock (providers)
			{
				if (!providers.TryGetValue(connectionId, out provider))
				{
					logger.LogTrace("Aborted, no such provider!");
					return null;
				}

				if (removeProvider)
					providers.Remove(connectionId);
			}

			Task trackingContextsUpdateTask;
			lock (mappedChannels)
			{
				foreach (var mappedConnectionChannel in mappedChannels.Where(x => x.Value.ProviderId == connectionId).Select(x => x.Key).ToList())
					mappedChannels.Remove(mappedConnectionChannel);

				var newMappedChannels = mappedChannels.Select(y => y.Value.Channel).ToList();

				if (removeProvider)
					lock (trackingContexts)
						trackingContextsUpdateTask = Task.WhenAll(trackingContexts.Select(x => x.UpdateChannels(newMappedChannels, cancellationToken)));
				else
					trackingContextsUpdateTask = Task.CompletedTask;
			}

			await trackingContextsUpdateTask;

			return provider;
		}

		/// <summary>
		/// Remap the channels for a given <paramref name="provider"/>.
		/// </summary>
		/// <param name="provider">The <see cref="IProvider"/> to remap channels for.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task"/> representing the running operation.</returns>
		async Task RemapProvider(IProvider provider, CancellationToken cancellationToken)
		{
			logger.LogTrace("Remapping channels for provider reconnection...");
			IEnumerable<Models.ChatChannel> channelsToMap;
			long providerId;
			lock (providers)
				providerId = providers.Where(x => x.Value == provider).Select(x => x.Key).First();

			lock (activeChatBots)
				channelsToMap = activeChatBots.FirstOrDefault(x => x.Id == providerId)?.Channels;

			if (channelsToMap?.Any() ?? false)
				await ChangeChannels(providerId, channelsToMap, cancellationToken);
		}

		/// <summary>
		/// Processes a <paramref name="message"/>.
		/// </summary>
		/// <param name="provider">The <see cref="IProvider"/> who recevied <paramref name="message"/>.</param>
		/// <param name="message">The <see cref="Message"/> to process. If <see langword="null"/>, this indicates the provider reconnected.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task"/> representing the running operation.</returns>
#pragma warning disable CA1502
		async Task ProcessMessage(IProvider provider, Message message, CancellationToken cancellationToken)
#pragma warning restore CA1502
		{
			if (!provider.Connected)
			{
				logger.LogTrace("Abort message processing because provider is disconnected!");
				return;
			}

			// provider reconnected, remap channels.
			if (message == null)
			{
				await RemapProvider(provider, cancellationToken);
				return;
			}

			// map the channel if it's private and we haven't seen it
			var providerChannelId = message.User.Channel.RealId;
			KeyValuePair<ulong, ChannelMapping>? mappedChannel;
			long providerId;
			lock (providers)
			{
				// important, otherwise we could end up processing during shutdown
				cancellationToken.ThrowIfCancellationRequested();

				providerId = providers
					.Where(x => x.Value == provider)
					.Select(x => x.Key)
					.First();
				mappedChannel = mappedChannels
					.Where(x => x.Value.ProviderId == providerId && x.Value.ProviderChannelId == providerChannelId)
					.Select(x => (KeyValuePair<ulong, ChannelMapping>?)x)
					.FirstOrDefault();
			}

			if (message.User.Channel.IsPrivateChannel)
				lock (mappedChannels)
					if (!mappedChannel.HasValue)
					{
						ulong newId;
						lock (synchronizationLock)
							newId = channelIdCounter++;
						logger.LogTrace(
							"Mapping private channel {connectionName}:{channelFriendlyName} as {newId}",
							message.User.Channel.ConnectionName,
							message.User.FriendlyName,
							newId);
						mappedChannels.Add(newId, new ChannelMapping
						{
							IsWatchdogChannel = false,
							ProviderChannelId = message.User.Channel.RealId,
							ProviderId = providerId,
							Channel = message.User.Channel,
						});
						message.User.Channel.RealId = newId;
					}
					else
						message.User.Channel.RealId = mappedChannel.Value.Key;
			else
			{
				if (!mappedChannel.HasValue)
				{
					logger.LogError(
						"Error mapping message: Provider ID: {providerId}, Channel Real ID: {realId}",
						providerId,
						message.User.Channel.RealId);
					await SendMessage(
						new List<ulong>
						{
							message.User.Channel.RealId,
						},
						null,
						"Processing error, check logs!",
						cancellationToken)
						;
					return;
				}

				var mappingChannelRepresentation = mappedChannel.Value.Value.Channel;

				message.User.Channel.Id = mappingChannelRepresentation.Id;
				message.User.Channel.Tag = mappingChannelRepresentation.Tag;
				message.User.Channel.IsAdminChannel = mappingChannelRepresentation.IsAdminChannel;
			}

			var splits = new List<string>(message.Content.Trim().Split(' '));
			var address = splits[0];
			if (address.Length > 1 && (address.Last() == ':' || address.Last() == ','))
				address = address[0..^1];

			address = address.ToUpperInvariant();

			var addressed =
				address == CommonMention.ToUpperInvariant()
				|| address == provider.BotMention.ToUpperInvariant();

			// no mention
			if (!addressed && !message.User.Channel.IsPrivateChannel)
				return;

			logger.LogTrace(
				"Start processing command: {message}. User (True provider Id): {profiderId}",
				message.Content,
				JsonConvert.SerializeObject(message.User));
			try
			{
				if (addressed)
					splits.RemoveAt(0);

				if (splits.Count == 0)
				{
					// just a mention
					await SendMessage(new List<ulong> { message.User.Channel.RealId }, message, "Hi!", cancellationToken);
					return;
				}

				var command = splits[0].ToUpperInvariant();
				splits.RemoveAt(0);
				var arguments = String.Join(" ", splits);

				ICommand GetCommand(string commandName)
				{
					if (!builtinCommands.TryGetValue(commandName, out var handler))
					{
						handler = trackingContexts
							.Where(x => x.CustomCommands != null)
							.SelectMany(x => x.CustomCommands)
							.Where(x => x.Name.ToUpperInvariant() == commandName)
							.FirstOrDefault();
					}

					return handler;
				}

				const string UnknownCommandMessage = "Unknown command! Type '?' or 'help' for available commands.";

				if (command == "HELP" || command == "?")
				{
					string helpText;
					if (splits.Count == 0)
					{
						var allCommands = builtinCommands.Select(x => x.Value).ToList();
						allCommands.AddRange(
							trackingContexts
								.Where(x => x.CustomCommands != null)
								.SelectMany(
									x => x.CustomCommands));
						helpText = String.Format(CultureInfo.InvariantCulture, "Available commands (Type '?' or 'help' and then a command name for more details): {0}", String.Join(", ", allCommands.Select(x => x.Name)));
					}
					else
					{
						var helpHandler = GetCommand(splits[0].ToUpperInvariant());
						if (helpHandler != default)
							helpText = String.Format(CultureInfo.InvariantCulture, "{0}: {1}{2}", helpHandler.Name, helpHandler.HelpText, helpHandler.AdminOnly ? " - May only be used in admin channels" : String.Empty);
						else
							helpText = UnknownCommandMessage;
					}

					await SendMessage(new List<ulong> { message.User.Channel.RealId }, message, helpText, cancellationToken);
					return;
				}

				var commandHandler = GetCommand(command);

				if (commandHandler == default)
				{
					await SendMessage(new List<ulong> { message.User.Channel.RealId }, message, UnknownCommandMessage, cancellationToken);
					return;
				}

				if (commandHandler.AdminOnly && !message.User.Channel.IsAdminChannel)
				{
					await SendMessage(new List<ulong> { message.User.Channel.RealId }, message, "Use this command in an admin channel!", cancellationToken);
					return;
				}

				var result = await commandHandler.Invoke(arguments, message.User, cancellationToken);
				if (result != null)
					await SendMessage(new List<ulong> { message.User.Channel.RealId }, message, result, cancellationToken);
			}
			catch (OperationCanceledException ex)
			{
				logger.LogTrace(ex, "Command processing canceled!");
				throw;
			}
			catch (Exception e)
			{
				// error bc custom commands should reply about why it failed
				logger.LogError(e, "Error processing chat command");
				await SendMessage(
					new List<ulong> { message.User.Channel.RealId },
					message,
					"TGS: Internal error processing command! Check server logs!",
					cancellationToken)
					;
			}
			finally
			{
				logger.LogTrace("Done processing command.");
			}
		}

		/// <summary>
		/// Monitors active providers for new <see cref="Message"/>s.
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task"/> representing the running operation.</returns>
		async Task MonitorMessages(CancellationToken cancellationToken)
		{
			logger.LogTrace("Starting processing loop...");
			var messageTasks = new Dictionary<IProvider, Task<Message>>();
			Task activeProcessingTask = Task.CompletedTask;
			try
			{
				Task updatedTask = null;
				while (!cancellationToken.IsCancellationRequested)
				{
					if (updatedTask?.IsCompleted != false)
						lock (synchronizationLock)
							updatedTask = connectionsUpdated.Task;

					// prune disconnected providers
					foreach (var disposedProviderMessageTaskKvp in messageTasks.Where(x => x.Key.Disposed).ToList())
						messageTasks.Remove(disposedProviderMessageTaskKvp.Key);

					// add new ones
					lock (providers)
						foreach (var providerKvp in providers)
							if (!messageTasks.ContainsKey(providerKvp.Value))
								messageTasks.Add(providerKvp.Value, providerKvp.Value.NextMessage(cancellationToken));

					if (messageTasks.Count == 0)
					{
						logger.LogTrace("No providers active, pausing messsage monitoring...");
						await updatedTask.WithToken(cancellationToken);
						logger.LogTrace("Resuming message monitoring...");
						continue;
					}

					// wait for a message
					await Task.WhenAny(updatedTask, Task.WhenAny(messageTasks.Select(x => x.Value)));

					// process completed ones
					foreach (var completedMessageTaskKvp in messageTasks.Where(x => x.Value.IsCompleted).ToList())
					{
						var message = await completedMessageTaskKvp.Value;
						var messageNumber = Interlocked.Increment(ref messagesProcessed);

						async Task WrapProcessMessage()
						{
							var localActiveProcessingTask = activeProcessingTask;
							using (LogContext.PushProperty("ChatMessage", messageNumber))
								try
								{
									await ProcessMessage(completedMessageTaskKvp.Key, message, cancellationToken);
								}
								catch (Exception ex)
								{
									logger.LogError(ex, "Error processing message {messageNumber}!", messageNumber);
								}

							await localActiveProcessingTask;
						}

						activeProcessingTask = WrapProcessMessage();

						messageTasks.Remove(completedMessageTaskKvp.Key);
					}
				}
			}
			catch (OperationCanceledException ex)
			{
				logger.LogTrace(ex, "Message processing loop cancelled!");
			}
			catch (Exception e)
			{
				logger.LogError(e, "Message loop crashed!");
			}
			finally
			{
				await activeProcessingTask;
			}

			logger.LogTrace("Leaving message processing loop");
		}

		/// <summary>
		/// Asynchronously send a given <paramref name="message"/> to a set of <paramref name="channelIds"/>.
		/// </summary>
		/// <param name="channelIds">The <see cref="Models.ChatChannel.Id"/>s of the <see cref="Models.ChatChannel"/>s to send to.</param>
		/// <param name="replyTo">The <see cref="Message"/> to reply to.</param>
		/// <param name="message">The message to send.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task"/> representing the running operation.</returns>
		Task SendMessage(IEnumerable<ulong> channelIds, Message replyTo, string message, CancellationToken cancellationToken)
		{
			logger.LogTrace("Chat send \"{message}\" to channels: {channelIdsCommaSeperated}", message, String.Join(", ", channelIds));

			return Task.WhenAll(
				channelIds.Select(x =>
				{
					ChannelMapping channelMapping;
					lock (mappedChannels)
						if (!mappedChannels.TryGetValue(x, out channelMapping))
							return Task.CompletedTask;
					IProvider provider;
					lock (providers)
						if (!providers.TryGetValue(channelMapping.ProviderId, out provider))
							return Task.CompletedTask;
					return provider.SendMessage(replyTo, message, channelMapping.ProviderChannelId, cancellationToken);
				}));
		}

		/// <summary>
		/// Aggregate all <see cref="IProvider.InitialConnectionJob"/>s into one <sse cref="Task"/>.
		/// </summary>
		/// <returns>A <see cref="Task"/> representing the running operation.</returns>
		async Task InitialConnection()
		{
			await Task.WhenAll(providers.Select(x => x.Value.InitialConnectionJob));
			logger.LogTrace("Initial provider connection task completed");
		}

		/// <summary>
		/// Adds a given <paramref name="task"/> to <see cref="messageSendTask"/>.
		/// </summary>
		/// <param name="task">The <see cref="Task"/> to add.</param>
		void AddMessageTask(Task task)
		{
			async Task Wrap(Task originalTask)
			{
				await originalTask;
				try
				{
					await task;
				}
				catch (Exception ex)
				{
					logger.LogWarning(ex, "Error in asynchronous chat message!");
				}
			}

			lock (handlerCts)
				messageSendTask = Wrap(messageSendTask);
		}
	}
}
