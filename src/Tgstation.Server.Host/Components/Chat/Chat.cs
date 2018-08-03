using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Api.Models.Internal;
using Tgstation.Server.Host.Components.Chat.Commands;
using Tgstation.Server.Host.Components.Chat.Providers;
using Tgstation.Server.Host.IO;

namespace Tgstation.Server.Host.Components.Chat
{
	/// <inheritdoc />
	sealed class Chat : IChat
	{
		const string CommonMention = "!tgs";

		/// <summary>
		/// The <see cref="IProviderFactory"/> for the <see cref="Chat"/>
		/// </summary>
		readonly IProviderFactory providerFactory;

		/// <summary>
		/// The <see cref="IIOManager"/> for the <see cref="Chat"/>
		/// </summary>
		readonly IIOManager ioManager;

		/// <summary>
		/// The <see cref="ICommandFactory"/> for the <see cref="Chat"/>
		/// </summary>
		readonly ICommandFactory commandFactory;

		/// <summary>
		/// The <see cref="ILogger"/> for the <see cref="Chat"/>
		/// </summary>
		readonly ILogger<Chat> logger;

		/// <summary>
		/// Unchanging <see cref="ICommand"/>s in the <see cref="Chat"/> mapped by <see cref="ICommand.Name"/>
		/// </summary>
		readonly Dictionary<string, ICommand> builtinCommands;

		/// <summary>
		/// Map of <see cref="IProvider"/>s in use, keyed by <see cref="ChatSettings.Id"/>
		/// </summary>
		readonly Dictionary<long, IProvider> providers;

		/// <summary>
		/// Map of <see cref="Channel.RealId"/>s to <see cref="ChannelMapping"/>s
		/// </summary>
		readonly Dictionary<ulong, ChannelMapping> mappedChannels;

		/// <summary>
		/// The active <see cref="IJsonTrackingContext"/>s for the <see cref="Chat"/>
		/// </summary>
		readonly List<IJsonTrackingContext> trackingContexts;

		/// <summary>
		/// The <see cref="CancellationTokenSource"/> for <see cref="chatHandler"/>
		/// </summary>
		readonly CancellationTokenSource handlerCts;

		/// <summary>
		/// The initial <see cref="Models.ChatSettings"/> for the <see cref="Chat"/>
		/// </summary>
		readonly List<Models.ChatSettings> initialChatSettings;
		
		/// <summary>
		/// The <see cref="ICustomCommandHandler"/> for the <see cref="ChangeChannels(long, IEnumerable{Api.Models.ChatChannel}, CancellationToken)"/>
		/// </summary>
		ICustomCommandHandler customCommandHandler;

		/// <summary>
		/// The <see cref="Task"/> that monitors incoming chat messages
		/// </summary>
		Task chatHandler;

		/// <summary>
		/// The <see cref="TaskCompletionSource{TResult}"/> that completes when <see cref="ChatSettings"/> change
		/// </summary>
		TaskCompletionSource<object> connectionsUpdated;

		/// <summary>
		/// Used for remapping <see cref="Channel.RealId"/>s
		/// </summary>
		ulong channelIdCounter;

		/// <summary>
		/// If <see cref="StartAsync(CancellationToken)"/> has been called
		/// </summary>
		bool started;

		/// <summary>
		/// Construct a <see cref="Chat"/>
		/// </summary>
		/// <param name="providerFactory">The value of <see cref="providerFactory"/></param>
		/// <param name="ioManager">The value of <see cref="ioManager"/></param>
		/// <param name="logger">The value of <see cref="logger"/></param>
		/// <param name="commandFactory">The value of <see cref="commandFactory"/></param>
		/// <param name="initialChatSettings">The <see cref="IEnumerable{T}"/> used to populate <see cref="initialChatSettings"/></param>
		public Chat(IProviderFactory providerFactory, IIOManager ioManager, ICommandFactory commandFactory, ILogger<Chat> logger, IEnumerable<Models.ChatSettings> initialChatSettings)
		{
			this.providerFactory = providerFactory ?? throw new ArgumentNullException(nameof(providerFactory));
			this.ioManager = ioManager ?? throw new ArgumentNullException(nameof(ioManager));
			this.commandFactory = commandFactory ?? throw new ArgumentNullException(nameof(commandFactory));
			this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
			this.initialChatSettings = initialChatSettings?.ToList() ?? throw new ArgumentNullException(nameof(initialChatSettings));

			builtinCommands = new Dictionary<string, ICommand>();
			providers = new Dictionary<long, IProvider>();
			mappedChannels = new Dictionary<ulong, ChannelMapping>();
			trackingContexts = new List<IJsonTrackingContext>();
			handlerCts = new CancellationTokenSource();
			connectionsUpdated = new TaskCompletionSource<object>();
			channelIdCounter = 1;
		}

		/// <inheritdoc />
		public void Dispose()
		{
			handlerCts.Dispose();
			foreach (var I in providers)
				I.Value.Dispose();
		}

		/// <summary>
		/// Remove a <see cref="IProvider"/> from <see cref="providers"/> and <see cref="mappedChannels"/> optionally updating the <see cref="trackingContexts"/> as well
		/// </summary>
		/// <param name="connectionId">The <see cref="ChatSettings.Id"/> of the <see cref="IProvider"/> to delete</param>
		/// <param name="updateTrackings">If <see cref="trackingContexts"/> should be update</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="IProvider"/> being removed if it exists, <see langword="false"/> otherwise</returns>
		async Task<IProvider> RemoveProvider(long connectionId, bool updateTrackings, CancellationToken cancellationToken)
		{
			IProvider provider;
			lock (providers)
				if (!providers.TryGetValue(connectionId, out provider))
					return null;
			Task task;
			lock (mappedChannels)
			{
				foreach (var I in mappedChannels.Where(x => x.Value.ProviderId == connectionId).Select(x => x.Key).ToList())
					mappedChannels.Remove(I);

				if (updateTrackings)
					lock (trackingContexts)
						task = Task.WhenAll(trackingContexts.Select(x => x.SetChannels(mappedChannels.Select(y => y.Value.Channel), cancellationToken)));
				else
					task = Task.CompletedTask;
			}
			await task.ConfigureAwait(false);
			return provider;
		}

		/// <summary>
		/// Processes a <paramref name="message"/>
		/// </summary>
		/// <param name="provider">The <see cref="IProvider"/> who recevied <paramref name="message"/></param>
		/// <param name="message">The <see cref="Message"/> to process</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		async Task ProcessMessage(IProvider provider, Message message, CancellationToken cancellationToken)
		{
			logger.LogTrace("Chat message: {0}. User (Note unconverted provider Id): {1}", message.Content, JsonConvert.SerializeObject(message.User));

			//map the channel if it's private and we haven't seen it
			if (message.User.Channel.IsPrivate)
				lock (providers)
					lock (mappedChannels)
					{
						if (!provider.Connected)
							return;
						var enumerable = mappedChannels.Where(x => x.Value.ProviderChannelId == message.User.Channel.RealId);
						if (!enumerable.Any())
						{
							ulong newId;
							lock (this)
								newId = channelIdCounter++;
							mappedChannels.Add(newId, new ChannelMapping
							{
								IsWatchdogChannel = false,
								ProviderChannelId = message.User.Channel.RealId,
								ProviderId = providers.Where(x => x.Value == provider).Select(x => x.Key).First(),
								Channel = message.User.Channel
							});
							message.User.Channel.RealId = newId;
						}
						else
							message.User.Channel.RealId = enumerable.First().Key;
					}

			var splits = new List<string>(message.Content.Trim().Split(' '));
			var address = splits[0];
			if (address.Length > 1 && (address[address.Length - 1] == ':' || address[address.Length - 1] == ','))
				address = address.Substring(0, address.Length - 1);

			address = address.ToUpperInvariant();

			var addressed = address == CommonMention.ToUpperInvariant() || address == provider.BotMention.ToUpperInvariant();

			if (!addressed && !message.User.Channel.IsPrivate)
				//no mention
				return;

			if (addressed)
				splits.RemoveAt(0);

			if (splits.Count == 0 || (!addressed && splits.Count == 1))
			{
				//just a mention
				await SendMessage("Hi!", new List<ulong> { message.User.Channel.RealId }, cancellationToken).ConfigureAwait(false);
				return;
			}

			var command = splits[0].ToUpperInvariant();
			splits.RemoveAt(0);
			var arguments = String.Join(" ", splits);
			
			try
			{
				async Task<ICommand> GetCommand(string commandName)
				{
					if (!builtinCommands.TryGetValue(commandName, out var handler))
					{
						var tasks = trackingContexts.Select(x => x.GetCustomCommands(cancellationToken));
						await Task.WhenAll(tasks).ConfigureAwait(false);
						handler = tasks.SelectMany(x => x.Result).Where(x => x.Name.ToUpperInvariant() == commandName).FirstOrDefault();
					}
					return handler;
				};

				const string UnknownCommandMessage = "Unknown command! Type '?' or 'help' for available commands.";

				if (command == "HELP" || command == "?")
				{
					string helpText;
					if (splits.Count == 0)
					{
						var allCommands = builtinCommands.Select(x => x.Value).ToList();
						var tasks = trackingContexts.Select(x => x.GetCustomCommands(cancellationToken));
						await Task.WhenAll(tasks).ConfigureAwait(false);
						allCommands.AddRange(tasks.SelectMany(x => x.Result));
						helpText = String.Format(CultureInfo.InvariantCulture, "Available commands (Type '?' or 'help' and then a command name for more details): {0}", String.Join(", ", allCommands.Select(x => x.Name)));
					}
					else
					{
						var helpHandler = await GetCommand(splits[0].ToUpperInvariant()).ConfigureAwait(false);
						if (helpHandler != default)
							helpText = String.Format(CultureInfo.InvariantCulture, "{0}: {1}", helpHandler.Name, helpHandler.HelpText);
						else
							helpText = UnknownCommandMessage;
					}
					await SendMessage(helpText, new List<ulong> { message.User.Channel.RealId }, cancellationToken).ConfigureAwait(false);
					return;
				}

				var commandHandler = await GetCommand(command).ConfigureAwait(false);
				if (commandHandler == default)
				{
					await SendMessage(UnknownCommandMessage, new List<ulong> { message.User.Channel.RealId }, cancellationToken).ConfigureAwait(false);
					return;
				}

				var result = await commandHandler.Invoke(arguments, message.User, cancellationToken).ConfigureAwait(false);
				if(result != null)
					await SendMessage(result, new List<ulong> { message.User.Channel.RealId }, cancellationToken).ConfigureAwait(false);
			}
			catch (Exception e)
			{
				//error bc custom commands should reply about why it failed
				logger.LogError("Error processing chat command: {0}", e);
				await SendMessage("Internal error processing command!", new List<ulong> { message.User.Channel.RealId }, cancellationToken).ConfigureAwait(false);
			}
		}

		/// <summary>
		/// Monitors active providers for new <see cref="Message"/>s
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		async Task MonitorMessages(CancellationToken cancellationToken)
		{
			var messageTasks = new Dictionary<IProvider, Task<Message>>();
			try
			{
				while (!cancellationToken.IsCancellationRequested)
				{
					//prune disconnected providers
					foreach (var I in messageTasks.Where(x => !x.Key.Connected).ToList())
						messageTasks.Remove(I.Key);

					//add new ones
					Task updatedTask;
					lock (this)
						updatedTask = connectionsUpdated.Task;
					lock (providers)
						foreach (var I in providers)
							if (I.Value.Connected && !messageTasks.ContainsKey(I.Value))
								messageTasks.Add(I.Value, I.Value.NextMessage(cancellationToken));

					if(messageTasks.Count == 0)
					{
						await Task.Delay(1000, cancellationToken).ConfigureAwait(false);
						continue;
					}

					//wait for a message
					await Task.WhenAny(updatedTask, Task.WhenAny(messageTasks.Select(x => x.Value))).ConfigureAwait(false);
					
					//process completed ones
					foreach (var I in messageTasks.Where(x => x.Value.IsCompleted).ToList())
					{
						var message = await I.Value.ConfigureAwait(false);

						await ProcessMessage(I.Key, message, cancellationToken).ConfigureAwait(false);

						messageTasks.Remove(I.Key);
					}
				}
			}
			catch (OperationCanceledException) { }
			catch(Exception e)
			{
				logger.LogError("Message monitor crashed!: Exception: {0}", e);
			}
		}

		/// <inheritdoc />
		public async Task ChangeChannels(long connectionId, IEnumerable<Api.Models.ChatChannel> newChannels, CancellationToken cancellationToken)
		{
			if (newChannels == null)
				throw new ArgumentNullException(nameof(newChannels));
			var provider = await RemoveProvider(connectionId, false, cancellationToken).ConfigureAwait(false);
			if (provider == null)
				return;
			var results = await provider.MapChannels(newChannels, cancellationToken).ConfigureAwait(false);
			if (results == null)    //aborted
				return;
			var mappings = Enumerable.Zip(newChannels, results, (x, y) => new ChannelMapping
			{
				IsWatchdogChannel = x.IsWatchdogChannel == true,
				ProviderChannelId = y.RealId,
				ProviderId = connectionId,
				Channel = y
			});

			ulong baseId;
			lock (this)
			{
				baseId = channelIdCounter;
				channelIdCounter += (ulong)results.Count;
			}

			Task task;
			lock (mappedChannels)
			{
				lock (providers)
					if (!providers.TryGetValue(connectionId, out IProvider verify) || verify != provider)   //aborted again
						return;
				foreach (var I in mappings)
				{
					var newId = baseId++;
					mappedChannels.Add(newId, I);
					I.Channel.RealId = newId;
				}

				lock (trackingContexts)
					task = Task.WhenAll(trackingContexts.Select(x => x.SetChannels(mappedChannels.Select(y => y.Value.Channel), cancellationToken)));
			}
			await task.ConfigureAwait(false);
		}

		/// <inheritdoc />
		public async Task ChangeSettings(ChatSettings newSettings, CancellationToken cancellationToken)
		{
			if (newSettings == null)
				throw new ArgumentNullException(nameof(newSettings));
			IProvider provider;

			async Task DisconnectProvider(IProvider p)
			{
				try
				{
					await p.Disconnect(cancellationToken).ConfigureAwait(false);
				}
				finally
				{
					p.Dispose();
				}
			}

			Task disconnectTask;
			lock (providers)
			{
				//raw settings changes forces a rebuild of the provider
				if (providers.TryGetValue(newSettings.Id, out provider))
				{
					providers.Remove(newSettings.Id);
					disconnectTask = DisconnectProvider(provider);
				}
				else
					disconnectTask = Task.CompletedTask;
				if (newSettings.Enabled.Value)
				{
					provider = providerFactory.CreateProvider(newSettings);
					providers.Add(newSettings.Id, provider);
				}
			}

			lock (mappedChannels)
				foreach (var I in mappedChannels.Where(x => x.Value.ProviderId == newSettings.Id).Select(x => x.Key).ToList())
					mappedChannels.Remove(I);

			await disconnectTask.ConfigureAwait(false);

			if (started)
			{
				if (newSettings.Enabled.Value)
					await provider.Connect(cancellationToken).ConfigureAwait(false);
				lock(this)
				{
					//same thread shennanigans
					var oldOne = connectionsUpdated;
					connectionsUpdated = new TaskCompletionSource<object>();
					oldOne.SetResult(null);
				}
			}
		}

		/// <inheritdoc />
		public Task SendMessage(string message, IEnumerable<ulong> channelIds, CancellationToken cancellationToken)
		{
			if (message == null)
				throw new ArgumentNullException(nameof(message));
			if (channelIds == null)
				throw new ArgumentNullException(nameof(channelIds));

			return Task.WhenAll(channelIds.Select(x =>
			{
				ChannelMapping channelMapping;
				lock(mappedChannels)
					if (!mappedChannels.TryGetValue(x, out channelMapping))
						return Task.CompletedTask;
				IProvider provider;
				lock (providers)
					if (!providers.TryGetValue(channelMapping.ProviderId, out provider))
						return Task.CompletedTask;
				return provider.SendMessage(channelMapping.ProviderChannelId, message, cancellationToken);
			}));
		}

		/// <inheritdoc />
		public Task SendWatchdogMessage(string message, CancellationToken cancellationToken)
		{
			List<ulong> wdChannels;
			lock (mappedChannels)   //so it doesn't change while we're using it
				wdChannels = mappedChannels.Where(x => x.Value.IsWatchdogChannel).Select(x => x.Key).ToList();
			return SendMessage(message, wdChannels, cancellationToken);
		}

		/// <inheritdoc />
		public async Task StartAsync(CancellationToken cancellationToken)
		{
			foreach (var I in commandFactory.GenerateCommands())
				builtinCommands.Add(I.Name.ToUpperInvariant(), I);
			await Task.WhenAll(initialChatSettings.Select(x => ChangeSettings(x, cancellationToken))).ConfigureAwait(false);
			await Task.WhenAll(providers.Select(x => x.Value).Select(x => x.Connect(cancellationToken))).ConfigureAwait(false);
			await Task.WhenAll(initialChatSettings.Select(x => ChangeChannels(x.Id, x.Channels, cancellationToken))).ConfigureAwait(false);
			chatHandler = MonitorMessages(handlerCts.Token);
			started = true;
		}

		/// <inheritdoc />
		public async Task StopAsync(CancellationToken cancellationToken)
		{
			handlerCts.Cancel();
			await chatHandler.ConfigureAwait(false);
			await Task.WhenAll(providers.Select(x => x.Value).Select(x => x.Disconnect(cancellationToken))).ConfigureAwait(false);
		}

		/// <inheritdoc />
		public async Task<IJsonTrackingContext> TrackJsons(string basePath, string channelsJsonName, string commandsJsonName, CancellationToken cancellationToken)
		{
			if (customCommandHandler == null)
				throw new InvalidOperationException("RegisterCommandHandler() hasn't been called!");
			JsonTrackingContext context = null;
			context = new JsonTrackingContext(ioManager, customCommandHandler, () =>
			{
				lock (trackingContexts)
					trackingContexts.Remove(context);
			}, ioManager.ConcatPath(basePath, commandsJsonName), ioManager.ConcatPath(basePath, channelsJsonName));
			Task task;
			lock (trackingContexts)
			{
				trackingContexts.Add(context);
				lock (mappedChannels)
					task = Task.WhenAll(trackingContexts.Select(x => x.SetChannels(mappedChannels.Select(y => y.Value.Channel), cancellationToken)));
			}
			await task.ConfigureAwait(false);
			return context;
		}

		/// <inheritdoc />
		public bool Connected(long connectionId)
		{
			lock (providers)
				return providers.TryGetValue(connectionId, out var provider) && provider.Connected;
		}

		/// <inheritdoc />
		public void RegisterCommandHandler(ICustomCommandHandler customCommandHandler)
		{
			if (this.customCommandHandler != null)
				throw new InvalidOperationException("RegisterCommandHandler() already called!");
			this.customCommandHandler = customCommandHandler ?? throw new ArgumentNullException(nameof(customCommandHandler));
		}

		/// <inheritdoc />
		public Task DeleteConnection(long connectionId, CancellationToken cancellationToken) => RemoveProvider(connectionId, true, cancellationToken);
	}
}
